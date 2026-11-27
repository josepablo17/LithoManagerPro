import {
  ArrowLeft,
  Calculator,
  FileText,
  Lock,
  X,
} from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Badge } from '../../../components/ui/Badge.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import styles from '../../../app/Page.module.css'
import { useAuth } from '../../security/hooks/useAuth.js'
import {
  describeLine,
  payslipPath,
  formatCurrency,
  formatDate,
  formatNumber,
  formatPeriod,
  getStatusLabel,
  getStatusVariant,
  payrollMutationRoles,
} from '../payrollFormat.js'
import {
  calculatePayrollRun,
  closePayrollRun,
  getPayrollRun,
  getPayrollRunEmployeeLines,
} from '../services/payrollRunService.js'
import payrollStyles from './Payroll.module.css'

export function PayrollRunDetailPage() {
  const { payrollRunId } = useParams()
  const { accessToken, user } = useAuth()
  const [detail, setDetail] = useState(null)
  const [warnings, setWarnings] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [actionError, setActionError] = useState('')
  const [notice, setNotice] = useState('')
  const [isCalculating, setIsCalculating] = useState(false)
  const [closeModalOpen, setCloseModalOpen] = useState(false)
  const [selectedEmployee, setSelectedEmployee] = useState(null)

  const canManage = payrollMutationRoles.has(user?.roleCode)

  const loadDetail = useCallback(async ({ signal } = {}) => {
    setIsLoading(true)
    setLoadError('')

    try {
      const result = await getPayrollRun({
        accessToken,
        payrollRunId,
        signal,
      })

      setDetail(result)
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
  }, [accessToken, payrollRunId])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadDetail({ signal: controller.signal })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadDetail])

  async function handleCalculate() {
    setIsCalculating(true)
    setActionError('')
    setNotice('')

    try {
      const result = await calculatePayrollRun({
        accessToken,
        payrollRunId,
        expectedRowVersion: detail.payrollRun.rowVersion,
      })

      setDetail(result)
      setWarnings(result.warnings ?? [])
      setNotice(
        `Planilla calculada para ${result.payrollRun.employeeCount} `
        + `${result.payrollRun.employeeCount === 1 ? 'empleado' : 'empleados'}.`,
      )
    } catch (error) {
      await handleMutationError(error)
    } finally {
      setIsCalculating(false)
    }
  }

  async function handleClose() {
    setActionError('')
    setNotice('')

    try {
      const result = await closePayrollRun({
        accessToken,
        payrollRunId,
        expectedRowVersion: detail.payrollRun.rowVersion,
      })

      setDetail(result)
      setWarnings([])
      setCloseModalOpen(false)
      setNotice('Planilla cerrada. Ya no se puede recalcular.')
    } catch (error) {
      setCloseModalOpen(false)
      await handleMutationError(error)
    }
  }

  async function handleMutationError(error) {
    if (error.errorCode === 'concurrency_conflict') {
      await loadDetail()
      setActionError(
        'La planilla fue modificada en otra sesión. Actualizamos los datos; revisa e inténtalo de nuevo.',
      )
      return
    }

    if (error.errorCode === 'configuration_not_found') {
      setActionError(
        'Falta configuración para calcular (conceptos de planilla o regla de aguinaldo activa).',
      )
      return
    }

    setActionError(error.message)
  }

  if (isLoading && !detail) {
    return (
      <section className={styles.page}>
        <p className={payrollStyles.meta}>Cargando planilla</p>
      </section>
    )
  }

  if (!detail) {
    return (
      <section className={styles.page}>
        <BackLink />
        <Alert variant="error">{loadError || 'No encontramos la planilla.'}</Alert>
      </section>
    )
  }

  const run = detail.payrollRun
  const employees = detail.employees
  const isAguinaldo = run.runType === 'Aguinaldo'
  const isClosed = run.status === 'Closed'

  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <BackLink />
          <div className={payrollStyles.titleRow}>
            <h1>{formatPeriod(run)}</h1>
            <Badge variant={getStatusVariant(run.status)}>
              {getStatusLabel(run.status)}
            </Badge>
          </div>
          <p>
            {isAguinaldo ? 'Salarios del ' : 'Período del '}
            {formatDate(run.periodStartDate)}
            {' al '}
            {formatDate(run.periodEndDate)}
            {' · Pago el '}
            {formatDate(run.paymentDate)}
          </p>
          {run.closedAtUtc ? (
            <p className={payrollStyles.meta}>
              Cerrada por {run.closedByEmailAddress}
            </p>
          ) : null}
          {!run.closedAtUtc && run.calculatedAtUtc ? (
            <p className={payrollStyles.meta}>
              Calculada por {run.calculatedByEmailAddress}
            </p>
          ) : null}
        </div>

        {canManage && !isClosed ? (
          <div className={payrollStyles.headerActions}>
            <Button
              variant={run.status === 'Calculated' ? 'secondary' : 'primary'}
              isLoading={isCalculating}
              loadingText="Calculando..."
              onClick={handleCalculate}
            >
              <Calculator size={18} aria-hidden="true" />
              {run.status === 'Calculated' ? 'Recalcular' : 'Calcular'}
            </Button>
            {run.status === 'Calculated' ? (
              <Button
                disabled={isCalculating}
                onClick={() => setCloseModalOpen(true)}
              >
                <Lock size={18} aria-hidden="true" />
                Cerrar planilla
              </Button>
            ) : null}
          </div>
        ) : null}
      </div>

      {notice ? <Alert variant="success">{notice}</Alert> : null}
      {actionError ? <Alert variant="error">{actionError}</Alert> : null}

      {warnings.length > 0 ? (
        <Alert variant="warning">
          <strong>Revisa antes de cerrar:</strong>
          <ul className={payrollStyles.warningList}>
            {warnings.map((warning) => (
              <li key={warning}>{warning}</li>
            ))}
          </ul>
        </Alert>
      ) : null}

      <div className={payrollStyles.summaryBar}>
        <SummaryItem label="Salario bruto" value={formatCurrency(run.totalGrossAmount)} />
        {!isAguinaldo ? (
          <>
            <SummaryItem
              label="Cargas obreras"
              value={formatCurrency(run.totalEmployeeContributionsAmount)}
            />
            <SummaryItem
              label="Impuesto sobre la renta"
              value={formatCurrency(run.totalIncomeTaxAmount)}
            />
          </>
        ) : null}
        <SummaryItem
          label="Neto a pagar"
          value={formatCurrency(run.totalNetAmount)}
          highlight
        />
        {!isAguinaldo ? (
          <>
            <SummaryItem
              label="Cargas patronales"
              value={formatCurrency(run.totalEmployerContributionsAmount)}
            />
            <SummaryItem
              label="Costo total para la empresa"
              value={formatCurrency(
                Number(run.totalGrossAmount)
                + Number(run.totalEmployerContributionsAmount),
              )}
            />
          </>
        ) : null}
      </div>

      {run.status === 'Open' ? (
        <Alert variant="info">
          Esta planilla todavía no se ha calculado.
          {canManage ? ' Usa “Calcular” para generar los montos.' : ''}
        </Alert>
      ) : null}

      <div className={styles.tableShell}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Empleado</th>
              {!isAguinaldo ? <th>Días</th> : null}
              <th className={payrollStyles.amountHeader}>Bruto</th>
              {!isAguinaldo ? (
                <>
                  <th className={payrollStyles.amountHeader}>Cargas obreras</th>
                  <th className={payrollStyles.amountHeader}>Renta</th>
                </>
              ) : null}
              <th className={payrollStyles.amountHeader}>Neto</th>
              {!isAguinaldo ? (
                <th className={payrollStyles.amountHeader}>Patronales</th>
              ) : null}
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {employees.length === 0 ? (
              <tr>
                <td colSpan="8" className={payrollStyles.emptyCell}>
                  {run.status === 'Open'
                    ? 'Sin resultados todavía'
                    : 'Ningún empleado tiene montos en este período'}
                </td>
              </tr>
            ) : null}

            {employees.map((employee) => (
              <tr key={employee.employeeId}>
                <td>
                  <div className={payrollStyles.employeeCell}>
                    <strong>{employee.employeeName}</strong>
                    <span>
                      {employee.departmentName}
                      {' · '}
                      {employee.identificationNumber}
                    </span>
                  </div>
                </td>
                {!isAguinaldo ? <td>{formatNumber(employee.paidDays)}</td> : null}
                <td className={payrollStyles.amount}>
                  {formatCurrency(employee.grossAmount)}
                </td>
                {!isAguinaldo ? (
                  <>
                    <td className={payrollStyles.amount}>
                      {formatCurrency(employee.employeeContributionsAmount)}
                    </td>
                    <td className={payrollStyles.amount}>
                      {formatCurrency(employee.incomeTaxAmount)}
                    </td>
                  </>
                ) : null}
                <td className={`${payrollStyles.amount} ${payrollStyles.netAmount}`}>
                  {formatCurrency(employee.netAmount)}
                </td>
                {!isAguinaldo ? (
                  <td className={payrollStyles.amount}>
                    {formatCurrency(employee.employerContributionsAmount)}
                  </td>
                ) : null}
                <td>
                  <div className={`${payrollStyles.actionsCell} ${payrollStyles.rowActions}`}>
                    <Button
                      variant="secondary"
                      size="small"
                      aria-label={`Ver detalle de ${employee.employeeName}`}
                      onClick={() => setSelectedEmployee(employee)}
                    >
                      Detalle
                    </Button>
                    <Link
                      className={payrollStyles.slipLink}
                      to={payslipPath(run.payrollRunId, employee.employeeId)}
                      aria-label={`Boleta de ${employee.employeeName}`}
                    >
                      <FileText size={16} aria-hidden="true" />
                      Boleta
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {closeModalOpen ? (
        <ClosePayrollModal
          run={run}
          onCancel={() => setCloseModalOpen(false)}
          onConfirm={handleClose}
        />
      ) : null}

      {selectedEmployee ? (
        <EmployeeLinesModal
          accessToken={accessToken}
          run={run}
          employee={selectedEmployee}
          onClose={() => setSelectedEmployee(null)}
        />
      ) : null}
    </section>
  )
}

function BackLink() {
  return (
    <Link className={payrollStyles.backLink} to="/payroll">
      <ArrowLeft size={16} aria-hidden="true" />
      Planillas
    </Link>
  )
}

function SummaryItem({ label, value, highlight = false }) {
  return (
    <div
      className={
        highlight
          ? `${payrollStyles.summaryItem} ${payrollStyles.summaryHighlight}`
          : payrollStyles.summaryItem
      }
    >
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function ClosePayrollModal({ run, onCancel, onConfirm }) {
  const [isSaving, setIsSaving] = useState(false)

  async function handleConfirm() {
    setIsSaving(true)
    await onConfirm()
    setIsSaving(false)
  }

  return (
    <div className={payrollStyles.modalOverlay}>
      <section
        className={payrollStyles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="payroll-close-title"
      >
        <div className={payrollStyles.modalHeader}>
          <div>
            <h2 id="payroll-close-title">Cerrar planilla</h2>
            <p>{formatPeriod(run)} · {formatCurrency(run.totalNetAmount)} a pagar</p>
          </div>
        </div>
        <div className={payrollStyles.modalBody}>
          <Alert variant="warning">
            Una planilla cerrada es definitiva: no se puede recalcular y sus
            salarios pasan a contar para el aguinaldo.
          </Alert>
          <div className={payrollStyles.formActions}>
            <Button variant="secondary" disabled={isSaving} onClick={onCancel}>
              Volver
            </Button>
            <Button
              isLoading={isSaving}
              loadingText="Cerrando..."
              onClick={handleConfirm}
            >
              <Lock size={18} aria-hidden="true" />
              Cerrar planilla
            </Button>
          </div>
        </div>
      </section>
    </div>
  )
}

const lineGroups = [
  { kind: 'Earning', title: 'Ingresos' },
  { kind: 'Informational', title: 'Otros pagos (no son salario)' },
  { kind: 'Deduction', title: 'Deducciones' },
  { kind: 'EmployerContribution', title: 'Aportes patronales (no se rebajan del salario)' },
]

function EmployeeLinesModal({
  accessToken,
  run,
  employee,
  onClose,
}) {
  const [lines, setLines] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')

  useEffect(() => {
    const controller = new AbortController()

    getPayrollRunEmployeeLines({
      accessToken,
      payrollRunId: run.payrollRunId,
      employeeId: employee.employeeId,
      signal: controller.signal,
    })
      .then((result) => {
        setLines(result)
        setIsLoading(false)
      })
      .catch((error) => {
        if (error.name !== 'AbortError') {
          setLoadError(error.message)
          setIsLoading(false)
        }
      })

    return () => controller.abort()
  }, [accessToken, employee.employeeId, run.payrollRunId])

  return (
    <div className={payrollStyles.modalOverlay}>
      <section
        className={`${payrollStyles.modal} ${payrollStyles.wideModal}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="payroll-lines-title"
      >
        <div className={payrollStyles.modalHeader}>
          <div>
            <h2 id="payroll-lines-title">{employee.employeeName}</h2>
            <p>
              {formatPeriod(run)}
              {' · Salario mensual '}
              {formatCurrency(employee.monthlyBaseSalary)}
            </p>
          </div>
          <Button variant="ghost" size="small" aria-label="Cerrar" onClick={onClose}>
            <X size={18} aria-hidden="true" />
          </Button>
        </div>

        <div className={payrollStyles.modalBody}>
          {loadError ? <Alert variant="error">{loadError}</Alert> : null}
          {isLoading ? <p className={payrollStyles.meta}>Cargando detalle</p> : null}

          {!isLoading
            ? lineGroups.map((group) => {
              const groupLines = lines.filter((line) => line.conceptKind === group.kind)

              if (groupLines.length === 0) {
                return null
              }

              const total = groupLines.reduce(
                (sum, line) => sum + Number(line.amount),
                0,
              )

              return (
                <section key={group.kind} className={payrollStyles.lineGroup}>
                  <h3>{group.title}</h3>
                  <table className={payrollStyles.lineTable}>
                    <thead>
                      <tr>
                        <th>Concepto</th>
                        <th>Detalle</th>
                        <th className={payrollStyles.amountHeader}>Monto</th>
                      </tr>
                    </thead>
                    <tbody>
                      {groupLines.map((line) => (
                        <tr key={line.payrollRunLineId}>
                          <td>{line.description}</td>
                          <td className={payrollStyles.meta}>{describeLine(line)}</td>
                          <td className={payrollStyles.amount}>
                            {formatCurrency(line.amount)}
                          </td>
                        </tr>
                      ))}
                      {groupLines.length > 1 ? (
                        <tr className={payrollStyles.lineTotal}>
                          <td colSpan="2">Total</td>
                          <td className={payrollStyles.amount}>{formatCurrency(total)}</td>
                        </tr>
                      ) : null}
                    </tbody>
                  </table>
                </section>
              )
            })
            : null}

          {!isLoading && !loadError ? (
            <>
              <div className={payrollStyles.netRow}>
                <span>Neto a pagar</span>
                <span>{formatCurrency(employee.netAmount)}</span>
              </div>
              <div className={payrollStyles.formActions}>
                <Link
                  className={payrollStyles.slipLink}
                  to={payslipPath(run.payrollRunId, employee.employeeId)}
                >
                  <FileText size={16} aria-hidden="true" />
                  Ver boleta de pago
                </Link>
              </div>
            </>
          ) : null}
        </div>
      </section>
    </div>
  )
}

