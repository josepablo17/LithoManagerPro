import { ArrowLeft, Printer } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import { useAuth } from '../../security/hooks/useAuth.js'
import {
  describeLine,
  formatCurrency,
  formatDate,
  formatNumber,
  formatPeriod,
  getStatusLabel,
} from '../payrollFormat.js'
import {
  getPayrollRun,
  getPayrollRunEmployeeLines,
} from '../services/payrollRunService.js'
import styles from './Payslip.module.css'

/**
 * Printable pay slip of one employee in one payroll run. Printing uses
 * the browser ("Guardar como PDF"); the print styles hide the app chrome.
 */
export function PayslipPage() {
  const { payrollRunId, employeeId } = useParams()
  const { accessToken } = useAuth()
  const [data, setData] = useState(null)
  const [loadError, setLoadError] = useState('')
  const [showEmployerContributions, setShowEmployerContributions] =
    useState(false)

  useEffect(() => {
    const controller = new AbortController()
    const { signal } = controller

    Promise.all([
      getPayrollRun({ accessToken, payrollRunId, signal }),
      getPayrollRunEmployeeLines({
        accessToken,
        payrollRunId,
        employeeId,
        signal,
      }),
    ])
      .then(([detail, lines]) => {
        const employee = detail.employees.find(
          (item) => String(item.employeeId) === String(employeeId),
        )

        if (!employee) {
          setLoadError('El empleado no está en esta planilla.')
          return
        }

        setData({ run: detail.payrollRun, employee, lines })
      })
      .catch((error) => {
        if (error.name !== 'AbortError') {
          setLoadError(error.message)
        }
      })

    return () => controller.abort()
  }, [accessToken, employeeId, payrollRunId])

  const backLink = (
    <Link className={styles.backLink} to={`/payroll/runs/${payrollRunId}`}>
      <ArrowLeft size={16} aria-hidden="true" />
      Volver a la planilla
    </Link>
  )

  if (loadError) {
    return (
      <section className={styles.page}>
        {backLink}
        <Alert variant="error">{loadError}</Alert>
      </section>
    )
  }

  if (!data) {
    return (
      <section className={styles.page}>
        <p className={styles.muted}>Cargando boleta</p>
      </section>
    )
  }

  const { run, employee, lines } = data
  const isAguinaldo = run.runType === 'Aguinaldo'
  const earnings = lines.filter((line) => line.conceptKind === 'Earning')
  const otherPayments = lines.filter((line) => line.conceptKind === 'Informational')
  const deductions = lines.filter((line) => line.conceptKind === 'Deduction')
  const employerContributions = lines.filter(
    (line) => line.conceptKind === 'EmployerContribution',
  )

  const totalEarnings = sum(earnings) + sum(otherPayments)
  const totalDeductions = sum(deductions)

  return (
    <section className={styles.page}>
      <div className={styles.toolbar}>
        {backLink}
        <div className={styles.toolbarActions}>
          {employerContributions.length > 0 ? (
            <label className={styles.checkbox}>
              <input
                type="checkbox"
                checked={showEmployerContributions}
                onChange={(event) =>
                  setShowEmployerContributions(event.target.checked)}
              />
              Incluir aportes patronales
            </label>
          ) : null}
          <Button onClick={() => window.print()}>
            <Printer size={18} aria-hidden="true" />
            Imprimir o guardar PDF
          </Button>
        </div>
      </div>

      <article className={styles.slip} aria-label="Boleta de pago">
        {run.status !== 'Closed' ? (
          <p className={styles.draft} role="note">
            Borrador: la planilla está {getStatusLabel(run.status).toLowerCase()} y
            los montos todavía pueden cambiar.
          </p>
        ) : null}

        <header className={styles.header}>
          <div className={styles.brand}>
            <div className={styles.brandMark} aria-hidden="true">LM</div>
            <div>
              <strong>LithoManagerPro</strong>
              <span>Comprobante de pago</span>
            </div>
          </div>
          <div className={styles.period}>
            <strong>{isAguinaldo ? formatPeriod(run) : `Planilla de ${formatPeriod(run).toLowerCase()}`}</strong>
            <span>
              {formatDate(run.periodStartDate)} al {formatDate(run.periodEndDate)}
            </span>
            <span>Fecha de pago: {formatDate(run.paymentDate)}</span>
          </div>
        </header>

        <dl className={styles.employee}>
          <Field label="Colaborador" value={employee.employeeName} />
          <Field label="Identificación" value={employee.identificationNumber} />
          <Field label="Departamento" value={employee.departmentName ?? '-'} />
          <Field label="Puesto" value={employee.jobTitle ?? '-'} />
          <Field label="Salario mensual" value={formatCurrency(employee.monthlyBaseSalary)} />
          {!isAguinaldo ? (
            <Field label="Días pagados" value={formatNumber(employee.paidDays)} />
          ) : null}
        </dl>

        <LineSection title="Ingresos" lines={[...earnings, ...otherPayments]} total={totalEarnings} />

        {deductions.length > 0 ? (
          <LineSection
            title="Deducciones"
            lines={deductions}
            total={totalDeductions}
            negative
          />
        ) : null}

        <div className={styles.net}>
          <span>Neto a pagar</span>
          <strong>{formatCurrency(employee.netAmount)}</strong>
        </div>

        {showEmployerContributions && employerContributions.length > 0 ? (
          <LineSection
            title="Aportes patronales (pagados por la empresa, no se rebajan del salario)"
            lines={employerContributions}
            total={sum(employerContributions)}
          />
        ) : null}

        <footer className={styles.footer}>
          <span>
            Planilla #{run.payrollRunId} · {getStatusLabel(run.status)}
            {run.closedAtUtc ? ` el ${formatDate(run.closedAtUtc)}` : ''}
          </span>
          <span>
            {isAguinaldo
              ? 'El aguinaldo no está sujeto a cargas sociales ni a impuesto sobre la renta.'
              : 'Las cargas sociales se reportan a la CCSS según la ley.'}
          </span>
        </footer>
      </article>
    </section>
  )
}

function Field({ label, value }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

function LineSection({ title, lines, total, negative = false }) {
  return (
    <section className={styles.section}>
      <h2>{title}</h2>
      <table className={styles.lines}>
        <thead>
          <tr>
            <th>Concepto</th>
            <th>Detalle</th>
            <th className={styles.amount}>Monto</th>
          </tr>
        </thead>
        <tbody>
          {lines.map((line) => (
            <tr key={line.payrollRunLineId}>
              <td>{line.description}</td>
              <td className={styles.muted}>{describeLine(line)}</td>
              <td className={styles.amount}>
                {negative ? `-${formatCurrency(line.amount)}` : formatCurrency(line.amount)}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <td colSpan="2">Total</td>
            <td className={styles.amount}>
              {negative ? `-${formatCurrency(total)}` : formatCurrency(total)}
            </td>
          </tr>
        </tfoot>
      </table>
    </section>
  )
}

function sum(lines) {
  return lines.reduce((total, line) => total + Number(line.amount), 0)
}
