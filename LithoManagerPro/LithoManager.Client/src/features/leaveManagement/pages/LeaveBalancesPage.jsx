import {
  CheckCircle2,
  RefreshCcw,
  Search,
  SlidersHorizontal,
  X,
} from 'lucide-react'
import {
  useCallback,
  useEffect,
  useState,
} from 'react'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Badge } from '../../../components/ui/Badge.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import styles from '../../../app/Page.module.css'
import { getEmployees } from '../../humanResources/services/employeeService.js'
import { useAuth } from '../../security/hooks/useAuth.js'
import {
  adjustEmployeeLeaveBalance,
  getEmployeeLeaveBalance,
  getMyLeaveBalance,
} from '../services/leaveManagementService.js'
import balanceStyles from './LeaveBalancesPage.module.css'
import leaveStyles from './LeaveRequestsPage.module.css'

// Roles that can list employees (HumanResourcesEmployees policy) and
// adjust balances (LeaveManagementAdministrationMutation policy).
const managementRoles = new Set([
  'SuperAdministrator',
  'HumanResourcesAdministrator',
])

const maxAdjustmentDays = 365

export function LeaveBalancesPage() {
  const { accessToken, user } = useAuth()
  const canUseSelfService = Boolean(user?.employeeId)
  const canManage = managementRoles.has(user?.roleCode)
  const [activeView, setActiveView] = useState('my')

  const selectedView =
    canManage && !canUseSelfService
      ? 'employees'
      : activeView

  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Saldos de vacaciones</h1>
          <p>
            Consulta cómo se compone el saldo disponible
            {canManage ? ' y ajusta el de cada empleado.' : '.'}
          </p>
        </div>
      </div>

      {canManage && canUseSelfService ? (
        <div className={leaveStyles.tabs} role="tablist">
          <button
            className={
              selectedView === 'my'
                ? leaveStyles.tabActive
                : leaveStyles.tab
            }
            type="button"
            role="tab"
            aria-selected={selectedView === 'my'}
            onClick={() => setActiveView('my')}
          >
            Mi saldo
          </button>
          <button
            className={
              selectedView === 'employees'
                ? leaveStyles.tabActive
                : leaveStyles.tab
            }
            type="button"
            role="tab"
            aria-selected={selectedView === 'employees'}
            onClick={() => setActiveView('employees')}
          >
            Empleados
          </button>
        </div>
      ) : null}

      {!canUseSelfService && !canManage ? (
        <Alert variant="warning">
          Tu usuario no está vinculado a un empleado, por lo que no
          tiene saldo de vacaciones.
        </Alert>
      ) : null}

      {selectedView === 'my' && canUseSelfService ? (
        <MyBalanceView accessToken={accessToken} />
      ) : null}

      {selectedView === 'employees' && canManage ? (
        <EmployeeBalancesView accessToken={accessToken} />
      ) : null}
    </section>
  )
}

function MyBalanceView({ accessToken }) {
  const [balance, setBalance] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')

  const loadBalance = useCallback(async ({ signal } = {}) => {
    setIsLoading(true)
    setLoadError('')

    try {
      const result = await getMyLeaveBalance({
        accessToken,
        signal,
      })

      setBalance(result)
    } catch (error) {
      if (error.name === 'AbortError') {
        return
      }

      setLoadError(getBalanceErrorMessage(error))
    } finally {
      if (!signal?.aborted) {
        setIsLoading(false)
      }
    }
  }, [accessToken])

  useAbortableEffect(loadBalance)

  return (
    <>
      <div className={styles.toolbar}>
        <span className={leaveStyles.muted}>
          {balance
            ? `Última actualización: ${formatDateTime(
              balance.updatedAtUtc ?? balance.createdAtUtc,
            )}`
            : null}
        </span>
        <Button
          variant="secondary"
          disabled={isLoading}
          onClick={() => loadBalance()}
        >
          <RefreshCcw size={18} aria-hidden="true" />
          Actualizar
        </Button>
      </div>

      {loadError ? (
        <Alert variant="error">{loadError}</Alert>
      ) : null}

      {isLoading && !balance ? (
        <p className={leaveStyles.muted}>Cargando saldo</p>
      ) : null}

      {balance ? <BalanceDetail balance={balance} /> : null}
    </>
  )
}

function EmployeeBalancesView({ accessToken }) {
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearchTerm, setDebouncedSearchTerm] =
    useState('')
  const [employees, setEmployees] = useState([])
  const [isLoadingEmployees, setIsLoadingEmployees] =
    useState(true)
  const [employeesError, setEmployeesError] = useState('')
  const [selectedEmployee, setSelectedEmployee] = useState(null)
  const [balance, setBalance] = useState(null)
  const [isLoadingBalance, setIsLoadingBalance] = useState(false)
  const [balanceError, setBalanceError] = useState('')
  const [notice, setNotice] = useState('')
  const [adjustModalOpen, setAdjustModalOpen] = useState(false)

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearchTerm(searchTerm)
    }, 250)

    return () => window.clearTimeout(timeoutId)
  }, [searchTerm])

  const loadEmployees = useCallback(async ({ signal } = {}) => {
    setIsLoadingEmployees(true)
    setEmployeesError('')

    try {
      const result = await getEmployees({
        accessToken,
        searchTerm: debouncedSearchTerm,
        isActive: true,
        signal,
      })

      setEmployees(result)
    } catch (error) {
      if (error.name === 'AbortError') {
        return
      }

      setEmployeesError(error.message)
    } finally {
      if (!signal?.aborted) {
        setIsLoadingEmployees(false)
      }
    }
  }, [accessToken, debouncedSearchTerm])

  useAbortableEffect(loadEmployees)

  async function selectEmployee(employee) {
    setSelectedEmployee(employee)
    setBalance(null)
    setBalanceError('')
    setNotice('')
    setIsLoadingBalance(true)

    try {
      const result = await getEmployeeLeaveBalance({
        accessToken,
        employeeId: employee.employeeId,
      })

      setBalance(result)
    } catch (error) {
      setBalanceError(getBalanceErrorMessage(error))
    } finally {
      setIsLoadingBalance(false)
    }
  }

  async function handleAdjusted(adjustResult, delta) {
    // The adjustment endpoint returns only the balance counters (no
    // policy data), so reload the full balance and fall back to merging
    // the counters if that request fails.
    let updatedBalance

    try {
      updatedBalance = await getEmployeeLeaveBalance({
        accessToken,
        employeeId: adjustResult.employeeId,
      })
    } catch {
      updatedBalance = mergeBalanceCounters(balance, adjustResult)
    }

    setBalance(updatedBalance)
    setAdjustModalOpen(false)
    setNotice(
      `Saldo ajustado en ${formatSignedDays(delta)} `
      + `${Math.abs(delta) === 1 ? 'día' : 'días'}. `
      + `Disponibles ahora: ${formatDays(updatedBalance.availableDays)}.`,
    )
  }

  return (
    <>
      {selectedEmployee ? (
        <section
          className={balanceStyles.selectedPanel}
          aria-label="Saldo del empleado seleccionado"
        >
          <div className={balanceStyles.selectedHeader}>
            <div>
              <h2>
                {selectedEmployee.firstName}
                {' '}
                {selectedEmployee.lastName}
              </h2>
              <p>
                {selectedEmployee.departmentName}
                {' · '}
                {selectedEmployee.identificationNumber}
              </p>
            </div>
            <div className={balanceStyles.selectedActions}>
              <Button
                disabled={!balance || isLoadingBalance}
                onClick={() => {
                  setNotice('')
                  setAdjustModalOpen(true)
                }}
              >
                <SlidersHorizontal size={18} aria-hidden="true" />
                Ajustar saldo
              </Button>
              <Button
                variant="ghost"
                size="small"
                aria-label="Cerrar saldo del empleado"
                onClick={() => {
                  setSelectedEmployee(null)
                  setBalance(null)
                  setBalanceError('')
                  setNotice('')
                }}
              >
                <X size={18} aria-hidden="true" />
              </Button>
            </div>
          </div>

          {notice ? (
            <Alert variant="success">{notice}</Alert>
          ) : null}

          {balanceError ? (
            <Alert variant="error">{balanceError}</Alert>
          ) : null}

          {isLoadingBalance ? (
            <p className={leaveStyles.muted}>Cargando saldo</p>
          ) : null}

          {balance ? <BalanceDetail balance={balance} /> : null}
        </section>
      ) : null}

      <div className={styles.toolbar}>
        <div className={leaveStyles.searchBox}>
          <Search
            className={leaveStyles.searchIcon}
            size={18}
            aria-hidden="true"
          />
          <input
            className={leaveStyles.searchInput}
            type="search"
            placeholder="Buscar empleado"
            aria-label="Buscar empleado"
            maxLength={150}
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
          />
        </div>
        <Button
          variant="secondary"
          disabled={isLoadingEmployees}
          onClick={() => loadEmployees()}
        >
          <RefreshCcw size={18} aria-hidden="true" />
          Actualizar
        </Button>
      </div>

      {employeesError ? (
        <Alert variant="error">{employeesError}</Alert>
      ) : null}

      <div className={styles.tableShell}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Empleado</th>
              <th>Departamento</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {isLoadingEmployees ? (
              <tr>
                <td colSpan="3" className={leaveStyles.loadingCell}>
                  Cargando empleados
                </td>
              </tr>
            ) : null}

            {!isLoadingEmployees && employees.length === 0 ? (
              <tr>
                <td colSpan="3" className={leaveStyles.emptyCell}>
                  No hay empleados activos con ese criterio
                </td>
              </tr>
            ) : null}

            {!isLoadingEmployees
              ? employees.map((employee) => (
                <tr key={employee.employeeId}>
                  <td>
                    <div className={leaveStyles.employeeCell}>
                      <strong>
                        {employee.firstName} {employee.lastName}
                      </strong>
                      <span>{employee.identificationNumber}</span>
                    </div>
                  </td>
                  <td>{employee.departmentName}</td>
                  <td>
                    <div className={leaveStyles.actionsCell}>
                      {selectedEmployee?.employeeId
                        === employee.employeeId ? (
                          <Badge variant="neutral">Seleccionado</Badge>
                        ) : (
                          <Button
                            variant="secondary"
                            size="small"
                            aria-label={`Ver saldo de ${employee.firstName} ${employee.lastName}`}
                            onClick={() => selectEmployee(employee)}
                          >
                            Ver saldo
                          </Button>
                        )}
                    </div>
                  </td>
                </tr>
              ))
              : null}
          </tbody>
        </table>
      </div>

      {adjustModalOpen && balance ? (
        <AdjustBalanceModal
          accessToken={accessToken}
          balance={balance}
          onClose={() => setAdjustModalOpen(false)}
          onAdjusted={handleAdjusted}
        />
      ) : null}
    </>
  )
}

function BalanceDetail({ balance }) {
  return (
    <div className={balanceStyles.detail}>
      <div className={leaveStyles.summaryBar}>
        <SummaryItem
          label="Disponibles"
          value={formatDays(balance.availableDays)}
          highlight
        />
        <SummaryItem
          label="Pendientes de aprobación"
          value={formatDays(balance.pendingDays)}
        />
        <SummaryItem
          label="Usados"
          value={formatDays(balance.usedDays)}
        />
        <SummaryItem
          label="Derecho anual"
          value={formatDays(balance.entitlementDays)}
        />
      </div>

      <section className={styles.section}>
        <h2>Cómo se calcula</h2>
        <dl
          className={balanceStyles.breakdown}
          aria-label="Desglose del saldo"
        >
          <BreakdownRow
            label="Acumulados"
            value={formatDays(balance.accruedDays)}
          />
          <BreakdownRow
            label="Ajustes manuales"
            value={formatSignedDays(balance.adjustedDays)}
          />
          <BreakdownRow
            label="Pendientes de aprobación"
            value={formatSignedDays(-Number(balance.pendingDays))}
          />
          <BreakdownRow
            label="Usados"
            value={formatSignedDays(-Number(balance.usedDays))}
          />
          <BreakdownRow
            label="Disponibles"
            value={formatDays(balance.availableDays)}
            isTotal
          />
        </dl>

        <p className={balanceStyles.policy}>
          {balance.leavePolicyName
            ? `Política ${balance.leavePolicyName} · `
            : ''}
          {formatDays(balance.entitlementDays)}
          {' días por cada '}
          {balance.entitlementWeeks}
          {' semanas de trabajo continuo · se cuentan días '}
          {balance.usesBusinessDays ? 'hábiles' : 'naturales'}
        </p>
      </section>
    </div>
  )
}

function SummaryItem({ label, value, highlight = false }) {
  return (
    <div
      className={
        highlight
          ? `${leaveStyles.summaryItem} ${balanceStyles.highlight}`
          : leaveStyles.summaryItem
      }
    >
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function BreakdownRow({ label, value, isTotal = false }) {
  return (
    <div
      className={
        isTotal
          ? `${balanceStyles.breakdownRow} ${balanceStyles.breakdownTotal}`
          : balanceStyles.breakdownRow
      }
    >
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

function AdjustBalanceModal({
  accessToken,
  balance,
  onClose,
  onAdjusted,
}) {
  const [deltaText, setDeltaText] = useState('')
  const [fieldError, setFieldError] = useState('')
  const [submitError, setSubmitError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  const parsedDelta = parseDelta(deltaText)
  const projectedAvailable = parsedDelta === null
    ? null
    : roundDays(Number(balance.availableDays) + parsedDelta)

  function handleClose() {
    if (!isSaving) {
      onClose()
    }
  }

  async function handleSubmit(event) {
    event.preventDefault()

    const validationError =
      validateAdjustment(deltaText, balance)

    if (validationError) {
      setFieldError(validationError)
      return
    }

    const delta = parseDelta(deltaText)

    setIsSaving(true)
    setSubmitError('')

    try {
      const updatedBalance = await adjustEmployeeLeaveBalance({
        accessToken,
        employeeId: balance.employeeId,
        leaveTypeCode: balance.leaveTypeCode,
        adjustedDaysDelta: delta,
      })

      await onAdjusted(updatedBalance, delta)
    } catch (error) {
      setSubmitError(getBalanceErrorMessage(error))
      setIsSaving(false)
    }
  }

  return (
    <div className={leaveStyles.modalOverlay}>
      <section
        className={`${leaveStyles.modal} ${leaveStyles.confirmModal}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="leave-balance-adjust-title"
      >
        <div className={leaveStyles.modalHeader}>
          <div>
            <h2 id="leave-balance-adjust-title">Ajustar saldo</h2>
            <p>
              Disponibles hoy: {formatDays(balance.availableDays)}
            </p>
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

        <form className={leaveStyles.form} onSubmit={handleSubmit}>
          {submitError ? (
            <Alert variant="error">{submitError}</Alert>
          ) : null}

          <div className={leaveStyles.field}>
            <label
              className={leaveStyles.label}
              htmlFor="leave-balance-delta"
            >
              Días a sumar o restar
            </label>
            <input
              id="leave-balance-delta"
              className={leaveStyles.dateInput}
              type="number"
              inputMode="decimal"
              step="0.5"
              min={-maxAdjustmentDays}
              max={maxAdjustmentDays}
              placeholder="Ej. 2 o -1.5"
              value={deltaText}
              aria-invalid={Boolean(fieldError)}
              aria-describedby="leave-balance-delta-help"
              onChange={(event) => {
                setDeltaText(event.target.value)
                setFieldError('')
              }}
            />
            {fieldError ? (
              <span className={leaveStyles.errorText}>
                {fieldError}
              </span>
            ) : (
              <span
                id="leave-balance-delta-help"
                className={leaveStyles.muted}
              >
                Usa un número negativo para descontar días.
              </span>
            )}
          </div>

          {projectedAvailable !== null && parsedDelta !== 0 ? (
            <p
              className={
                projectedAvailable < 0
                  ? `${balanceStyles.projection} ${balanceStyles.projectionNegative}`
                  : balanceStyles.projection
              }
            >
              Disponibles después del ajuste:
              {' '}
              <strong>{formatDays(projectedAvailable)}</strong>
            </p>
          ) : null}

          <div className={leaveStyles.formActions}>
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
              loadingText="Guardando..."
            >
              <CheckCircle2 size={18} aria-hidden="true" />
              Aplicar ajuste
            </Button>
          </div>
        </form>
      </section>
    </div>
  )
}

function useAbortableEffect(load) {
  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      load({ signal: controller.signal })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [load])
}

function validateAdjustment(deltaText, balance) {
  const delta = parseDelta(deltaText)

  if (delta === null) {
    return 'Ingresa la cantidad de días.'
  }

  if (delta === 0) {
    return 'El ajuste no puede ser cero.'
  }

  if (Math.abs(delta) > maxAdjustmentDays) {
    return `El ajuste no puede superar ${maxAdjustmentDays} días.`
  }

  if (!/^-?\d+(\.\d{1,2})?$/.test(deltaText.trim())) {
    return 'Usa como máximo dos decimales.'
  }

  const projectedAvailable =
    roundDays(Number(balance.availableDays) + delta)

  if (projectedAvailable < 0) {
    return 'El saldo disponible no puede quedar negativo.'
  }

  return ''
}

function mergeBalanceCounters(currentBalance, adjustResult) {
  return {
    ...currentBalance,
    accruedDays: adjustResult.accruedDays,
    adjustedDays: adjustResult.adjustedDays,
    pendingDays: adjustResult.pendingDays,
    usedDays: adjustResult.usedDays,
    availableDays: adjustResult.availableDays,
    updatedAtUtc: adjustResult.updatedAtUtc,
    rowVersion: adjustResult.rowVersion,
  }
}

function parseDelta(deltaText) {
  const normalized = deltaText?.trim()

  if (!normalized) {
    return null
  }

  const value = Number(normalized)

  return Number.isFinite(value) ? value : null
}

function getBalanceErrorMessage(error) {
  const errorCode = error?.errorCode
    ?? error?.details?.errorCode
    ?? error?.details?.extensions?.errorCode
    ?? ''

  if (errorCode === 'leave_balance_not_found') {
    return 'Este empleado todavía no tiene un saldo de vacaciones registrado.'
  }

  if (errorCode === 'insufficient_leave_balance') {
    return 'El ajuste dejaría el saldo disponible en negativo.'
  }

  return error?.message
    ?? 'No pudimos cargar el saldo. Inténtalo de nuevo.'
}

function roundDays(value) {
  return Math.round(value * 100) / 100
}

function formatDays(value) {
  if (value === null || value === undefined) {
    return '-'
  }

  return new Intl.NumberFormat('es-CR', {
    maximumFractionDigits: 2,
  }).format(Number(value))
}

function formatSignedDays(value) {
  const number = Number(value ?? 0)
  const formatted = formatDays(Math.abs(number))

  if (number > 0) {
    return `+${formatted}`
  }

  if (number < 0) {
    return `−${formatted}`
  }

  return formatted
}

function formatDateTime(value) {
  if (!value) {
    return 'Sin cambios'
  }

  return new Intl.DateTimeFormat('es-CR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
