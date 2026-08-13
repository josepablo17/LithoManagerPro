import {
  CheckCircle2,
  Edit3,
  Plus,
  RefreshCcw,
  Search,
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
  createDepartment,
  getDepartments,
  setDepartmentStatus,
  updateDepartment,
} from '../services/departmentService.js'
import departmentStyles from './DepartmentsPage.module.css'

const emptyForm = {
  departmentCode: '',
  name: '',
  description: '',
}

const statusFilterValues = {
  all: null,
  active: true,
  inactive: false,
}

export function DepartmentsPage() {
  const { accessToken } = useAuth()
  const [departments, setDepartments] = useState([])
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [modalState, setModalState] = useState(null)
  const [formValues, setFormValues] = useState(emptyForm)
  const [formErrors, setFormErrors] = useState({})
  const [submitError, setSubmitError] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [statusModalDepartment, setStatusModalDepartment] =
    useState(null)
  const [statusActionId, setStatusActionId] = useState(null)

  const selectedStatus = statusFilterValues[statusFilter]

  const loadDepartments = useCallback(async ({
    signal,
    silent = false,
  } = {}) => {
    if (!silent) {
      setIsLoading(true)
    }

    setLoadError('')

    try {
      const result = await getDepartments({
        accessToken,
        searchTerm: debouncedSearchTerm,
        isActive: selectedStatus,
        signal,
      })

      setDepartments(result)
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
  }, [accessToken, debouncedSearchTerm, selectedStatus])

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedSearchTerm(searchTerm)
    }, 250)

    return () => window.clearTimeout(timeoutId)
  }, [searchTerm])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadDepartments({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadDepartments])

  const summary = useMemo(() => {
    const active = departments.filter(
      (department) => department.isActive,
    ).length

    return {
      total: departments.length,
      active,
      inactive: departments.length - active,
    }
  }, [departments])

  function openCreateModal() {
    setModalState({
      mode: 'create',
      department: null,
    })
    setFormValues(emptyForm)
    setFormErrors({})
    setSubmitError('')
  }

  function openEditModal(department) {
    setModalState({
      mode: 'edit',
      department,
    })
    setFormValues({
      departmentCode: department.departmentCode,
      name: department.name,
      description: department.description ?? '',
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

  async function handleSubmit(event) {
    event.preventDefault()

    const validationErrors = validateDepartmentForm(formValues)

    if (Object.keys(validationErrors).length > 0) {
      setFormErrors(validationErrors)
      return
    }

    const payload = {
      departmentCode: formValues.departmentCode.trim(),
      name: formValues.name.trim(),
      description: normalizeDescription(
        formValues.description,
      ),
    }

    setIsSaving(true)
    setSubmitError('')

    try {
      if (modalState.mode === 'create') {
        await createDepartment({
          accessToken,
          ...payload,
        })
      } else {
        await updateDepartment({
          accessToken,
          departmentId:
            modalState.department.departmentId,
          expectedRowVersion:
            modalState.department.rowVersion,
          ...payload,
        })
      }

      setModalState(null)
      setFormValues(emptyForm)
      setFormErrors({})

      await loadDepartments({
        silent: true,
      })
    } catch (error) {
      setSubmitError(error.message)
    } finally {
      setIsSaving(false)
    }
  }

  function openStatusModal(department) {
    setStatusModalDepartment(department)
    setLoadError('')
  }

  function closeStatusModal() {
    if (statusActionId !== null) {
      return
    }

    setStatusModalDepartment(null)
  }

  async function handleStatusChange() {
    const department = statusModalDepartment

    if (!department) {
      return
    }

    const nextStatus = !department.isActive

    setStatusActionId(department.departmentId)
    setLoadError('')

    try {
      await setDepartmentStatus({
        accessToken,
        departmentId: department.departmentId,
        isActive: nextStatus,
        expectedRowVersion: department.rowVersion,
      })

      await loadDepartments({
        silent: true,
      })

      setStatusModalDepartment(null)
    } catch (error) {
      setLoadError(error.message)
    } finally {
      setStatusActionId(null)
    }
  }

  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Departamentos</h1>
          <p>Administra la estructura organizacional de la empresa.</p>
        </div>
        <Button onClick={openCreateModal}>
          <Plus size={18} aria-hidden="true" />
          Agregar
        </Button>
      </div>

      <div className={departmentStyles.summaryBar}>
        <SummaryItem label="Total" value={summary.total} />
        <SummaryItem label="Activos" value={summary.active} />
        <SummaryItem label="Inactivos" value={summary.inactive} />
      </div>

      {loadError ? (
        <Alert variant="danger">{loadError}</Alert>
      ) : null}

      <div className={styles.toolbar}>
        <div className={departmentStyles.toolbarGroup}>
          <div className={departmentStyles.searchBox}>
            <Search
              className={departmentStyles.searchIcon}
              size={18}
              aria-hidden="true"
            />
            <input
              className={departmentStyles.searchInput}
              type="search"
              placeholder="Buscar departamento"
              aria-label="Buscar departamento"
              maxLength={100}
              value={searchTerm}
              onChange={(event) =>
                setSearchTerm(event.target.value)}
            />
          </div>

          <select
            className={styles.select}
            aria-label="Filtrar por estado"
            value={statusFilter}
            onChange={(event) =>
              setStatusFilter(event.target.value)}
          >
            <option value="all">Todos los estados</option>
            <option value="active">Activos</option>
            <option value="inactive">Inactivos</option>
          </select>
        </div>

        <Button
          variant="secondary"
          onClick={() => loadDepartments()}
          disabled={isLoading}
        >
          <RefreshCcw size={18} aria-hidden="true" />
          Actualizar
        </Button>
      </div>

      <div className={styles.tableShell}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Código</th>
              <th>Nombre</th>
              <th>Estado</th>
              <th>Actualizado</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td
                  colSpan="5"
                  className={departmentStyles.loadingCell}
                >
                  Cargando departamentos
                </td>
              </tr>
            ) : null}

            {!isLoading && departments.length === 0 ? (
              <tr>
                <td
                  colSpan="5"
                  className={departmentStyles.emptyCell}
                >
                  No hay departamentos para mostrar
                </td>
              </tr>
            ) : null}

            {!isLoading
              ? departments.map((department) => (
                <tr key={department.departmentId}>
                  <td>
                    <strong>{department.departmentCode}</strong>
                  </td>
                  <td>
                    <div className={departmentStyles.departmentName}>
                      <strong>{department.name}</strong>
                      {department.description ? (
                        <span>{department.description}</span>
                      ) : null}
                    </div>
                  </td>
                  <td>
                    <Badge
                      variant={
                        department.isActive
                          ? 'success'
                          : 'neutral'
                      }
                    >
                      {department.isActive
                        ? 'Activo'
                        : 'Inactivo'}
                    </Badge>
                  </td>
                  <td className={departmentStyles.muted}>
                    {formatDate(
                      department.updatedAtUtc
                        ?? department.createdAtUtc,
                    )}
                  </td>
                  <td>
                    <div className={departmentStyles.actionsCell}>
                      <Button
                        variant="secondary"
                        size="small"
                        title="Editar departamento"
                        onClick={() => openEditModal(department)}
                      >
                        <Edit3 size={16} aria-hidden="true" />
                        Editar
                      </Button>
                      <Button
                        variant={
                          department.isActive
                            ? 'danger'
                            : 'secondary'
                        }
                        size="small"
                        title={
                          department.isActive
                            ? 'Desactivar departamento'
                            : 'Activar departamento'
                        }
                        isLoading={
                          statusActionId
                            === department.departmentId
                        }
                        loadingText="Procesando..."
                        onClick={() =>
                          openStatusModal(department)}
                      >
                        {department.isActive ? (
                          <XCircle size={16} aria-hidden="true" />
                        ) : (
                          <CheckCircle2 size={16} aria-hidden="true" />
                        )}
                        {department.isActive
                          ? 'Desactivar'
                          : 'Activar'}
                      </Button>
                    </div>
                  </td>
                </tr>
              ))
              : null}
          </tbody>
        </table>
      </div>

      {modalState ? (
        <DepartmentModal
          mode={modalState.mode}
          values={formValues}
          errors={formErrors}
          submitError={submitError}
          isSaving={isSaving}
          onChange={handleFieldChange}
          onClose={closeModal}
          onSubmit={handleSubmit}
        />
      ) : null}

      {statusModalDepartment ? (
        <DepartmentStatusModal
          department={statusModalDepartment}
          isSaving={
            statusActionId
              === statusModalDepartment.departmentId
          }
          onClose={closeStatusModal}
          onConfirm={handleStatusChange}
        />
      ) : null}
    </section>
  )
}

function SummaryItem({ label, value }) {
  return (
    <div className={departmentStyles.summaryItem}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function DepartmentModal({
  mode,
  values,
  errors,
  submitError,
  isSaving,
  onChange,
  onClose,
  onSubmit,
}) {
  const title = mode === 'create'
    ? 'Nuevo departamento'
    : 'Editar departamento'

  return (
    <div className={departmentStyles.modalOverlay}>
      <section
        className={departmentStyles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="department-form-title"
      >
        <div className={departmentStyles.modalHeader}>
          <h2 id="department-form-title">{title}</h2>
          <Button
            variant="ghost"
            size="small"
            aria-label="Cerrar"
            onClick={onClose}
          >
            <X size={18} aria-hidden="true" />
          </Button>
        </div>

        <form className={departmentStyles.form} onSubmit={onSubmit}>
          {submitError ? (
            <Alert variant="danger">{submitError}</Alert>
          ) : null}

          <TextInput
            id="department-code"
            name="departmentCode"
            label="Código"
            maxLength={50}
            required
            value={values.departmentCode}
            error={errors.departmentCode}
            onChange={onChange}
          />

          <TextInput
            id="department-name"
            name="name"
            label="Nombre"
            maxLength={100}
            required
            value={values.name}
            error={errors.name}
            onChange={onChange}
          />

          <div className={departmentStyles.field}>
            <label
              className={departmentStyles.label}
              htmlFor="department-description"
            >
              Descripción
            </label>
            <textarea
              id="department-description"
              name="description"
              className={
                errors.description
                  ? departmentStyles.textareaError
                  : departmentStyles.textarea
              }
              maxLength={300}
              value={values.description}
              aria-invalid={Boolean(errors.description)}
              aria-describedby="department-description-footer"
              onChange={onChange}
            />
            <div
              id="department-description-footer"
              className={departmentStyles.fieldFooter}
            >
              <span className={errors.description
                ? departmentStyles.errorText
                : undefined}
              >
                {errors.description ?? ''}
              </span>
              <span>{values.description.length}/300</span>
            </div>
          </div>

          <div className={departmentStyles.formActions}>
            <Button
              variant="secondary"
              onClick={onClose}
              disabled={isSaving}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              isLoading={isSaving}
              loadingText="Guardando..."
            >
              Guardar
            </Button>
          </div>
        </form>
      </section>
    </div>
  )
}

function DepartmentStatusModal({
  department,
  isSaving,
  onClose,
  onConfirm,
}) {
  const nextStatus = !department.isActive
  const title = nextStatus
    ? 'Activar departamento'
    : 'Desactivar departamento'
  const actionText = nextStatus
    ? 'Activar'
    : 'Desactivar'

  return (
    <div className={departmentStyles.modalOverlay}>
      <section
        className={`${departmentStyles.modal} ${departmentStyles.confirmModal}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="department-status-title"
      >
        <div className={departmentStyles.modalHeader}>
          <div>
            <h2 id="department-status-title">{title}</h2>
            <p>
              {department.departmentCode} · {department.name}
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

        <div className={departmentStyles.confirmBody}>
          <p>
            {nextStatus
              ? 'El departamento volverá a estar disponible para nuevas operaciones.'
              : 'El departamento dejará de estar disponible para nuevas asignaciones.'}
          </p>
          {!nextStatus ? (
            <p className={departmentStyles.warningText}>
              Revisa que no tenga empleados activos antes de continuar.
            </p>
          ) : null}
        </div>

        <div className={departmentStyles.confirmActions}>
          <Button
            variant="secondary"
            disabled={isSaving}
            onClick={onClose}
          >
            Cancelar
          </Button>
          <Button
            variant={nextStatus ? 'primary' : 'danger'}
            isLoading={isSaving}
            loadingText="Procesando..."
            onClick={onConfirm}
          >
            {nextStatus ? (
              <CheckCircle2 size={18} aria-hidden="true" />
            ) : (
              <XCircle size={18} aria-hidden="true" />
            )}
            {actionText}
          </Button>
        </div>
      </section>
    </div>
  )
}

function validateDepartmentForm(values) {
  const errors = {}
  const departmentCode = values.departmentCode.trim()
  const name = values.name.trim()
  const description = values.description.trim()

  if (!departmentCode) {
    errors.departmentCode = 'El código es requerido.'
  } else if (departmentCode.length > 50) {
    errors.departmentCode = 'Máximo 50 caracteres.'
  } else if (departmentCode.includes(' ')) {
    errors.departmentCode = 'El código no debe contener espacios.'
  }

  if (!name) {
    errors.name = 'El nombre es requerido.'
  } else if (name.length > 100) {
    errors.name = 'Máximo 100 caracteres.'
  }

  if (description.length > 300) {
    errors.description = 'Máximo 300 caracteres.'
  }

  return errors
}

function normalizeDescription(description) {
  const normalizedDescription = description.trim()

  return normalizedDescription
    ? normalizedDescription
    : null
}

function formatDate(value) {
  if (!value) {
    return 'Sin cambios'
  }

  return new Intl.DateTimeFormat('es-GT', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
