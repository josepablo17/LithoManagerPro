import { ClipboardCheck, Plus, RefreshCcw, X } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Badge } from '../../../components/ui/Badge.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import styles from '../../../app/Page.module.css'
import { useAuth } from '../../security/hooks/useAuth.js'
import {
  formatCurrency,
  formatDate,
  formatPeriod,
  getRunTypeLabel,
  getStatusLabel,
  getStatusVariant,
  monthNames,
  payrollMutationRoles,
} from '../payrollFormat.js'
import {
  createPayrollRun,
  getPayrollRuns,
} from '../services/payrollRunService.js'
import payrollStyles from './Payroll.module.css'

export function PayrollRunsPage() {
  const { accessToken, user } = useAuth()
  const navigate = useNavigate()
  const currentYear = new Date().getFullYear()
  const [periodYear, setPeriodYear] = useState(currentYear)
  const [runs, setRuns] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [createModalOpen, setCreateModalOpen] = useState(false)

  const canManage = payrollMutationRoles.has(user?.roleCode)

  const loadRuns = useCallback(async ({ signal } = {}) => {
    setIsLoading(true)
    setLoadError('')

    try {
      const result = await getPayrollRuns({
        accessToken,
        periodYear,
        signal,
      })

      setRuns(result)
    } catch (error) {
      if (error.name === 'AbortError') {
        return
      }

      setLoadError(error.message)
    } finally {
      if (!signal?.aborted) {
        setIsLoading(false)
      }
    }
  }, [accessToken, periodYear])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadRuns({ signal: controller.signal })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadRuns])

  const yearOptions = [currentYear + 1, currentYear, currentYear - 1, currentYear - 2]

  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Planilla</h1>
          <p>
            Planillas mensuales y aguinaldo: cálculo de salarios, cargas
            sociales de la CCSS, impuesto sobre la renta y neto a pagar.
          </p>
        </div>
        <div className={payrollStyles.headerActions}>
          <Button
            variant="secondary"
            onClick={() => navigate('/payroll/operations')}
          >
            <ClipboardCheck size={18} aria-hidden="true" />
            Horas extra, incapacidades y horarios
          </Button>
          {canManage ? (
            <Button onClick={() => setCreateModalOpen(true)}>
              <Plus size={18} aria-hidden="true" />
              Nueva planilla
            </Button>
          ) : null}
        </div>
      </div>

      <div className={styles.toolbar}>
        <select
          className={styles.select}
          aria-label="Filtrar planillas por año"
          value={periodYear}
          onChange={(event) => setPeriodYear(Number(event.target.value))}
        >
          {yearOptions.map((year) => (
            <option key={year} value={year}>
              {year}
            </option>
          ))}
        </select>
        <Button
          variant="secondary"
          disabled={isLoading}
          onClick={() => loadRuns()}
        >
          <RefreshCcw size={18} aria-hidden="true" />
          Actualizar
        </Button>
      </div>

      {loadError ? <Alert variant="error">{loadError}</Alert> : null}

      <div className={styles.tableShell}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Período</th>
              <th>Estado</th>
              <th>Empleados</th>
              <th className={payrollStyles.amountHeader}>Bruto</th>
              <th className={payrollStyles.amountHeader}>Neto a pagar</th>
              <th className={payrollStyles.amountHeader}>Cargas patronales</th>
              <th>Pago</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan="8" className={payrollStyles.loadingCell}>
                  Cargando planillas
                </td>
              </tr>
            ) : null}

            {!isLoading && runs.length === 0 ? (
              <tr>
                <td colSpan="8" className={payrollStyles.emptyCell}>
                  No hay planillas en {periodYear}
                </td>
              </tr>
            ) : null}

            {!isLoading
              ? runs.map((run) => (
                <tr key={run.payrollRunId}>
                  <td>
                    <div className={payrollStyles.employeeCell}>
                      <strong>{formatPeriod(run)}</strong>
                      <span>{getRunTypeLabel(run.runType)}</span>
                    </div>
                  </td>
                  <td>
                    <Badge variant={getStatusVariant(run.status)}>
                      {getStatusLabel(run.status)}
                    </Badge>
                  </td>
                  <td>{run.employeeCount}</td>
                  <td className={payrollStyles.amount}>
                    {formatCurrency(run.totalGrossAmount)}
                  </td>
                  <td className={`${payrollStyles.amount} ${payrollStyles.netAmount}`}>
                    {formatCurrency(run.totalNetAmount)}
                  </td>
                  <td className={payrollStyles.amount}>
                    {formatCurrency(run.totalEmployerContributionsAmount)}
                  </td>
                  <td>{formatDate(run.paymentDate)}</td>
                  <td>
                    <div className={payrollStyles.actionsCell}>
                      <Link
                        className={styles.textLink}
                        to={`/payroll/runs/${run.payrollRunId}`}
                        aria-label={`Ver planilla ${formatPeriod(run)}`}
                      >
                        Ver
                      </Link>
                    </div>
                  </td>
                </tr>
              ))
              : null}
          </tbody>
        </table>
      </div>

      {createModalOpen ? (
        <CreatePayrollRunModal
          accessToken={accessToken}
          defaultYear={periodYear}
          onClose={() => setCreateModalOpen(false)}
          onCreated={(detail) => {
            setCreateModalOpen(false)
            navigate(`/payroll/runs/${detail.payrollRun.payrollRunId}`)
          }}
        />
      ) : null}
    </section>
  )
}

function CreatePayrollRunModal({
  accessToken,
  defaultYear,
  onClose,
  onCreated,
}) {
  const today = new Date()
  const [values, setValues] = useState({
    runType: 'Regular',
    periodYear: String(defaultYear),
    periodMonth: String(today.getMonth() + 1),
    paymentDate: '',
  })
  const [submitError, setSubmitError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  function handleChange(event) {
    const { name, value } = event.target

    setValues((current) => ({ ...current, [name]: value }))
    setSubmitError('')
  }

  function handleClose() {
    if (!isSaving) {
      onClose()
    }
  }

  async function handleSubmit(event) {
    event.preventDefault()

    const year = Number(values.periodYear)

    if (!Number.isInteger(year) || year < 2000 || year > 2100) {
      setSubmitError('Ingresa un año válido.')
      return
    }

    setIsSaving(true)
    setSubmitError('')

    try {
      const detail = await createPayrollRun({
        accessToken,
        runType: values.runType,
        periodYear: year,
        periodMonth: Number(values.periodMonth),
        paymentDate: values.paymentDate,
      })

      onCreated(detail)
    } catch (error) {
      setSubmitError(
        error.errorCode === 'duplicate_record'
          ? 'Ya existe una planilla para ese período.'
          : error.message,
      )
      setIsSaving(false)
    }
  }

  const isAguinaldo = values.runType === 'Aguinaldo'

  return (
    <div className={payrollStyles.modalOverlay}>
      <section
        className={payrollStyles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="payroll-create-title"
      >
        <div className={payrollStyles.modalHeader}>
          <div>
            <h2 id="payroll-create-title">Nueva planilla</h2>
            <p>Se crea abierta; después se calcula y se cierra.</p>
          </div>
          <Button
            variant="ghost"
            size="small"
            aria-label="Cerrar"
            disabled={isSaving}
            onClick={handleClose}
          >
            <X size={18} aria-hidden="true" />
          </Button>
        </div>

        <form
          className={`${payrollStyles.modalBody} ${payrollStyles.form}`}
          onSubmit={handleSubmit}
        >
          {submitError ? <Alert variant="error">{submitError}</Alert> : null}

          <div className={payrollStyles.field}>
            <label className={payrollStyles.label} htmlFor="payroll-run-type">
              Tipo
            </label>
            <select
              id="payroll-run-type"
              name="runType"
              className={payrollStyles.input}
              value={values.runType}
              onChange={handleChange}
            >
              <option value="Regular">Planilla mensual</option>
              <option value="Aguinaldo">Aguinaldo</option>
            </select>
            {isAguinaldo ? (
              <span className={payrollStyles.hint}>
                Suma los salarios de las planillas mensuales cerradas del 1 de
                diciembre del año anterior al 30 de noviembre, y los divide
                entre 12.
              </span>
            ) : null}
          </div>

          <div className={payrollStyles.formGrid}>
            <div className={payrollStyles.field}>
              <label className={payrollStyles.label} htmlFor="payroll-year">
                Año
              </label>
              <input
                id="payroll-year"
                name="periodYear"
                className={payrollStyles.input}
                type="number"
                min="2000"
                max="2100"
                value={values.periodYear}
                onChange={handleChange}
                required
              />
            </div>

            {!isAguinaldo ? (
              <div className={payrollStyles.field}>
                <label className={payrollStyles.label} htmlFor="payroll-month">
                  Mes
                </label>
                <select
                  id="payroll-month"
                  name="periodMonth"
                  className={payrollStyles.input}
                  value={values.periodMonth}
                  onChange={handleChange}
                >
                  {monthNames.map((name, index) => (
                    <option key={name} value={index + 1}>
                      {name[0].toUpperCase() + name.slice(1)}
                    </option>
                  ))}
                </select>
              </div>
            ) : null}
          </div>

          <div className={payrollStyles.field}>
            <label className={payrollStyles.label} htmlFor="payroll-payment-date">
              Fecha de pago (opcional)
            </label>
            <input
              id="payroll-payment-date"
              name="paymentDate"
              className={payrollStyles.input}
              type="date"
              value={values.paymentDate}
              onChange={handleChange}
            />
            <span className={payrollStyles.hint}>
              {isAguinaldo
                ? 'Si la dejas vacía se usa el 20 de diciembre, la fecha límite legal.'
                : 'Si la dejas vacía se usa el último día del mes.'}
            </span>
          </div>

          <div className={payrollStyles.formActions}>
            <Button
              variant="secondary"
              disabled={isSaving}
              onClick={handleClose}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              isLoading={isSaving}
              loadingText="Creando..."
            >
              Crear planilla
            </Button>
          </div>
        </form>
      </section>
    </div>
  )
}
