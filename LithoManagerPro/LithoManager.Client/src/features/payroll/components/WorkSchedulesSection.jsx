import { CalendarClock } from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Badge } from '../../../components/ui/Badge.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import pageStyles from '../../../app/Page.module.css'
import {
  formatDate,
  formatNumber,
  getOperationErrorMessage,
  getWorkShiftLabel,
  todayIsoDate,
} from '../payrollFormat.js'
import {
  getWorkSchedules,
  setWorkSchedule,
} from '../services/payrollOperationsService.js'
import styles from '../pages/Payroll.module.css'
import { FormActions, FormField, PayrollModal } from './PayrollModal.jsx'

export function WorkSchedulesSection({
  accessToken,
  canManage,
  catalogs,
  employees,
}) {
  const [schedules, setSchedules] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [notice, setNotice] = useState('')
  const [editTarget, setEditTarget] = useState(null)

  const loadSchedules = useCallback(async ({ signal } = {}) => {
    setIsLoading(true)
    setLoadError('')

    try {
      setSchedules(await getWorkSchedules({ accessToken, signal }))
    } catch (error) {
      if (error.name !== 'AbortError') {
        setLoadError(error.message)
      }
    } finally {
      if (!signal?.aborted) {
        setIsLoading(false)
      }
    }
  }, [accessToken])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadSchedules({ signal: controller.signal })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadSchedules])

  // Administrators see every active employee, so those without a schedule
  // stand out. Other roles only see the employees that have one.
  const rows = useMemo(() => {
    const byEmployee = new Map(
      schedules.map((schedule) => [schedule.employeeId, schedule]),
    )

    if (employees.length === 0) {
      return schedules.map((schedule) => ({
        employeeId: schedule.employeeId,
        name: `${schedule.firstName} ${schedule.lastName}`,
        identificationNumber: schedule.identificationNumber,
        schedule,
      }))
    }

    return employees.map((employee) => ({
      employeeId: employee.employeeId,
      name: `${employee.firstName} ${employee.lastName}`,
      identificationNumber: employee.identificationNumber,
      schedule: byEmployee.get(employee.employeeId) ?? null,
    }))
  }, [employees, schedules])

  const missingCount = rows.filter((row) => row.schedule === null).length

  return (
    <>
      <div className={styles.sectionHeader}>
        <p>
          La jornada define las horas ordinarias por día con las que se pagan
          las horas extra.
        </p>
      </div>

      {notice ? <Alert variant="success">{notice}</Alert> : null}
      {loadError ? <Alert variant="error">{loadError}</Alert> : null}
      {!isLoading && missingCount > 0 ? (
        <Alert variant="warning">
          {missingCount === 1
            ? '1 empleado no tiene horario: sus horas extra se pagan con una jornada de 8 horas.'
            : `${missingCount} empleados no tienen horario: sus horas extra se pagan con una jornada de 8 horas.`}
        </Alert>
      ) : null}

      <div className={pageStyles.tableShell}>
        <table className={pageStyles.table}>
          <thead>
            <tr>
              <th>Empleado</th>
              <th>Jornada</th>
              <th>Horas semanales</th>
              <th>Vigente desde</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan="5" className={styles.loadingCell}>Cargando horarios</td>
              </tr>
            ) : null}

            {!isLoading && rows.length === 0 ? (
              <tr>
                <td colSpan="5" className={styles.emptyCell}>No hay horarios registrados</td>
              </tr>
            ) : null}

            {!isLoading
              ? rows.map((row) => (
                <tr key={row.employeeId}>
                  <td>
                    <div className={styles.employeeCell}>
                      <strong>{row.name}</strong>
                      <span>{row.identificationNumber}</span>
                    </div>
                  </td>
                  <td>
                    {row.schedule ? (
                      getWorkShiftLabel(
                        row.schedule.workShiftTypeCode,
                        row.schedule.workShiftTypeName,
                      )
                    ) : (
                      <Badge variant="warning">Sin horario</Badge>
                    )}
                  </td>
                  <td>
                    {row.schedule ? `${formatNumber(row.schedule.weeklyOrdinaryHours)} h` : '-'}
                  </td>
                  <td>{row.schedule ? formatDate(row.schedule.effectiveFromDate) : '-'}</td>
                  <td>
                    {canManage ? (
                      <div className={styles.rowActions}>
                        <Button
                          variant="secondary"
                          size="small"
                          aria-label={`${row.schedule ? 'Cambiar' : 'Asignar'} horario de ${row.name}`}
                          onClick={() => {
                            setNotice('')
                            setEditTarget(row)
                          }}
                        >
                          <CalendarClock size={16} aria-hidden="true" />
                          {row.schedule ? 'Cambiar' : 'Asignar'}
                        </Button>
                      </div>
                    ) : null}
                  </td>
                </tr>
              ))
              : null}
          </tbody>
        </table>
      </div>

      {editTarget ? (
        <WorkScheduleModal
          accessToken={accessToken}
          row={editTarget}
          workShiftTypes={catalogs.workShiftTypes}
          onClose={() => setEditTarget(null)}
          onSaved={async (schedule) => {
            setEditTarget(null)
            setNotice(
              `Horario de ${schedule.firstName} ${schedule.lastName}: jornada ${getWorkShiftLabel(
                schedule.workShiftTypeCode,
                schedule.workShiftTypeName,
              ).toLowerCase()} desde el ${formatDate(schedule.effectiveFromDate)}.`,
            )
            await loadSchedules()
          }}
        />
      ) : null}
    </>
  )
}

function WorkScheduleModal({
  accessToken,
  row,
  workShiftTypes,
  onClose,
  onSaved,
}) {
  const currentFrom = row.schedule?.effectiveFromDate?.slice(0, 10) ?? null
  const today = todayIsoDate()
  const [values, setValues] = useState({
    workShiftTypeId:
      row.schedule?.workShiftTypeId ?? workShiftTypes[0]?.workShiftTypeId ?? '',
    effectiveFromDate: currentFrom && currentFrom > today ? currentFrom : today,
  })
  const [error, setError] = useState('')
  const [submitError, setSubmitError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()

    if (!values.effectiveFromDate) {
      setError('La fecha es requerida.')
      return
    }

    if (currentFrom && values.effectiveFromDate < currentFrom) {
      setError(`No puede ser anterior al horario actual (${formatDate(currentFrom)}).`)
      return
    }

    setIsSaving(true)
    setSubmitError('')

    try {
      const schedule = await setWorkSchedule({
        accessToken,
        employeeId: row.employeeId,
        workShiftTypeId: Number(values.workShiftTypeId),
        effectiveFromDate: values.effectiveFromDate,
      })

      await onSaved(schedule)
    } catch (requestError) {
      setSubmitError(getOperationErrorMessage(requestError))
      setIsSaving(false)
    }
  }

  return (
    <PayrollModal
      title={row.schedule ? 'Cambiar horario' : 'Asignar horario'}
      subtitle={row.name}
      isBusy={isSaving}
      onClose={onClose}
    >
      <form className={styles.form} onSubmit={handleSubmit}>
        {submitError ? <Alert variant="error">{submitError}</Alert> : null}

        <FormField id="schedule-shift" label="Jornada">
          <select
            id="schedule-shift"
            className={styles.input}
            value={values.workShiftTypeId}
            onChange={(event) => setValues((current) => ({
              ...current,
              workShiftTypeId: event.target.value,
            }))}
          >
            {workShiftTypes.map((shift) => (
              <option key={shift.workShiftTypeId} value={shift.workShiftTypeId}>
                {getWorkShiftLabel(shift.workShiftTypeCode, shift.name)}
                {` · ${formatNumber(shift.maxOrdinaryHoursPerDay)} h por día, `}
                {`${formatNumber(shift.maxOrdinaryHoursPerWeek)} h por semana`}
              </option>
            ))}
          </select>
        </FormField>

        <FormField
          id="schedule-from"
          label="Vigente desde"
          error={error}
          hint={row.schedule
            ? 'El horario actual se cierra el día anterior.'
            : undefined}
        >
          <input
            id="schedule-from"
            type="date"
            min={currentFrom ?? undefined}
            className={styles.input}
            value={values.effectiveFromDate}
            onChange={(event) => {
              setValues((current) => ({
                ...current,
                effectiveFromDate: event.target.value,
              }))
              setError('')
            }}
          />
        </FormField>

        <FormActions isSaving={isSaving} submitText="Guardar horario" onCancel={onClose} />
      </form>
    </PayrollModal>
  )
}
