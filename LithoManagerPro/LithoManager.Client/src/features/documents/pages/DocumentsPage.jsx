import {
  CheckCircle2,
  Download,
  Edit3,
  FilePlus2,
  RefreshCcw,
  Search,
  Upload,
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
import { TextInput } from '../../../components/ui/TextInput.jsx'
import styles from '../../../app/Page.module.css'
import { useAuth } from '../../security/hooks/useAuth.js'
import {
  createEmployeeDocument,
  downloadEmployeeDocument,
  ensureEmployeeRecord,
  getDocumentEmployeeOptions,
  getDocumentTypes,
  getEmployeeDocuments,
  setEmployeeDocumentStatus,
  updateEmployeeDocument,
} from '../services/documentService.js'
import documentStyles from './DocumentsPage.module.css'

const emptyForm = {
  employeeId: '',
  documentTypeId: '',
  title: '',
  description: '',
  issuedDate: '',
  expirationDate: '',
  isVisibleToEmployee: true,
  file: null,
}

const administrationRoles = new Set([
  'SuperAdministrator',
  'HumanResourcesAdministrator',
  'HumanResourcesStaff',
])

const statusMutationRoles = new Set([
  'SuperAdministrator',
  'HumanResourcesAdministrator',
])

const statusFilterValues = {
  all: null,
  active: true,
  inactive: false,
}

const visibilityFilterValues = {
  all: null,
  visible: true,
  internal: false,
}

export function DocumentsPage() {
  const { accessToken, user } = useAuth()
  const [documents, setDocuments] = useState([])
  const [documentTypes, setDocumentTypes] = useState([])
  const [employeeOptions, setEmployeeOptions] = useState([])
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearchTerm, setDebouncedSearchTerm] =
    useState('')
  const [employeeIdFilter, setEmployeeIdFilter] = useState('')
  const [documentTypeFilter, setDocumentTypeFilter] =
    useState('')
  const [statusFilter, setStatusFilter] = useState('active')
  const [visibilityFilter, setVisibilityFilter] =
    useState('all')
  const [createdFrom, setCreatedFrom] = useState('')
  const [createdTo, setCreatedTo] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingTypes, setIsLoadingTypes] = useState(false)
  const [isLoadingEmployees, setIsLoadingEmployees] =
    useState(false)
  const [loadError, setLoadError] = useState('')
  const [actionNotice, setActionNotice] = useState('')
  const [modalState, setModalState] = useState(null)
  const [formValues, setFormValues] = useState(emptyForm)
  const [formErrors, setFormErrors] = useState({})
  const [submitError, setSubmitError] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [statusTarget, setStatusTarget] = useState(null)
  const [statusActionId, setStatusActionId] = useState(null)
  const [downloadActionId, setDownloadActionId] = useState(null)

  const canAdministerDocuments =
    administrationRoles.has(user?.roleCode)
  const canChangeStatus =
    statusMutationRoles.has(user?.roleCode)
  const canEditDocuments = canAdministerDocuments

  const loadDocumentTypes = useCallback(async ({
    signal,
  } = {}) => {
    if (!canAdministerDocuments) {
      setDocumentTypes([])
      return
    }

    setIsLoadingTypes(true)

    try {
      const result = await getDocumentTypes({
        accessToken,
        isActive: true,
        signal,
      })

      setDocumentTypes(result)
    } catch (error) {
      if (error.name !== 'AbortError') {
        setLoadError(error.message)
      }
    } finally {
      if (!signal?.aborted) {
        setIsLoadingTypes(false)
      }
    }
  }, [accessToken, canAdministerDocuments])

  const loadEmployeeOptions = useCallback(async ({
    signal,
  } = {}) => {
    if (!canAdministerDocuments) {
      setEmployeeOptions([])
      return
    }

    setIsLoadingEmployees(true)

    try {
      const result = await getDocumentEmployeeOptions({
        accessToken,
        signal,
      })

      setEmployeeOptions(result)
    } catch (error) {
      if (error.name !== 'AbortError') {
        setLoadError(error.message)
      }
    } finally {
      if (!signal?.aborted) {
        setIsLoadingEmployees(false)
      }
    }
  }, [accessToken, canAdministerDocuments])

  const loadDocuments = useCallback(async ({
    signal,
    silent = false,
  } = {}) => {
    if (!silent) {
      setIsLoading(true)
    }

    setLoadError('')

    try {
      const result = await getEmployeeDocuments({
        accessToken,
        employeeId: canAdministerDocuments
          ? toPositiveInteger(employeeIdFilter)
          : null,
        documentTypeId: canAdministerDocuments
          ? toPositiveInteger(documentTypeFilter)
          : null,
        isActive: canAdministerDocuments
          ? statusFilterValues[statusFilter]
          : true,
        isVisibleToEmployee: canAdministerDocuments
          ? visibilityFilterValues[visibilityFilter]
          : true,
        createdFromUtc: canAdministerDocuments
          ? createdFrom
          : null,
        createdToUtc: canAdministerDocuments
          ? createdTo
          : null,
        searchTerm: debouncedSearchTerm,
        signal,
      })

      setDocuments(result)
    } catch (error) {
      if (error.name === 'AbortError') {
        return
      }

      setLoadError(error.message)
    } finally {
      if (!signal?.aborted && !silent) {
        setIsLoading(false)
      }
    }
  }, [
    accessToken,
    canAdministerDocuments,
    createdFrom,
    createdTo,
    debouncedSearchTerm,
    documentTypeFilter,
    employeeIdFilter,
    statusFilter,
    visibilityFilter,
  ])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearchTerm(searchTerm)
    }, 250)

    return () => window.clearTimeout(timeoutId)
  }, [searchTerm])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadDocumentTypes({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadDocumentTypes])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadEmployeeOptions({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadEmployeeOptions])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadDocuments({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadDocuments])

  const summary = useMemo(() => {
    const active = documents.filter(
      (document) => document.isActive,
    ).length
    const visible = documents.filter(
      (document) => document.isVisibleToEmployee,
    ).length

    return {
      total: documents.length,
      active,
      internal: documents.length - visible,
    }
  }, [documents])

  function openCreateModal() {
    setActionNotice('')
    setModalState({
      mode: 'create',
      document: null,
    })
    setFormValues(emptyForm)
    setFormErrors({})
    setSubmitError('')
  }

  function openEditModal(document) {
    setActionNotice('')
    setModalState({
      mode: 'edit',
      document,
    })
    setFormValues({
      employeeId: String(document.employeeId),
      documentTypeId: String(document.documentTypeId),
      title: document.title,
      description: document.description ?? '',
      issuedDate: toInputDate(document.issuedDate),
      expirationDate: toInputDate(document.expirationDate),
      isVisibleToEmployee: document.isVisibleToEmployee,
      file: null,
    })
    setFormErrors({})
    setSubmitError('')
  }

  function closeModal() {
    if (isSaving) {
      return
    }

    setModalState(null)
    setFormValues(emptyForm)
    setFormErrors({})
    setSubmitError('')
  }

  function handleFieldChange(event) {
    const { name, type, value, checked, files } = event.target
    const nextValue = type === 'checkbox'
      ? checked
      : type === 'file'
        ? files?.[0] ?? null
        : value

    setFormValues((current) => ({
      ...current,
      [name]: nextValue,
    }))

    setFormErrors((current) => ({
      ...current,
      [name]: undefined,
    }))
  }

  async function handleSubmit(event) {
    event.preventDefault()

    const validationErrors = validateDocumentForm(
      formValues,
      modalState?.mode,
    )

    if (Object.keys(validationErrors).length > 0) {
      setFormErrors(validationErrors)
      return
    }

    setIsSaving(true)
    setSubmitError('')

    try {
      if (modalState.mode === 'create') {
        const employeeId = Number(formValues.employeeId)

        await ensureEmployeeRecord({
          accessToken,
          employeeId,
        })

        await createEmployeeDocument({
          accessToken,
          employeeId,
          documentTypeId: Number(formValues.documentTypeId),
          title: formValues.title.trim(),
          description: normalizeText(formValues.description),
          issuedDate: formValues.issuedDate || null,
          expirationDate: formValues.expirationDate || null,
          isVisibleToEmployee:
            formValues.isVisibleToEmployee,
          file: formValues.file,
        })
      } else {
        await updateEmployeeDocument({
          accessToken,
          employeeDocumentId:
            modalState.document.employeeDocumentId,
          documentTypeId: Number(formValues.documentTypeId),
          title: formValues.title.trim(),
          description: normalizeText(formValues.description),
          issuedDate: formValues.issuedDate || null,
          expirationDate: formValues.expirationDate || null,
          isVisibleToEmployee:
            formValues.isVisibleToEmployee,
          expectedRowVersion:
            modalState.document.rowVersion,
        })
      }

      setModalState(null)
      setFormValues(emptyForm)
      await loadDocuments({
        silent: true,
      })
    } catch (error) {
      setSubmitError(getDocumentActionMessage(error))
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDownload(document) {
    setDownloadActionId(document.employeeDocumentId)
    setLoadError('')

    try {
      const result = await downloadEmployeeDocument({
        accessToken,
        employeeDocumentId: document.employeeDocumentId,
      })

      saveDownloadedFile(
        result.blob,
        result.fileName ?? document.originalFileName,
      )
    } catch (error) {
      setLoadError(error.message)
    } finally {
      setDownloadActionId(null)
    }
  }

  function openStatusModal(document) {
    setActionNotice('')
    setStatusTarget(document)
  }

  async function handleStatusChange() {
    if (!statusTarget) {
      return
    }

    setStatusActionId(statusTarget.employeeDocumentId)
    setLoadError('')
    setActionNotice('')

    try {
      await setEmployeeDocumentStatus({
        accessToken,
        employeeDocumentId:
          statusTarget.employeeDocumentId,
        isActive: !statusTarget.isActive,
        expectedRowVersion: statusTarget.rowVersion,
      })

      setStatusTarget(null)
      await loadDocuments({
        silent: true,
      })
    } catch (error) {
      if (isStaleDocumentError(error)) {
        setStatusTarget(null)
        await loadDocuments({
          silent: true,
        })
        setActionNotice(getDocumentActionMessage(error))
      } else {
        setLoadError(error.message)
      }
    } finally {
      setStatusActionId(null)
    }
  }

  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Documentos</h1>
          <p>
            {canAdministerDocuments
              ? 'Administra expedientes y documentos laborales.'
              : 'Consulta tus documentos laborales.'}
          </p>
        </div>
        {canEditDocuments ? (
          <Button onClick={openCreateModal}>
            <FilePlus2 size={18} aria-hidden="true" />
            Cargar
          </Button>
        ) : null}
      </div>

      {canAdministerDocuments ? (
        <div className={documentStyles.summaryBar}>
          <SummaryItem label="Total" value={summary.total} />
          <SummaryItem label="Activos" value={summary.active} />
          <SummaryItem label="Internos" value={summary.internal} />
        </div>
      ) : null}

      {loadError ? (
        <Alert variant="error">{loadError}</Alert>
      ) : null}

      {actionNotice ? (
        <Alert variant="warning">{actionNotice}</Alert>
      ) : null}

      <DocumentsToolbar
        canAdministerDocuments={canAdministerDocuments}
        searchTerm={searchTerm}
        employeeIdFilter={employeeIdFilter}
        documentTypeFilter={documentTypeFilter}
        statusFilter={statusFilter}
        visibilityFilter={visibilityFilter}
        createdFrom={createdFrom}
        createdTo={createdTo}
        documentTypes={documentTypes}
        employeeOptions={employeeOptions}
        isLoading={isLoading}
        isLoadingEmployees={isLoadingEmployees}
        onSearchTermChange={setSearchTerm}
        onEmployeeIdFilterChange={setEmployeeIdFilter}
        onDocumentTypeFilterChange={setDocumentTypeFilter}
        onStatusFilterChange={setStatusFilter}
        onVisibilityFilterChange={setVisibilityFilter}
        onCreatedFromChange={setCreatedFrom}
        onCreatedToChange={setCreatedTo}
        onRefresh={() => loadDocuments()}
      />

      <DocumentsTable
        documents={documents}
        isLoading={isLoading}
        canEditDocuments={canEditDocuments}
        canChangeStatus={canChangeStatus}
        downloadActionId={downloadActionId}
        statusActionId={statusActionId}
        onEdit={openEditModal}
        onDownload={handleDownload}
        onStatus={openStatusModal}
      />

      {modalState ? (
        <DocumentModal
          mode={modalState.mode}
          values={formValues}
          errors={formErrors}
          submitError={submitError}
          isSaving={isSaving}
          isLoadingTypes={isLoadingTypes}
          isLoadingEmployees={isLoadingEmployees}
          documentTypes={documentTypes}
          employeeOptions={employeeOptions}
          onChange={handleFieldChange}
          onClose={closeModal}
          onSubmit={handleSubmit}
        />
      ) : null}

      {statusTarget ? (
        <DocumentStatusModal
          document={statusTarget}
          isSaving={
            statusActionId
              === statusTarget.employeeDocumentId
          }
          onClose={() => {
            if (statusActionId === null) {
              setStatusTarget(null)
            }
          }}
          onConfirm={handleStatusChange}
        />
      ) : null}
    </section>
  )
}

function DocumentsToolbar({
  canAdministerDocuments,
  searchTerm,
  employeeIdFilter,
  documentTypeFilter,
  statusFilter,
  visibilityFilter,
  createdFrom,
  createdTo,
  documentTypes,
  employeeOptions,
  isLoading,
  isLoadingEmployees,
  onSearchTermChange,
  onEmployeeIdFilterChange,
  onDocumentTypeFilterChange,
  onStatusFilterChange,
  onVisibilityFilterChange,
  onCreatedFromChange,
  onCreatedToChange,
  onRefresh,
}) {
  return (
    <div className={styles.toolbar}>
      <div className={documentStyles.toolbarGroup}>
        <div className={documentStyles.searchBox}>
          <Search
            className={documentStyles.searchIcon}
            size={18}
            aria-hidden="true"
          />
          <input
            className={documentStyles.searchInput}
            type="search"
            placeholder="Buscar documento"
            aria-label="Buscar documento"
            maxLength={150}
            value={searchTerm}
            onChange={(event) =>
              onSearchTermChange(event.target.value)}
          />
        </div>

        {canAdministerDocuments ? (
          <>
            <select
              className={styles.select}
              aria-label="Filtrar por empleado"
              value={employeeIdFilter}
              disabled={isLoadingEmployees}
              onChange={(event) =>
                onEmployeeIdFilterChange(event.target.value)}
            >
              <option value="">Todos los empleados</option>
              {employeeOptions.map((employee) => (
                <option
                  key={employee.employeeId}
                  value={employee.employeeId}
                >
                  {formatEmployeeOption(employee)}
                </option>
              ))}
            </select>

            <select
              className={styles.select}
              aria-label="Filtrar por tipo"
              value={documentTypeFilter}
              onChange={(event) =>
                onDocumentTypeFilterChange(event.target.value)}
            >
              <option value="">Todos los tipos</option>
              {documentTypes.map((documentType) => (
                <option
                  key={documentType.documentTypeId}
                  value={documentType.documentTypeId}
                >
                  {documentType.name}
                </option>
              ))}
            </select>

            <select
              className={styles.select}
              aria-label="Filtrar por estado"
              value={statusFilter}
              onChange={(event) =>
                onStatusFilterChange(event.target.value)}
            >
              <option value="all">Todos los estados</option>
              <option value="active">Activos</option>
              <option value="inactive">Inactivos</option>
            </select>

            <select
              className={styles.select}
              aria-label="Filtrar por visibilidad"
              value={visibilityFilter}
              onChange={(event) =>
                onVisibilityFilterChange(event.target.value)}
            >
              <option value="all">Toda visibilidad</option>
              <option value="visible">Visible al empleado</option>
              <option value="internal">Interno</option>
            </select>

            <input
              className={documentStyles.dateInput}
              type="date"
              aria-label="Creado desde"
              value={createdFrom}
              onChange={(event) =>
                onCreatedFromChange(event.target.value)}
            />

            <input
              className={documentStyles.dateInput}
              type="date"
              aria-label="Creado hasta"
              value={createdTo}
              onChange={(event) =>
                onCreatedToChange(event.target.value)}
            />
          </>
        ) : null}
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
  )
}

function DocumentsTable({
  documents,
  isLoading,
  canEditDocuments,
  canChangeStatus,
  downloadActionId,
  statusActionId,
  onEdit,
  onDownload,
  onStatus,
}) {
  return (
    <div className={styles.tableShell}>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>Documento</th>
            <th>Empleado</th>
            <th>Tipo</th>
            <th>Visibilidad</th>
            <th>Estado</th>
            <th>Actualizado</th>
            <th aria-label="Acciones" />
          </tr>
        </thead>
        <tbody>
          {isLoading ? (
            <tr>
              <td
                colSpan="7"
                className={documentStyles.loadingCell}
              >
                Cargando documentos
              </td>
            </tr>
          ) : null}

          {!isLoading && documents.length === 0 ? (
            <tr>
              <td
                colSpan="7"
                className={documentStyles.emptyCell}
              >
                No hay documentos para mostrar
              </td>
            </tr>
          ) : null}

          {!isLoading
            ? documents.map((document) => (
              <tr key={document.employeeDocumentId}>
                <td>
                  <div className={documentStyles.documentCell}>
                    <strong>{document.title}</strong>
                    <span>{document.originalFileName}</span>
                  </div>
                </td>
                <td>
                  <div className={documentStyles.employeeCell}>
                    <strong>
                      {document.firstName} {document.lastName}
                    </strong>
                    <span>
                      {document.departmentName}
                      {' · '}
                      {document.identificationNumber}
                    </span>
                  </div>
                </td>
                <td>{document.documentTypeName}</td>
                <td>
                  <Badge
                    variant={
                      document.isVisibleToEmployee
                        ? 'info'
                        : 'neutral'
                    }
                  >
                    {document.isVisibleToEmployee
                      ? 'Visible'
                      : 'Interno'}
                  </Badge>
                </td>
                <td>
                  <Badge
                    variant={
                      document.isActive
                        ? 'success'
                        : 'neutral'
                    }
                  >
                    {document.isActive ? 'Activo' : 'Inactivo'}
                  </Badge>
                </td>
                <td className={documentStyles.muted}>
                  {formatDateTime(
                    document.updatedAtUtc
                      ?? document.createdAtUtc,
                  )}
                </td>
                <td>
                  <div className={documentStyles.actionsCell}>
                    <Button
                      variant="secondary"
                      size="small"
                      title="Descargar documento"
                      isLoading={
                        downloadActionId
                          === document.employeeDocumentId
                      }
                      loadingText="Descargando..."
                      onClick={() => onDownload(document)}
                    >
                      <Download size={16} aria-hidden="true" />
                      Descargar
                    </Button>
                    {canEditDocuments ? (
                      <Button
                        variant="secondary"
                        size="small"
                        title="Editar documento"
                        onClick={() => onEdit(document)}
                      >
                        <Edit3 size={16} aria-hidden="true" />
                        Editar
                      </Button>
                    ) : null}
                    {canChangeStatus ? (
                      <Button
                        variant={
                          document.isActive
                            ? 'danger'
                            : 'secondary'
                        }
                        size="small"
                        title={
                          document.isActive
                            ? 'Desactivar documento'
                            : 'Activar documento'
                        }
                        isLoading={
                          statusActionId
                            === document.employeeDocumentId
                        }
                        loadingText="Procesando..."
                        onClick={() => onStatus(document)}
                      >
                        {document.isActive ? (
                          <XCircle size={16} aria-hidden="true" />
                        ) : (
                          <CheckCircle2 size={16} aria-hidden="true" />
                        )}
                        {document.isActive
                          ? 'Desactivar'
                          : 'Activar'}
                      </Button>
                    ) : null}
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

function DocumentModal({
  mode,
  values,
  errors,
  submitError,
  isSaving,
  isLoadingTypes,
  isLoadingEmployees,
  documentTypes,
  employeeOptions,
  onChange,
  onClose,
  onSubmit,
}) {
  const title = mode === 'create'
    ? 'Cargar documento'
    : 'Editar documento'

  return (
    <div className={documentStyles.modalOverlay}>
      <section
        className={documentStyles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="document-form-title"
      >
        <div className={documentStyles.modalHeader}>
          <h2 id="document-form-title">{title}</h2>
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

        <form
          className={documentStyles.form}
          onSubmit={onSubmit}
        >
          {submitError ? (
            <Alert variant="error">{submitError}</Alert>
          ) : null}

          <div className={documentStyles.formGrid}>
            <SelectField
              id="document-employee-id"
              name="employeeId"
              label="Empleado"
              value={values.employeeId}
              error={errors.employeeId}
              disabled={mode === 'edit' || isLoadingEmployees}
              onChange={onChange}
              required
            >
              <option value="">Seleccionar empleado</option>
              {employeeOptions.map((employee) => (
                <option
                  key={employee.employeeId}
                  value={employee.employeeId}
                >
                  {formatEmployeeOption(employee)}
                </option>
              ))}
            </SelectField>

            <SelectField
              id="document-type-id"
              name="documentTypeId"
              label="Tipo"
              value={values.documentTypeId}
              error={errors.documentTypeId}
              disabled={isLoadingTypes}
              onChange={onChange}
              required
            >
              <option value="">Seleccionar tipo</option>
              {documentTypes.map((documentType) => (
                <option
                  key={documentType.documentTypeId}
                  value={documentType.documentTypeId}
                >
                  {documentType.name}
                </option>
              ))}
            </SelectField>
          </div>

          <TextInput
            id="document-title"
            name="title"
            label="Título"
            maxLength={150}
            value={values.title}
            error={errors.title}
            onChange={onChange}
            required
          />

          <div className={documentStyles.field}>
            <label
              className={documentStyles.label}
              htmlFor="document-description"
            >
              Descripción
            </label>
            <textarea
              id="document-description"
              name="description"
              className={
                errors.description
                  ? documentStyles.textareaError
                  : documentStyles.textarea
              }
              maxLength={500}
              value={values.description}
              aria-invalid={Boolean(errors.description)}
              onChange={onChange}
            />
            <div className={documentStyles.fieldFooter}>
              <span>
                {values.description.length}/500
              </span>
              {errors.description ? (
                <span className={documentStyles.errorText}>
                  {errors.description}
                </span>
              ) : null}
            </div>
          </div>

          <div className={documentStyles.formGrid}>
            <DateField
              id="document-issued-date"
              name="issuedDate"
              label="Fecha de emisión"
              value={values.issuedDate}
              error={errors.issuedDate}
              onChange={onChange}
            />

            <DateField
              id="document-expiration-date"
              name="expirationDate"
              label="Fecha de vencimiento"
              value={values.expirationDate}
              error={errors.expirationDate}
              onChange={onChange}
            />
          </div>

          <label className={documentStyles.checkboxLabel}>
            <input
              name="isVisibleToEmployee"
              type="checkbox"
              checked={values.isVisibleToEmployee}
              onChange={onChange}
            />
            <span>Visible al empleado</span>
          </label>

          {mode === 'create' ? (
            <FileField
              file={values.file}
              error={errors.file}
              onChange={onChange}
            />
          ) : null}

          <div className={documentStyles.formActions}>
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
              {mode === 'create' ? (
                <Upload size={18} aria-hidden="true" />
              ) : (
                <Edit3 size={18} aria-hidden="true" />
              )}
              Guardar
            </Button>
          </div>
        </form>
      </section>
    </div>
  )
}

function DocumentStatusModal({
  document,
  isSaving,
  onClose,
  onConfirm,
}) {
  const isDeactivating = document.isActive

  return (
    <div className={documentStyles.modalOverlay}>
      <section
        className={`${documentStyles.modal} ${documentStyles.confirmModal}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="document-status-title"
      >
        <div className={documentStyles.modalHeader}>
          <div>
            <h2 id="document-status-title">
              {isDeactivating
                ? 'Desactivar documento'
                : 'Activar documento'}
            </h2>
            <p>{document.title}</p>
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

        <div className={documentStyles.confirmBody}>
          <p>
            {isDeactivating
              ? 'El documento dejará de estar disponible en el expediente.'
              : 'El documento volverá a estar disponible en el expediente.'}
          </p>
          <p className={documentStyles.warningText}>
            La acción conserva el historial del documento.
          </p>
        </div>

        <div className={documentStyles.confirmActions}>
          <Button
            variant="secondary"
            disabled={isSaving}
            onClick={onClose}
          >
            Volver
          </Button>
          <Button
            variant={isDeactivating ? 'danger' : 'primary'}
            isLoading={isSaving}
            loadingText="Procesando..."
            onClick={onConfirm}
          >
            {isDeactivating ? (
              <XCircle size={18} aria-hidden="true" />
            ) : (
              <CheckCircle2 size={18} aria-hidden="true" />
            )}
            {isDeactivating ? 'Desactivar' : 'Activar'}
          </Button>
        </div>
      </section>
    </div>
  )
}

function SummaryItem({ label, value }) {
  return (
    <div className={documentStyles.summaryItem}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function SelectField({
  id,
  name,
  label,
  value,
  error,
  disabled,
  required,
  onChange,
  children,
}) {
  return (
    <div className={documentStyles.field}>
      <label className={documentStyles.label} htmlFor={id}>
        {label}
        {required ? <span aria-hidden="true"> *</span> : null}
      </label>
      <select
        id={id}
        name={name}
        className={
          error
            ? documentStyles.selectError
            : documentStyles.select
        }
        value={value}
        disabled={disabled}
        aria-invalid={Boolean(error)}
        onChange={onChange}
        required={required}
      >
        {children}
      </select>
      {error ? (
        <span className={documentStyles.errorText}>{error}</span>
      ) : null}
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
    <div className={documentStyles.field}>
      <label className={documentStyles.label} htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        name={name}
        className={
          error
            ? documentStyles.dateInputError
            : documentStyles.dateInput
        }
        type="date"
        value={value}
        aria-invalid={Boolean(error)}
        onChange={onChange}
      />
      {error ? (
        <span className={documentStyles.errorText}>{error}</span>
      ) : null}
    </div>
  )
}

function FileField({ file, error, onChange }) {
  return (
    <div className={documentStyles.field}>
      <label
        className={documentStyles.label}
        htmlFor="document-file"
      >
        Archivo <span aria-hidden="true">*</span>
      </label>
      <input
        id="document-file"
        name="file"
        className={
          error
            ? documentStyles.fileInputError
            : documentStyles.fileInput
        }
        type="file"
        aria-invalid={Boolean(error)}
        onChange={onChange}
        required
      />
      <div className={documentStyles.fieldFooter}>
        <span>{file?.name ?? 'Sin archivo seleccionado'}</span>
        {error ? (
          <span className={documentStyles.errorText}>{error}</span>
        ) : null}
      </div>
    </div>
  )
}

function validateDocumentForm(values, mode) {
  const errors = {}
  const employeeId = Number(values.employeeId)
  const documentTypeId = Number(values.documentTypeId)

  if (!Number.isInteger(employeeId) || employeeId <= 0) {
    errors.employeeId = 'El empleado es requerido.'
  }

  if (!Number.isInteger(documentTypeId) || documentTypeId <= 0) {
    errors.documentTypeId = 'El tipo de documento es requerido.'
  }

  if (!values.title.trim()) {
    errors.title = 'El título es requerido.'
  } else if (values.title.trim().length > 150) {
    errors.title = 'El título no puede superar 150 caracteres.'
  }

  if (values.description.length > 500) {
    errors.description =
      'La descripción no puede superar 500 caracteres.'
  }

  if (
    values.issuedDate
    && values.expirationDate
    && values.expirationDate < values.issuedDate
  ) {
    errors.expirationDate =
      'El vencimiento no puede ser menor a la emisión.'
  }

  if (mode === 'create' && !values.file) {
    errors.file = 'El archivo es requerido.'
  }

  return errors
}

function getDocumentActionMessage(error) {
  if (isStaleDocumentError(error)) {
    return 'El documento ya fue actualizado en otra sesión. Actualizamos la lista para mostrar el estado actual.'
  }

  return error?.message
    ?? 'No pudimos completar la operación. Inténtalo de nuevo.'
}

function isStaleDocumentError(error) {
  const errorCode = error?.errorCode
    ?? error?.details?.errorCode
    ?? error?.details?.extensions?.errorCode
    ?? ''

  return errorCode === 'concurrency_conflict'
}

function saveDownloadedFile(blob, fileName) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')

  link.href = url
  link.download = fileName || 'documento'
  link.click()
  URL.revokeObjectURL(url)
}

function toPositiveInteger(value) {
  const parsed = Number(value)

  return Number.isInteger(parsed) && parsed > 0
    ? parsed
    : null
}

function normalizeText(value) {
  const normalized = value.trim()

  return normalized.length > 0 ? normalized : null
}

function toInputDate(value) {
  if (!value) {
    return ''
  }

  return value.slice(0, 10)
}

function formatEmployeeOption(employee) {
  return [
    `${employee.firstName} ${employee.lastName}`,
    employee.identificationNumber,
    employee.departmentName,
  ]
    .filter(Boolean)
    .join(' · ')
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
