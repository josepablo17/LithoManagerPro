import {
  CalendarPlus,
  CheckCircle2,
  RefreshCcw,
  Search,
  ThumbsDown,
  ThumbsUp,
  X,
  XCircle,
} from 'lucide-react'
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Badge } from '../../../components/ui/Badge.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import styles from '../../../app/Page.module.css'
import { useAuth } from '../../security/hooks/useAuth.js'
import {
  cancelLeaveRequest,
  createLeaveRequest,
  getLeaveRequests,
  getLeaveRequestStatuses,
  getMyLeaveBalance,
  getMyLeaveRequests,
  respondLeaveRequest,
} from '../services/leaveManagementService.js'
import leaveStyles from './LeaveRequestsPage.module.css'

const emptyForm = {
  startDate: '',
  endDate: '',
}

const administrationRoles = new Set([
  'SuperAdministrator',
  'HumanResourcesAdministrator',
  'HumanResourcesStaff',
])

const mutationRoles = new Set([
  'SuperAdministrator',
  'HumanResourcesAdministrator',
])

export function LeaveRequestsPage() {
  const { accessToken, user } = useAuth()
  const [activeView, setActiveView] = useState('my')
  const [statuses, setStatuses] = useState([])
  const [balance, setBalance] = useState(null)
  const [balanceError, setBalanceError] = useState('')
  const [myRequests, setMyRequests] = useState([])
  const [adminRequests, setAdminRequests] = useState([])
  const [myStatusFilter, setMyStatusFilter] = useState('')
  const [adminStatusFilter, setAdminStatusFilter] =
    useState('Pending')
  const [adminSearchTerm, setAdminSearchTerm] = useState('')
  const [debouncedAdminSearchTerm, setDebouncedAdminSearchTerm] =
    useState('')
  const [startDateFrom, setStartDateFrom] = useState('')
  const [startDateTo, setStartDateTo] = useState('')
  const [isLoadingMyData, setIsLoadingMyData] = useState(true)
  const [isLoadingAdminData, setIsLoadingAdminData] =
    useState(false)
  const [loadError, setLoadError] = useState('')
  const [formValues, setFormValues] = useState(emptyForm)
  const [formErrors, setFormErrors] = useState({})
  const [createModalOpen, setCreateModalOpen] = useState(false)
  const [submitError, setSubmitError] = useState('')
  const [actionError, setActionError] = useState('')
  const [actionNotice, setActionNotice] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [cancelRequestTarget, setCancelRequestTarget] =
    useState(null)
  const [responseTarget, setResponseTarget] = useState(null)

  const canViewAdministration =
    administrationRoles.has(user?.roleCode)

  const canRespond =
    mutationRoles.has(user?.roleCode)

  const canUseSelfService = Boolean(user?.employeeId)
  const selectedView =
    canViewAdministration && !canUseSelfService
      ? 'admin'
      : activeView

  const loadReferenceData = useCallback(async ({
    signal,
  } = {}) => {
    setBalanceError('')

    try {
      const balanceRequest = canUseSelfService
        ? getMyLeaveBalance({
            accessToken,
            signal,
          }).catch((error) => {
            if (error.name === 'AbortError') {
              throw error
            }

            setBalanceError(error.message)
            return null
          })
        : Promise.resolve(null)

      const [statusesResult, balanceResult] =
        await Promise.all([
          getLeaveRequestStatuses({
            accessToken,
            isActive: true,
            signal,
          }),
          balanceRequest,
        ])

      setStatuses(statusesResult)
      setBalance(balanceResult)
    } catch (error) {
      if (error.name === 'AbortError') {
        return
      }

      setLoadError(error.message)
    }
  }, [accessToken, canUseSelfService])

  const loadMyRequests = useCallback(async ({
    signal,
    silent = false,
  } = {}) => {
    if (!canUseSelfService) {
      setMyRequests([])
      setIsLoadingMyData(false)
      return
    }

    if (!silent) {
      setIsLoadingMyData(true)
    }

    setLoadError('')

    try {
      const result = await getMyLeaveRequests({
        accessToken,
        statusCode: myStatusFilter || null,
        signal,
      })

      setMyRequests(result)
    } catch (error) {
      if (error.name === 'AbortError') {
        return
      }

      setLoadError(error.message)
    } finally {
      if (!signal?.aborted && !silent) {
        setIsLoadingMyData(false)
      }
    }
  }, [accessToken, canUseSelfService, myStatusFilter])

  const loadAdminRequests = useCallback(async ({
    signal,
    silent = false,
  } = {}) => {
    if (!canViewAdministration) {
      return
    }

    if (!silent) {
      setIsLoadingAdminData(true)
    }

    setLoadError('')

    try {
      const result = await getLeaveRequests({
        accessToken,
        statusCode: adminStatusFilter || null,
        startDateFrom: startDateFrom || null,
        startDateTo: startDateTo || null,
        searchTerm: debouncedAdminSearchTerm,
        signal,
      })

      setAdminRequests(result)
    } catch (error) {
      if (error.name === 'AbortError') {
        return
      }

      setLoadError(error.message)
    } finally {
      if (!signal?.aborted && !silent) {
        setIsLoadingAdminData(false)
      }
    }
  }, [
    accessToken,
    adminStatusFilter,
    canViewAdministration,
    debouncedAdminSearchTerm,
    startDateFrom,
    startDateTo,
  ])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedAdminSearchTerm(adminSearchTerm)
    }, 250)

    return () => window.clearTimeout(timeoutId)
  }, [adminSearchTerm])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadReferenceData({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadReferenceData])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadMyRequests({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadMyRequests])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadAdminRequests({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadAdminRequests])

  const mySummary = useMemo(() => {
    return {
      total: myRequests.length,
      pending: myRequests.filter(isPendingRequest).length,
      approved: myRequests.filter(
        (request) =>
          request.leaveRequestStatusCode === 'Approved',
      ).length,
    }
  }, [myRequests])

  function openCreateModal() {
    if (!canUseSelfService) {
      return
    }

    setActionNotice('')
    setCreateModalOpen(true)
    setFormValues(emptyForm)
    setFormErrors({})
    setSubmitError('')
  }

  function openCancelRequestModal(request) {
    setActionError('')
    setActionNotice('')
    setCancelRequestTarget(request)
  }

  function openResponseModal(request, isApproved) {
    setActionError('')
    setActionNotice('')
    setResponseTarget({
      request,
      isApproved,
    })
  }

  function closeCreateModal() {
    if (isSaving) {
      return
    }

    setCreateModalOpen(false)
    setFormValues(emptyForm)
    setFormErrors({})
    setSubmitError('')
  }

  function handleFieldChange(event) {
    const { name, value } = event.target

    setFormValues((current) => ({
      ...current,
      [name]: value,
    }))

    setFormErrors((current) => ({
      ...current,
      [name]: undefined,
    }))
  }

  async function handleCreateSubmit(event) {
    event.preventDefault()

    const validationErrors =
      validateLeaveRequestForm(formValues)

    if (Object.keys(validationErrors).length > 0) {
      setFormErrors(validationErrors)
      return
    }

    setIsSaving(true)
    setSubmitError('')

    try {
      await createLeaveRequest({
        accessToken,
        startDate: formValues.startDate,
        endDate: formValues.endDate,
        leaveTypeCode: null,
      })

      setCreateModalOpen(false)
      setFormValues(emptyForm)
      await refreshAfterMutation()
    } catch (error) {
      setSubmitError(error.message)
    } finally {
      setIsSaving(false)
    }
  }

  async function handleCancelRequest() {
    if (!cancelRequestTarget) {
      return
    }

    setIsSaving(true)
    setLoadError('')
    setActionError('')
    setActionNotice('')

    try {
      await cancelLeaveRequest({
        accessToken,
        leaveRequestId:
          cancelRequestTarget.leaveRequestId,
        expectedRowVersion:
          cancelRequestTarget.rowVersion,
      })

      setCancelRequestTarget(null)
      await refreshAfterMutation()
    } catch (error) {
      const message =
        getLeaveRequestActionErrorMessage(error)

      if (isStaleLeaveRequestError(error)) {
        setCancelRequestTarget(null)
        await refreshAfterMutation()
        setActionNotice(message)
      } else {
        setActionError(message)
      }
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRespondRequest() {
    if (!responseTarget) {
      return
    }

    setIsSaving(true)
    setLoadError('')
    setActionError('')
    setActionNotice('')

    try {
      await respondLeaveRequest({
        accessToken,
        leaveRequestId:
          responseTarget.request.leaveRequestId,
        isApproved:
          responseTarget.isApproved,
        expectedRowVersion:
          responseTarget.request.rowVersion,
      })

      setResponseTarget(null)
      await refreshAfterMutation()
    } catch (error) {
      const message =
        getLeaveRequestActionErrorMessage(error)

      if (isStaleLeaveRequestError(error)) {
        setResponseTarget(null)
        await refreshAfterMutation()
        setActionNotice(message)
      } else {
        setActionError(message)
      }
    } finally {
      setIsSaving(false)
    }
  }

  async function refreshAfterMutation() {
    await Promise.all([
      loadReferenceData(),
      canUseSelfService
        ? loadMyRequests({
            silent: true,
          })
        : Promise.resolve(),
      canViewAdministration
        ? loadAdminRequests({
            silent: true,
          })
        : Promise.resolve(),
    ])
  }

  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Solicitudes de vacaciones</h1>
          <p>Consulta saldos, solicitudes propias y aprobaciones pendientes.</p>
        </div>
        {canUseSelfService ? (
          <Button onClick={openCreateModal}>
            <CalendarPlus size={18} aria-hidden="true" />
            Solicitar
          </Button>
        ) : null}
      </div>

      {loadError ? (
        <Alert variant="error">{loadError}</Alert>
      ) : null}

      {actionNotice ? (
        <Alert variant="warning">{actionNotice}</Alert>
      ) : null}

      {canUseSelfService ? (
        <div className={leaveStyles.summaryBar}>
          <SummaryItem
            label="Disponibles"
            value={formatDays(balance?.availableDays)}
          />
          <SummaryItem
            label="Pendientes"
            value={formatDays(balance?.pendingDays)}
          />
          <SummaryItem
            label="Usados"
            value={formatDays(balance?.usedDays)}
          />
          <SummaryItem
            label="Solicitudes"
            value={mySummary.total}
          />
        </div>
      ) : null}

      {balanceError ? (
        <Alert variant="warning">{balanceError}</Alert>
      ) : null}

      {canViewAdministration ? (
        <div className={leaveStyles.tabs} role="tablist">
          {canUseSelfService ? (
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
              Mis vacaciones
            </button>
          ) : null}
          <button
            className={
              selectedView === 'admin'
                ? leaveStyles.tabActive
                : leaveStyles.tab
            }
            type="button"
            role="tab"
            aria-selected={selectedView === 'admin'}
            onClick={() => setActiveView('admin')}
          >
            Administración
          </button>
        </div>
      ) : null}

      {selectedView === 'admin' && canViewAdministration ? (
        <AdministrationRequestsView
          requests={adminRequests}
          statuses={statuses}
          statusFilter={adminStatusFilter}
          searchTerm={adminSearchTerm}
          startDateFrom={startDateFrom}
          startDateTo={startDateTo}
          isLoading={isLoadingAdminData}
          canRespond={canRespond}
          onStatusFilterChange={setAdminStatusFilter}
          onSearchTermChange={setAdminSearchTerm}
          onStartDateFromChange={setStartDateFrom}
          onStartDateToChange={setStartDateTo}
          onRefresh={() => loadAdminRequests()}
          onRespond={openResponseModal}
        />
      ) : canUseSelfService ? (
        <MyRequestsView
          requests={myRequests}
          statuses={statuses}
          statusFilter={myStatusFilter}
          summary={mySummary}
          isLoading={isLoadingMyData}
          onStatusFilterChange={setMyStatusFilter}
          onRefresh={() => loadMyRequests()}
          onCancel={openCancelRequestModal}
        />
      ) : (
        <div className={styles.section}>
          <h2>Sin expediente vinculado</h2>
          <p>
            Tu usuario puede administrar solicitudes, pero no tiene un empleado
            vinculado para usar la vista personal de vacaciones.
          </p>
        </div>
      )}

      {createModalOpen ? (
        <CreateLeaveRequestModal
          values={formValues}
          errors={formErrors}
          submitError={submitError}
          isSaving={isSaving}
          onChange={handleFieldChange}
          onClose={closeCreateModal}
          onSubmit={handleCreateSubmit}
        />
      ) : null}

      {cancelRequestTarget ? (
        <CancelLeaveRequestModal
          request={cancelRequestTarget}
          error={actionError}
          isSaving={isSaving}
          onClose={() => {
            if (!isSaving) {
              setCancelRequestTarget(null)
            }
          }}
          onConfirm={handleCancelRequest}
        />
      ) : null}

      {responseTarget ? (
        <RespondLeaveRequestModal
          request={responseTarget.request}
          isApproved={responseTarget.isApproved}
          error={actionError}
          isSaving={isSaving}
          onClose={() => {
            if (!isSaving) {
              setResponseTarget(null)
            }
          }}
          onConfirm={handleRespondRequest}
        />
      ) : null}
    </section>
  )
}

function MyRequestsView({
  requests,
  statuses,
  statusFilter,
  summary,
  isLoading,
  onStatusFilterChange,
  onRefresh,
  onCancel,
}) {
  return (
    <>
      <div className={styles.toolbar}>
        <select
          className={styles.select}
          aria-label="Filtrar mis solicitudes por estado"
          value={statusFilter}
          onChange={(event) =>
            onStatusFilterChange(event.target.value)}
        >
          <option value="">Todos los estados</option>
          {statuses.map((status) => (
            <option
              key={status.leaveRequestStatusCode}
              value={status.leaveRequestStatusCode}
            >
              {status.name}
            </option>
          ))}
        </select>

        <Button
          variant="secondary"
          onClick={onRefresh}
          disabled={isLoading}
        >
          <RefreshCcw size={18} aria-hidden="true" />
          Actualizar
        </Button>
      </div>

      <RequestsTable
        requests={requests}
        isLoading={isLoading}
        emptyText="No hay solicitudes para mostrar"
        showEmployee
        renderActions={(request) =>
          isPendingRequest(request) ? (
            <Button
              variant="danger"
              size="small"
              title="Cancelar solicitud"
              onClick={() => onCancel(request)}
            >
              <XCircle size={16} aria-hidden="true" />
              Cancelar
            </Button>
          ) : null}
      />

      <div className={styles.section}>
        <h2>Resumen</h2>
        <p>
          Pendientes: {summary.pending} · Aprobadas: {summary.approved}
        </p>
      </div>
    </>
  )
}

function AdministrationRequestsView({
  requests,
  statuses,
  statusFilter,
  searchTerm,
  startDateFrom,
  startDateTo,
  isLoading,
  canRespond,
  onStatusFilterChange,
  onSearchTermChange,
  onStartDateFromChange,
  onStartDateToChange,
  onRefresh,
  onRespond,
}) {
  return (
    <>
      <div className={styles.toolbar}>
        <div className={leaveStyles.toolbarGroup}>
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
              onChange={(event) =>
                onSearchTermChange(event.target.value)}
            />
          </div>

          <select
            className={styles.select}
            aria-label="Filtrar solicitudes por estado"
            value={statusFilter}
            onChange={(event) =>
              onStatusFilterChange(event.target.value)}
          >
            <option value="">Todos los estados</option>
            {statuses.map((status) => (
              <option
                key={status.leaveRequestStatusCode}
                value={status.leaveRequestStatusCode}
              >
                {status.name}
              </option>
            ))}
          </select>

          <input
            className={leaveStyles.dateInput}
            type="date"
            aria-label="Fecha inicial desde"
            value={startDateFrom}
            onChange={(event) =>
              onStartDateFromChange(event.target.value)}
          />

          <input
            className={leaveStyles.dateInput}
            type="date"
            aria-label="Fecha inicial hasta"
            value={startDateTo}
            onChange={(event) =>
              onStartDateToChange(event.target.value)}
          />
        </div>

        <Button
          variant="secondary"
          onClick={onRefresh}
          disabled={isLoading}
        >
          <RefreshCcw size={18} aria-hidden="true" />
          Actualizar
        </Button>
      </div>

      <RequestsTable
        requests={requests}
        isLoading={isLoading}
        emptyText="No hay solicitudes con esos filtros"
        showEmployee
        renderActions={(request) =>
          canRespond && isPendingRequest(request) ? (
            <>
              <Button
                variant="secondary"
                size="small"
                title="Rechazar solicitud"
                onClick={() => onRespond(request, false)}
              >
                <ThumbsDown size={16} aria-hidden="true" />
                Rechazar
              </Button>
              <Button
                size="small"
                title="Aprobar solicitud"
                onClick={() => onRespond(request, true)}
              >
                <ThumbsUp size={16} aria-hidden="true" />
                Aprobar
              </Button>
            </>
          ) : null}
      />
    </>
  )
}

function RequestsTable({
  requests,
  isLoading,
  emptyText,
  showEmployee,
  renderActions,
}) {
  return (
    <div className={styles.tableShell}>
      <table className={styles.table}>
        <thead>
          <tr>
            {showEmployee ? <th>Empleado</th> : null}
            <th>Periodo</th>
            <th>Días</th>
            <th>Estado</th>
            <th>Actualizado</th>
            <th aria-label="Acciones" />
          </tr>
        </thead>
        <tbody>
          {isLoading ? (
            <tr>
              <td
                colSpan={showEmployee ? '6' : '5'}
                className={leaveStyles.loadingCell}
              >
                Cargando solicitudes
              </td>
            </tr>
          ) : null}

          {!isLoading && requests.length === 0 ? (
            <tr>
              <td
                colSpan={showEmployee ? '6' : '5'}
                className={leaveStyles.emptyCell}
              >
                {emptyText}
              </td>
            </tr>
          ) : null}

          {!isLoading
            ? requests.map((request) => (
              <tr key={request.leaveRequestId}>
                {showEmployee ? (
                  <td>
                    <div className={leaveStyles.employeeCell}>
                      <strong>
                        {request.firstName} {request.lastName}
                      </strong>
                      <span>
                        {request.departmentName}
                        {' · '}
                        {request.identificationNumber}
                      </span>
                    </div>
                  </td>
                ) : null}
                <td>
                  <div className={leaveStyles.periodCell}>
                    <strong>
                      {formatDate(request.startDate)}
                      {' - '}
                      {formatDate(request.endDate)}
                    </strong>
                    <span>{request.leaveTypeName}</span>
                  </div>
                </td>
                <td>{formatDays(request.requestedDays)}</td>
                <td>
                  <Badge
                    variant={getStatusBadgeVariant(
                      request.leaveRequestStatusCode,
                    )}
                  >
                    {request.leaveRequestStatusName}
                  </Badge>
                </td>
                <td className={leaveStyles.muted}>
                  {formatDateTime(
                    request.updatedAtUtc
                      ?? request.createdAtUtc,
                  )}
                </td>
                <td>
                  <div className={leaveStyles.actionsCell}>
                    {renderActions(request)}
                  </div>
                </td>
              </tr>
            ))
            : null}
        </tbody>
      </table>
    </div>
  )
}

function SummaryItem({ label, value }) {
  return (
    <div className={leaveStyles.summaryItem}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function CreateLeaveRequestModal({
  values,
  errors,
  submitError,
  isSaving,
  onChange,
  onClose,
  onSubmit,
}) {
  return (
    <div className={leaveStyles.modalOverlay}>
      <section
        className={leaveStyles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="leave-request-form-title"
      >
        <div className={leaveStyles.modalHeader}>
          <h2 id="leave-request-form-title">
            Nueva solicitud
          </h2>
          <Button
            variant="ghost"
            size="small"
            aria-label="Cerrar"
            disabled={isSaving}
            onClick={onClose}
          >
            <X size={18} aria-hidden="true" />
          </Button>
        </div>

        <form className={leaveStyles.form} onSubmit={onSubmit}>
          {submitError ? (
            <Alert variant="error">{submitError}</Alert>
          ) : null}

          <div className={leaveStyles.formGrid}>
            <DateField
              id="leave-start-date"
              name="startDate"
              label="Fecha inicial"
              value={values.startDate}
              error={errors.startDate}
              onChange={onChange}
            />

            <DateField
              id="leave-end-date"
              name="endDate"
              label="Fecha final"
              value={values.endDate}
              error={errors.endDate}
              onChange={onChange}
            />
          </div>

          <div className={leaveStyles.formActions}>
            <Button
              variant="secondary"
              disabled={isSaving}
              onClick={onClose}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              isLoading={isSaving}
              loadingText="Guardando..."
            >
              Enviar
            </Button>
          </div>
        </form>
      </section>
    </div>
  )
}

function DateField({
  id,
  name,
  label,
  value,
  error,
  onChange,
}) {
  return (
    <div className={leaveStyles.field}>
      <label className={leaveStyles.label} htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        name={name}
        className={leaveStyles.dateInput}
        type="date"
        value={value}
        aria-invalid={Boolean(error)}
        onChange={onChange}
        required
      />
      {error ? (
        <span className={leaveStyles.errorText}>{error}</span>
      ) : null}
    </div>
  )
}

function CancelLeaveRequestModal({
  request,
  error,
  isSaving,
  onClose,
  onConfirm,
}) {
  return (
    <ConfirmModal
      title="Cancelar solicitud"
      request={request}
      error={error}
      isSaving={isSaving}
      confirmVariant="danger"
      confirmText="Cancelar solicitud"
      onClose={onClose}
      onConfirm={onConfirm}
    >
      <p>La solicitud volverá a liberar los días pendientes.</p>
      <p className={leaveStyles.warningText}>
        Solo se pueden cancelar solicitudes pendientes.
      </p>
    </ConfirmModal>
  )
}

function RespondLeaveRequestModal({
  request,
  isApproved,
  error,
  isSaving,
  onClose,
  onConfirm,
}) {
  return (
    <ConfirmModal
      title={isApproved
        ? 'Aprobar solicitud'
        : 'Rechazar solicitud'}
      request={request}
      error={error}
      isSaving={isSaving}
      confirmVariant={isApproved ? 'primary' : 'danger'}
      confirmText={isApproved ? 'Aprobar' : 'Rechazar'}
      onClose={onClose}
      onConfirm={onConfirm}
    >
      <p>
        {isApproved
          ? 'Los días pasarán de pendientes a usados.'
          : 'Los días pendientes volverán a estar disponibles.'}
      </p>
    </ConfirmModal>
  )
}

function ConfirmModal({
  title,
  request,
  error,
  isSaving,
  confirmVariant,
  confirmText,
  onClose,
  onConfirm,
  children,
}) {
  return (
    <div className={leaveStyles.modalOverlay}>
      <section
        className={`${leaveStyles.modal} ${leaveStyles.confirmModal}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="leave-confirm-title"
      >
        <div className={leaveStyles.modalHeader}>
          <div>
            <h2 id="leave-confirm-title">{title}</h2>
            <p>
              {formatDate(request.startDate)}
              {' - '}
              {formatDate(request.endDate)}
              {' · '}
              {formatDays(request.requestedDays)}
            </p>
          </div>
          <Button
            variant="ghost"
            size="small"
            aria-label="Cerrar"
            disabled={isSaving}
            onClick={onClose}
          >
            <X size={18} aria-hidden="true" />
          </Button>
        </div>

        <div className={leaveStyles.confirmBody}>
          {error ? (
            <Alert variant="error">{error}</Alert>
          ) : null}
          {children}
        </div>

        <div className={leaveStyles.confirmActions}>
          <Button
            variant="secondary"
            disabled={isSaving}
            onClick={onClose}
          >
            Volver
          </Button>
          <Button
            variant={confirmVariant}
            isLoading={isSaving}
            loadingText="Procesando..."
            onClick={onConfirm}
          >
            {confirmVariant === 'danger' ? (
              <XCircle size={18} aria-hidden="true" />
            ) : (
              <CheckCircle2 size={18} aria-hidden="true" />
            )}
            {confirmText}
          </Button>
        </div>
      </section>
    </div>
  )
}

function validateLeaveRequestForm(values) {
  const errors = {}

  if (!values.startDate) {
    errors.startDate = 'La fecha inicial es requerida.'
  }

  if (!values.endDate) {
    errors.endDate = 'La fecha final es requerida.'
  }

  if (
    values.startDate
    && values.endDate
    && values.endDate < values.startDate
  ) {
    errors.endDate =
      'La fecha final no puede ser menor a la inicial.'
  }

  return errors
}

function isStaleLeaveRequestError(error) {
  const errorCode = getApiErrorCode(error)

  return errorCode === 'concurrency_conflict'
    || errorCode === 'leave_request_already_resolved'
}

function getLeaveRequestActionErrorMessage(error) {
  if (isStaleLeaveRequestError(error)) {
    return 'La solicitud ya fue actualizada en otra sesión. Actualizamos la lista para mostrar el estado actual.'
  }

  return error?.message
    ?? 'No pudimos completar la operación. Inténtalo de nuevo.'
}

function getApiErrorCode(error) {
  return error?.errorCode
    ?? error?.details?.errorCode
    ?? error?.details?.extensions?.errorCode
    ?? ''
}

function isPendingRequest(request) {
  return request.leaveRequestStatusCode === 'Pending'
}

function getStatusBadgeVariant(statusCode) {
  const variants = {
    Pending: 'warning',
    Approved: 'success',
    Rejected: 'danger',
    Cancelled: 'neutral',
  }

  return variants[statusCode] ?? 'neutral'
}

function formatDays(value) {
  if (value === null || value === undefined) {
    return '-'
  }

  return new Intl.NumberFormat('es-CR', {
    maximumFractionDigits: 2,
  }).format(Number(value))
}

function formatDate(value) {
  if (!value) {
    return 'Sin fecha'
  }

  return new Intl.DateTimeFormat('es-CR', {
    dateStyle: 'medium',
    timeZone: 'UTC',
  }).format(new Date(value))
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
