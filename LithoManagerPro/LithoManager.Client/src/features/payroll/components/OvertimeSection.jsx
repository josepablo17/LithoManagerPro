import { Plus, ThumbsDown, ThumbsUp, X } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Badge } from '../../../components/ui/Badge.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import pageStyles from '../../../app/Page.module.css'
import {
  formatDate,
  formatNumber,
  getApprovalStatusLabel,
  getApprovalStatusVariant,
  getOperationErrorMessage,
  getOvertimeRuleLabel,
  isStaleRecordError,
  todayIsoDate,
} from '../payrollFormat.js'
import {
  cancelOvertimeRecord,
  createOvertimeRecord,
  getOvertimeRecords,
  respondOvertimeRecord,
} from '../services/payrollOperationsService.js'
import styles from '../pages/Payroll.module.css'
import { FormActions, FormField, PayrollModal } from './PayrollModal.jsx'

const statusFilters = [
  { value: 'Pending', label: 'Pendientes' },
  { value: 'Approved', label: 'Aprobadas' },
  { value: 'Rejected', label: 'Rechazadas' },
  { value: 'Cancelled', label: 'Canceladas' },
  { value: '', label: 'Todas' },
]

export function OvertimeSection({
  accessToken,
  canManage,
  catalogs,
  employees,
}) {
  const [statusFilter, setStatusFilter] = useState('Pending')
  const [records, setRecords] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [notice, setNotice] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const [rejectTarget, setRejectTarget] = useState(null)
  const [busyRecordId, setBusyRecordId] = useState(null)

  const loadRecords = useCallback(async ({ signal } = {}) => {
    setIsLoading(true)
    setLoadError('')

    try {
      setRecords(await getOvertimeRecords({
        accessToken,
        approvalStatus: statusFilter,
        signal,
      }))
    } catch (error) {
      if (error.name !== 'AbortError') {
        setLoadError(error.message)
      }
    } finally {
      if (!signal?.aborted) {
        setIsLoading(false)
      }
    }
  }, [accessToken, statusFilter])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadRecords({ signal: controller.signal })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadRecords])

  async function runAction(record, action, successMessage) {
    setBusyRecordId(record.overtimeRecordId)
    setNotice('')
    setLoadError('')

    try {
      await action()
      setNotice(successMessage)
      await loadRecords()
    } catch (error) {
      setLoadError(getOperationErrorMessage(error))

      if (isStaleRecordError(error)) {
        await loadRecords()
      }
    } finally {
      setBusyRecordId(null)
    }
  }

  function approve(record) {
    runAction(
      record,
      () => respondOvertimeRecord({
        accessToken,
        overtimeRecordId: record.overtimeRecordId,
        isApproved: true,
        expectedRowVersion: record.rowVersion,
      }),
      `Horas extra de ${record.firstName} ${record.lastName} aprobadas.`,
    )
  }

  function cancel(record) {
    runAction(
      record,
      () => cancelOvertimeRecord({
        accessToken,
        overtimeRecordId: record.overtimeRecordId,
        expectedRowVersion: record.rowVersion,
      }),
      'Registro de horas extra cancelado.',
    )
  }

  return (
    <>
      <div className={styles.sectionHeader}>
        <select
          className={pageStyles.select}
          aria-label="Filtrar horas extra por estado"
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value)}
        >
          {statusFilters.map((filter) => (
            <option key={filter.label} value={filter.value}>
              {filter.label}
            </option>
          ))}
        </select>
        {canManage ? (
          <Button
            onClick={() => {
              setNotice('')
              setCreateOpen(true)
            }}
          >
            <Plus size={18} aria-hidden="true" />
            Registrar horas extra
          </Button>
        ) : null}
      </div>

      {notice ? <Alert variant="success">{notice}</Alert> : null}
      {loadError ? <Alert variant="error">{loadError}</Alert> : null}

      <div className={pageStyles.tableShell}>
        <table className={pageStyles.table}>
          <thead>
            <tr>
              <th>Empleado</th>
              <th>Fecha</th>
              <th>Horas</th>
              <th>Tipo</th>
              <th>Estado</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan="6" className={styles.loadingCell}>Cargando horas extra</td>
              </tr>
            ) : null}

            {!isLoading && records.length === 0 ? (
              <tr>
                <td colSpan="6" className={styles.emptyCell}>
                  No hay horas extra con ese estado
                </td>
              </tr>
            ) : null}

            {!isLoading
              ? records.map((record) => (
                <tr key={record.overtimeRecordId}>
                  <td>
                    <div className={styles.employeeCell}>
                      <strong>{record.firstName} {record.lastName}</strong>
                      <span>{record.identificationNumber}</span>
                    </div>
                  </td>
                  <td>{formatDate(record.overtimeDate)}</td>
                  <td>{formatNumber(record.hours)} h</td>
                  <td>
                    {getOvertimeRuleLabel(record.overtimeRuleCode, record.overtimeRuleName)}
                    {' '}
                    <span className={styles.meta}>
                      (x{formatNumber(record.hourMultiplier)})
                    </span>
                  </td>
                  <td>
                    <Badge variant={getApprovalStatusVariant(record.approvalStatus)}>
                      {getApprovalStatusLabel(record.approvalStatus)}
                    </Badge>
                    {record.rejectionReason ? (
                      <div className={styles.meta}>{record.rejectionReason}</div>
                    ) : null}
                  </td>
                  <td>
                    {canManage && record.approvalStatus === 'Pending' ? (
                      <div className={styles.rowActions}>
                        <Button
                          variant="ghost"
                          size="small"
                          disabled={busyRecordId !== null}
                          aria-label={`Cancelar horas extra de ${record.firstName} ${record.lastName}`}
                          onClick={() => cancel(record)}
                        >
                          <X size={16} aria-hidden="true" />
                        </Button>
                        <Button
                          variant="secondary"
                          size="small"
                          disabled={busyRecordId !== null}
                          onClick={() => {
                            setNotice('')
                            setRejectTarget(record)
                          }}
                        >
                          <ThumbsDown size={16} aria-hidden="true" />
                          Rechazar
                        </Button>
                        <Button
                          size="small"
                          isLoading={busyRecordId === record.overtimeRecordId}
                          loadingText="..."
                          disabled={busyRecordId !== null}
                          onClick={() => approve(record)}
                        >
                          <ThumbsUp size={16} aria-hidden="true" />
                          Aprobar
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

      {createOpen ? (
        <CreateOvertimeModal
          accessToken={accessToken}
          employees={employees}
          overtimeRules={catalogs.overtimeRules}
          onClose={() => setCreateOpen(false)}
          onCreated={async (record) => {
            setCreateOpen(false)
            setNotice(
              `Horas extra registradas para ${record.firstName} ${record.lastName}. Quedan pendientes de aprobación.`,
            )
            setStatusFilter('Pending')
            await loadRecords()
          }}
        />
      ) : null}

      {rejectTarget ? (
        <RejectOvertimeModal
          record={rejectTarget}
          onClose={() => setRejectTarget(null)}
          onConfirm={async (reason) => {
            const record = rejectTarget
            setRejectTarget(null)
            await runAction(
              record,
              () => respondOvertimeRecord({
                accessToken,
                overtimeRecordId: record.overtimeRecordId,
                isApproved: false,
                rejectionReason: reason,
                expectedRowVersion: record.rowVersion,
              }),
              'Horas extra rechazadas.',
            )
          }}
        />
      ) : null}
    </>
  )
}

function CreateOvertimeModal({
  accessToken,
  employees,
  overtimeRules,
  onClose,
  onCreated,
}) {
  const [values, setValues] = useState({
    employeeId: '',
    // Standard overtime is the common case; the API sorts rules by code.
    overtimeRuleId:
      (overtimeRules.find((rule) => rule.overtimeRuleCode === 'Standard')
        ?? overtimeRules[0])?.overtimeRuleId ?? '',
    overtimeDate: todayIsoDate(),
    hours: '',
    notes: '',
  })
  const [errors, setErrors] = useState({})
  const [submitError, setSubmitError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  function handleChange(event) {
    const { name, value } = event.target

    setValues((current) => ({ ...current, [name]: value }))
    setErrors((current) => ({ ...current, [name]: undefined }))
  }

  async function handleSubmit(event) {
    event.preventDefault()

    const hours = Number(values.hours)
    const validation = {}

    if (!values.employeeId) {
      validation.employeeId = 'Selecciona un empleado.'
    }

    if (!values.overtimeRuleId) {
      validation.overtimeRuleId = 'Selecciona el tipo de hora extra.'
    }

    if (!values.overtimeDate) {
      validation.overtimeDate = 'La fecha es requerida.'
    }

    if (!values.hours || !(hours > 0) || hours > 24) {
      validation.hours = 'Ingresa entre 0,5 y 24 horas.'
    }

    if (Object.keys(validation).length > 0) {
      setErrors(validation)
      return
    }

    setIsSaving(true)
    setSubmitError('')

    try {
      const record = await createOvertimeRecord({
        accessToken,
        employeeId: Number(values.employeeId),
        overtimeRuleId: Number(values.overtimeRuleId),
        overtimeDate: values.overtimeDate,
        hours,
        notes: values.notes.trim(),
      })

      await onCreated(record)
    } catch (error) {
      setSubmitError(getOperationErrorMessage(error))
      setIsSaving(false)
    }
  }

  return (
    <PayrollModal
      title="Registrar horas extra"
      subtitle="Quedan pendientes hasta que un administrador las apruebe."
      isBusy={isSaving}
      onClose={onClose}
    >
      <form className={styles.form} onSubmit={handleSubmit}>
        {submitError ? <Alert variant="error">{submitError}</Alert> : null}

        <FormField id="overtime-employee" label="Empleado" error={errors.employeeId}>
          <select
            id="overtime-employee"
            name="employeeId"
            className={styles.input}
            value={values.employeeId}
            onChange={handleChange}
          >
            <option value="">Selecciona un empleado</option>
            {employees.map((employee) => (
              <option key={employee.employeeId} value={employee.employeeId}>
                {employee.firstName} {employee.lastName}
              </option>
            ))}
          </select>
        </FormField>

        <div className={styles.formGrid}>
          <FormField id="overtime-date" label="Fecha" error={errors.overtimeDate}>
            <input
              id="overtime-date"
              name="overtimeDate"
              type="date"
              className={styles.input}
              value={values.overtimeDate}
              onChange={handleChange}
            />
          </FormField>

          <FormField id="overtime-hours" label="Horas" error={errors.hours}>
            <input
              id="overtime-hours"
              name="hours"
              type="number"
              step="0.5"
              min="0.5"
              max="24"
              className={styles.input}
              value={values.hours}
              onChange={handleChange}
            />
          </FormField>
        </div>

        <FormField id="overtime-rule" label="Tipo" error={errors.overtimeRuleId}>
          <select
            id="overtime-rule"
            name="overtimeRuleId"
            className={styles.input}
            value={values.overtimeRuleId}
            onChange={handleChange}
          >
            {overtimeRules.map((rule) => (
              <option key={rule.overtimeRuleId} value={rule.overtimeRuleId}>
                {getOvertimeRuleLabel(rule.overtimeRuleCode, rule.name)}
                {' '}
                (x{formatNumber(rule.hourMultiplier)})
              </option>
            ))}
          </select>
        </FormField>

        <FormField id="overtime-notes" label="Notas (opcional)">
          <textarea
            id="overtime-notes"
            name="notes"
            maxLength={500}
            className={styles.textarea}
            value={values.notes}
            onChange={handleChange}
          />
        </FormField>

        <FormActions isSaving={isSaving} submitText="Registrar" onCancel={onClose} />
      </form>
    </PayrollModal>
  )
}

function RejectOvertimeModal({ record, onClose, onConfirm }) {
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()

    if (!reason.trim()) {
      setError('Indica el motivo del rechazo.')
      return
    }

    setIsSaving(true)
    await onConfirm(reason.trim())
  }

  return (
    <PayrollModal
      title="Rechazar horas extra"
      subtitle={`${record.firstName} ${record.lastName} · ${formatDate(record.overtimeDate)} · ${formatNumber(record.hours)} h`}
      isBusy={isSaving}
      onClose={onClose}
    >
      <form className={styles.form} onSubmit={handleSubmit}>
        <FormField id="overtime-reject-reason" label="Motivo" error={error}>
          <textarea
            id="overtime-reject-reason"
            maxLength={300}
            className={styles.textarea}
            value={reason}
            onChange={(event) => {
              setReason(event.target.value)
              setError('')
            }}
          />
        </FormField>
        <FormActions
          isSaving={isSaving}
          submitText="Rechazar"
          submitVariant="danger"
          onCancel={onClose}
        />
      </form>
    </PayrollModal>
  )
}
