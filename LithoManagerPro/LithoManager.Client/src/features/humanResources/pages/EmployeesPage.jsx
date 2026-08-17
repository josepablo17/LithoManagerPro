import {
  CheckCircle2,
  Edit3,
  History,
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
import { getDepartments } from '../services/departmentService.js'
import {
  createEmployee,
  getAssignableEmployeeUsers,
  getEmployeeIdentificationTypes,
  getEmployeeSalaryHistory,
  getEmployees,
  setEmployeeStatus,
  updateEmployee,
} from '../services/employeeService.js'
import employeeStyles from './EmployeesPage.module.css'

const emptyForm = {
  userId: '',
  userEmailAddress: '',
  departmentId: '',
  identificationType: 'CEDULA_FISICA',
  identificationNumber: '',
  firstName: '',
  lastName: '',
  phoneNumber: '',
  birthDate: '',
  hireDate: '',
  terminationDate: '',
  jobTitle: '',
  baseSalary: '',
  profileImagePath: '',
}

const emptyHistoryFilters = {
  effectiveFromDate: '',
  effectiveToDate: '',
}

const statusFilterValues = {
  all: null,
  active: true,
  inactive: false,
}

export function EmployeesPage() {
  const { accessToken } = useAuth()
  const [employees, setEmployees] = useState([])
  const [departments, setDepartments] = useState([])
  const [assignableUsers, setAssignableUsers] = useState([])
  const [identificationTypes, setIdentificationTypes] =
    useState([])
  const [searchTerm, setSearchTerm] = useState('')
  const [debouncedSearchTerm, setDebouncedSearchTerm] =
    useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [departmentFilter, setDepartmentFilter] = useState('')
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState('')
  const [modalState, setModalState] = useState(null)
  const [formValues, setFormValues] = useState(emptyForm)
  const [formErrors, setFormErrors] = useState({})
  const [submitError, setSubmitError] = useState('')
  const [isSaving, setIsSaving] = useState(false)
  const [isUserOptionsLoading, setIsUserOptionsLoading] =
    useState(false)
  const [statusModalEmployee, setStatusModalEmployee] =
    useState(null)
  const [statusActionId, setStatusActionId] = useState(null)
  const [historyModalEmployee, setHistoryModalEmployee] =
    useState(null)
  const [salaryHistory, setSalaryHistory] = useState([])
  const [historyFilters, setHistoryFilters] = useState(
    emptyHistoryFilters,
  )
  const [isHistoryLoading, setIsHistoryLoading] = useState(false)
  const [historyError, setHistoryError] = useState('')

  const selectedStatus = statusFilterValues[statusFilter]

  const loadEmployees = useCallback(async ({
    signal,
    silent = false,
  } = {}) => {
    if (!silent) {
      setIsLoading(true)
    }

    setLoadError('')

    try {
      const result = await getEmployees({
        accessToken,
        searchTerm: debouncedSearchTerm,
        departmentId: toPositiveInteger(departmentFilter),
        isActive: selectedStatus,
        signal,
      })

      setEmployees(result)
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
    debouncedSearchTerm,
    departmentFilter,
    selectedStatus,
  ])

  const loadDepartments = useCallback(async ({ signal } = {}) => {
    try {
      const result = await getDepartments({
        accessToken,
        isActive: null,
        signal,
      })

      setDepartments(result)
    } catch (error) {
      if (error.name !== 'AbortError') {
        setLoadError(error.message)
      }
    }
  }, [accessToken])

  const loadAssignableUsers = useCallback(async ({
    employeeId,
    signal,
  } = {}) => {
    setIsUserOptionsLoading(true)

    try {
      const result = await getAssignableEmployeeUsers({
        accessToken,
        employeeId,
        signal,
      })

      setAssignableUsers(result)
    } catch (error) {
      if (error.name !== 'AbortError') {
        setLoadError(error.message)
      }
    } finally {
      if (!signal?.aborted) {
        setIsUserOptionsLoading(false)
      }
    }
  }, [accessToken])

  const loadIdentificationTypes = useCallback(async ({
    signal,
  } = {}) => {
    try {
      const result = await getEmployeeIdentificationTypes({
        accessToken,
        signal,
      })

      setIdentificationTypes(result)
    } catch (error) {
      if (error.name !== 'AbortError') {
        setLoadError(error.message)
      }
    }
  }, [accessToken])

  const loadSalaryHistory = useCallback(async ({
    employee,
    filters = historyFilters,
    signal,
  }) => {
    setIsHistoryLoading(true)
    setHistoryError('')

    try {
      const result = await getEmployeeSalaryHistory({
        accessToken,
        employeeId: employee.employeeId,
        effectiveFromDate: filters.effectiveFromDate,
        effectiveToDate: filters.effectiveToDate,
        signal,
      })

      setSalaryHistory(result)
    } catch (error) {
      if (error.name === 'AbortError') {
        return
      }

      setHistoryError(error.message)
    } finally {
      if (!signal?.aborted) {
        setIsHistoryLoading(false)
      }
    }
  }, [accessToken, historyFilters])

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

      loadAssignableUsers({
        signal: controller.signal,
      })

      loadIdentificationTypes({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [
    loadAssignableUsers,
    loadDepartments,
    loadIdentificationTypes,
  ])

  useEffect(() => {
    const controller = new AbortController()
    const timeoutId = window.setTimeout(() => {
      loadEmployees({
        signal: controller.signal,
      })
    }, 0)

    return () => {
      window.clearTimeout(timeoutId)
      controller.abort()
    }
  }, [loadEmployees])

  const summary = useMemo(() => {
    const active = employees.filter(
      (employee) => employee.isActive,
    ).length
    const linkedUsers = employees.filter(
      (employee) => Boolean(employee.userId),
    ).length

    return {
      total: employees.length,
      active,
      inactive: employees.length - active,
      linkedUsers,
    }
  }, [employees])

  function openCreateModal() {
    setModalState({
      mode: 'create',
      employee: null,
    })
    setFormValues(createEmptyForm(identificationTypes))
    setFormErrors({})
    setSubmitError('')
    loadAssignableUsers()
  }

  function openEditModal(employee) {
    setModalState({
      mode: 'edit',
      employee,
    })
    setFormValues({
      userId: employee.userId ? String(employee.userId) : '',
      userEmailAddress: employee.emailAddress ?? '',
      departmentId: String(employee.departmentId),
      identificationType:
        employee.identificationType
        ?? getDefaultIdentificationType(identificationTypes),
      identificationNumber: employee.identificationNumber,
      firstName: employee.firstName,
      lastName: employee.lastName,
      phoneNumber: employee.phoneNumber ?? '',
      birthDate: toDateInputValue(employee.birthDate),
      hireDate: toDateInputValue(employee.hireDate),
      terminationDate: toDateInputValue(employee.terminationDate),
      jobTitle: employee.jobTitle,
      baseSalary: String(employee.baseSalary),
      profileImagePath: employee.profileImagePath ?? '',
    })
    setFormErrors({})
    setSubmitError('')
    loadAssignableUsers({
      employeeId: employee.employeeId,
    })
  }

  function closeModal() {
    if (isSaving) {
      return
    }

    setModalState(null)
    setFormValues(createEmptyForm(identificationTypes))
    setFormErrors({})
    setSubmitError('')
  }

  function handleFieldChange(event) {
    const { name, value } = event.target

    if (name === 'identificationType') {
      const selectedIdentificationType =
        getIdentificationTypeByCode(identificationTypes, value)

      setFormValues((current) => ({
        ...current,
        identificationType: value,
        identificationNumber: sanitizeIdentificationNumber(
          current.identificationNumber,
          selectedIdentificationType,
        ),
      }))

      setFormErrors((current) => ({
        ...current,
        identificationType: undefined,
        identificationNumber: undefined,
      }))

      return
    }

    if (name === 'userId') {
      const selectedUser = assignableUsers.find(
        (user) => String(user.userId) === value,
      )

      setFormValues((current) => ({
        ...current,
        userId: value,
        userEmailAddress: selectedUser?.emailAddress ?? '',
      }))

      setFormErrors((current) => ({
        ...current,
        userId: undefined,
      }))

      return
    }

    if (name === 'identificationNumber') {
      const selectedIdentificationType =
        getIdentificationTypeByCode(
          identificationTypes,
          formValues.identificationType,
        )

      setFormValues((current) => ({
        ...current,
        identificationNumber: sanitizeIdentificationNumber(
          value,
          selectedIdentificationType,
        ),
      }))

      setFormErrors((current) => ({
        ...current,
        identificationNumber: undefined,
      }))

      return
    }

    if (name === 'phoneNumber') {
      setFormValues((current) => ({
        ...current,
        phoneNumber: value.replace(/\D/g, '').slice(0, 8),
      }))

      setFormErrors((current) => ({
        ...current,
        phoneNumber: undefined,
      }))

      return
    }

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

    const validationErrors = validateEmployeeForm(
      formValues,
      identificationTypes,
    )

    if (Object.keys(validationErrors).length > 0) {
      setFormErrors(validationErrors)
      return
    }

    const payload = createEmployeePayload(formValues)

    setIsSaving(true)
    setSubmitError('')

    try {
      if (modalState.mode === 'create') {
        await createEmployee({
          accessToken,
          employee: payload,
        })
      } else {
        await updateEmployee({
          accessToken,
          employeeId: modalState.employee.employeeId,
          employee: {
            ...payload,
            expectedRowVersion: modalState.employee.rowVersion,
          },
        })
      }

      setModalState(null)
      setFormValues(createEmptyForm(identificationTypes))
      setFormErrors({})

      await loadEmployees({
        silent: true,
      })
    } catch (error) {
      setSubmitError(error.message)
    } finally {
      setIsSaving(false)
    }
  }

  function openStatusModal(employee) {
    setStatusModalEmployee(employee)
    setLoadError('')
  }

  function closeStatusModal() {
    if (statusActionId !== null) {
      return
    }

    setStatusModalEmployee(null)
  }

  async function handleStatusChange() {
    const employee = statusModalEmployee

    if (!employee) {
      return
    }

    const nextStatus = !employee.isActive

    setStatusActionId(employee.employeeId)
    setLoadError('')

    try {
      await setEmployeeStatus({
        accessToken,
        employeeId: employee.employeeId,
        isActive: nextStatus,
        expectedRowVersion: employee.rowVersion,
      })

      await loadEmployees({
        silent: true,
      })

      setStatusModalEmployee(null)
    } catch (error) {
      setLoadError(error.message)
    } finally {
      setStatusActionId(null)
    }
  }

  function openHistoryModal(employee) {
    setHistoryModalEmployee(employee)
    setHistoryFilters(emptyHistoryFilters)
    setSalaryHistory([])
    setHistoryError('')
    loadSalaryHistory({
      employee,
      filters: emptyHistoryFilters,
    })
  }

  function closeHistoryModal() {
    if (isHistoryLoading) {
      return
    }

    setHistoryModalEmployee(null)
    setSalaryHistory([])
    setHistoryError('')
  }

  function handleHistoryFilterChange(event) {
    const { name, value } = event.target

    setHistoryFilters((current) => ({
      ...current,
      [name]: value,
    }))
  }

  async function handleHistorySubmit(event) {
    event.preventDefault()

    if (!historyModalEmployee) {
      return
    }

    await loadSalaryHistory({
      employee: historyModalEmployee,
      filters: historyFilters,
    })
  }

  return (
    <section className={styles.page}>
      <div className={styles.pageHeader}>
        <div>
          <h1>Empleados</h1>
          <p>
            Gestiona la información laboral, el vínculo con usuarios y
            el historial salarial.
          </p>
        </div>
        <Button onClick={openCreateModal}>
          <Plus size={18} aria-hidden="true" />
          Agregar
        </Button>
      </div>

      <div className={employeeStyles.summaryBar}>
        <SummaryItem label="Total" value={summary.total} />
        <SummaryItem label="Activos" value={summary.active} />
        <SummaryItem label="Inactivos" value={summary.inactive} />
        <SummaryItem
          label="Con usuario"
          value={summary.linkedUsers}
        />
      </div>

      {loadError ? (
        <Alert variant="danger">{loadError}</Alert>
      ) : null}

      <div className={styles.toolbar}>
        <div className={employeeStyles.toolbarGroup}>
          <div className={employeeStyles.searchBox}>
            <Search
              className={employeeStyles.searchIcon}
              size={18}
              aria-hidden="true"
            />
            <input
              className={employeeStyles.searchInput}
              type="search"
              placeholder="Buscar empleado"
              aria-label="Buscar empleado"
              maxLength={100}
              value={searchTerm}
              onChange={(event) =>
                setSearchTerm(event.target.value)}
            />
          </div>

          <select
            className={styles.select}
            aria-label="Filtrar por departamento"
            value={departmentFilter}
            onChange={(event) =>
              setDepartmentFilter(event.target.value)}
          >
            <option value="">Todos los departamentos</option>
            {departments.map((department) => (
              <option
                key={department.departmentId}
                value={department.departmentId}
              >
                {department.departmentCode} · {department.name}
              </option>
            ))}
          </select>

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
          onClick={() => loadEmployees()}
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
              <th>Empleado</th>
              <th>Departamento</th>
              <th>Puesto</th>
              <th>Salario</th>
              <th>Estado</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td
                  colSpan="6"
                  className={employeeStyles.loadingCell}
                >
                  Cargando empleados
                </td>
              </tr>
            ) : null}

            {!isLoading && employees.length === 0 ? (
              <tr>
                <td
                  colSpan="6"
                  className={employeeStyles.emptyCell}
                >
                  No hay empleados para mostrar
                </td>
              </tr>
            ) : null}

            {!isLoading
              ? employees.map((employee) => (
                <tr key={employee.employeeId}>
                  <td>
                    <div className={employeeStyles.employeeName}>
                      <strong>
                        {employee.firstName} {employee.lastName}
                      </strong>
                      <span>
                        {employee.identificationNumber}
                        {' · '}
                        {getIdentificationTypeLabel(
                          identificationTypes,
                          employee.identificationType,
                        )}
                        {employee.emailAddress
                          ? ` · ${employee.emailAddress}`
                          : ''}
                      </span>
                    </div>
                  </td>
                  <td>
                    <div className={employeeStyles.employeeName}>
                      <strong>{employee.departmentName}</strong>
                      <span>{employee.departmentCode}</span>
                    </div>
                  </td>
                  <td>
                    <div className={employeeStyles.employeeName}>
                      <strong>{employee.jobTitle}</strong>
                      <span>
                        Ingreso {formatDateOnly(employee.hireDate)}
                      </span>
                    </div>
                  </td>
                  <td>{formatCurrency(employee.baseSalary)}</td>
                  <td>
                    <Badge
                      variant={
                        employee.isActive ? 'success' : 'neutral'
                      }
                    >
                      {employee.isActive ? 'Activo' : 'Inactivo'}
                    </Badge>
                  </td>
                  <td>
                    <div className={employeeStyles.actionsCell}>
                      <Button
                        variant="secondary"
                        size="small"
                        title="Ver historial salarial"
                        onClick={() => openHistoryModal(employee)}
                      >
                        <History size={16} aria-hidden="true" />
                        Historial
                      </Button>
                      <Button
                        variant="secondary"
                        size="small"
                        title="Editar empleado"
                        onClick={() => openEditModal(employee)}
                      >
                        <Edit3 size={16} aria-hidden="true" />
                        Editar
                      </Button>
                      <Button
                        variant={
                          employee.isActive
                            ? 'danger'
                            : 'secondary'
                        }
                        size="small"
                        title={
                          employee.isActive
                            ? 'Desactivar empleado'
                            : 'Activar empleado'
                        }
                        isLoading={
                          statusActionId === employee.employeeId
                        }
                        loadingText="Procesando..."
                        onClick={() => openStatusModal(employee)}
                      >
                        {employee.isActive ? (
                          <XCircle size={16} aria-hidden="true" />
                        ) : (
                          <CheckCircle2
                            size={16}
                            aria-hidden="true"
                          />
                        )}
                        {employee.isActive
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
        <EmployeeModal
          mode={modalState.mode}
          values={formValues}
          departments={departments}
          assignableUsers={assignableUsers}
          identificationTypes={identificationTypes}
          errors={formErrors}
          submitError={submitError}
          isSaving={isSaving}
          isUserOptionsLoading={isUserOptionsLoading}
          onChange={handleFieldChange}
          onClose={closeModal}
          onSubmit={handleSubmit}
        />
      ) : null}

      {statusModalEmployee ? (
        <EmployeeStatusModal
          employee={statusModalEmployee}
          isSaving={
            statusActionId === statusModalEmployee.employeeId
          }
          onClose={closeStatusModal}
          onConfirm={handleStatusChange}
        />
      ) : null}

      {historyModalEmployee ? (
        <SalaryHistoryModal
          employee={historyModalEmployee}
          filters={historyFilters}
          salaryHistory={salaryHistory}
          isLoading={isHistoryLoading}
          error={historyError}
          onChange={handleHistoryFilterChange}
          onSubmit={handleHistorySubmit}
          onClose={closeHistoryModal}
        />
      ) : null}
    </section>
  )
}

function SummaryItem({ label, value }) {
  return (
    <div className={employeeStyles.summaryItem}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  )
}

function EmployeeModal({
  mode,
  values,
  departments,
  assignableUsers,
  identificationTypes,
  errors,
  submitError,
  isSaving,
  isUserOptionsLoading,
  onChange,
  onClose,
  onSubmit,
}) {
  const title = mode === 'create'
    ? 'Nuevo empleado'
    : 'Editar empleado'
  const selectedIdentificationType =
    getIdentificationTypeByCode(
      identificationTypes,
      values.identificationType,
    )
  const selectedUserIsMissing =
    values.userId
    && !assignableUsers.some(
      (user) => String(user.userId) === values.userId,
    )

  return (
    <div className={employeeStyles.modalOverlay}>
      <section
        className={employeeStyles.modal}
        role="dialog"
        aria-modal="true"
        aria-labelledby="employee-form-title"
      >
        <div className={employeeStyles.modalHeader}>
          <h2 id="employee-form-title">{title}</h2>
          <Button
            variant="ghost"
            size="small"
            aria-label="Cerrar"
            onClick={onClose}
          >
            <X size={18} aria-hidden="true" />
          </Button>
        </div>

        <form className={employeeStyles.form} onSubmit={onSubmit}>
          {submitError ? (
            <Alert variant="danger">{submitError}</Alert>
          ) : null}

          <div className={employeeStyles.formGrid}>
            <SelectField
              id="employee-identification-type"
              name="identificationType"
              label="Tipo de identificación"
              required
              value={values.identificationType}
              error={errors.identificationType}
              onChange={onChange}
            >
              {identificationTypes.length === 0 ? (
                <option value="CEDULA_FISICA">Cédula física</option>
              ) : null}
              {identificationTypes.map((identificationType) => (
                <option
                  key={identificationType.identificationType}
                  value={identificationType.identificationType}
                >
                  {identificationType.name}
                </option>
              ))}
            </SelectField>

            <TextInput
              id="employee-identification-number"
              name="identificationNumber"
              label="Identificación"
              inputMode={
                selectedIdentificationType?.isNumericOnly
                  ? 'numeric'
                  : 'text'
              }
              maxLength={
                selectedIdentificationType?.maxLength ?? 20
              }
              required
              value={values.identificationNumber}
              error={errors.identificationNumber}
              helperText={getIdentificationHelperText(
                selectedIdentificationType,
              )}
              onChange={onChange}
            />

            <SelectField
              id="employee-department-id"
              name="departmentId"
              label="Departamento"
              required
              value={values.departmentId}
              error={errors.departmentId}
              onChange={onChange}
            >
              <option value="">Selecciona un departamento</option>
              {departments.map((department) => (
                <option
                  key={department.departmentId}
                  value={department.departmentId}
                  disabled={!department.isActive}
                >
                  {department.departmentCode} · {department.name}
                  {department.isActive ? '' : ' · Inactivo'}
                </option>
              ))}
            </SelectField>

            <TextInput
              id="employee-first-name"
              name="firstName"
              label="Nombre"
              maxLength={100}
              required
              value={values.firstName}
              error={errors.firstName}
              onChange={onChange}
            />

            <TextInput
              id="employee-last-name"
              name="lastName"
              label="Apellidos"
              maxLength={150}
              required
              value={values.lastName}
              error={errors.lastName}
              onChange={onChange}
            />

            <TextInput
              id="employee-job-title"
              name="jobTitle"
              label="Puesto"
              maxLength={100}
              required
              value={values.jobTitle}
              error={errors.jobTitle}
              onChange={onChange}
            />

            <TextInput
              id="employee-base-salary"
              name="baseSalary"
              label="Salario base"
              type="number"
              min="0.01"
              step="0.01"
              required
              value={values.baseSalary}
              error={errors.baseSalary}
              onChange={onChange}
            />

            <TextInput
              id="employee-hire-date"
              name="hireDate"
              label="Fecha de ingreso"
              type="date"
              required
              value={values.hireDate}
              error={errors.hireDate}
              onChange={onChange}
            />

            <TextInput
              id="employee-termination-date"
              name="terminationDate"
              label="Fecha de salida"
              type="date"
              value={values.terminationDate}
              error={errors.terminationDate}
              onChange={onChange}
            />

            <TextInput
              id="employee-birth-date"
              name="birthDate"
              label="Fecha de nacimiento"
              type="date"
              value={values.birthDate}
              error={errors.birthDate}
              onChange={onChange}
            />

            <TextInput
              id="employee-phone-number"
              name="phoneNumber"
              label="Teléfono"
              inputMode="numeric"
              maxLength={8}
              value={values.phoneNumber}
              error={errors.phoneNumber}
              helperText="Opcional, 8 dígitos sin extensión."
              onChange={onChange}
            />

            <SelectField
              id="employee-user-id"
              name="userId"
              label="Usuario vinculado"
              value={values.userId}
              error={errors.userId}
              helperText={
                isUserOptionsLoading
                  ? 'Cargando usuarios disponibles.'
                  : 'Opcional, solo se muestran usuarios disponibles.'
              }
              onChange={onChange}
            >
              <option value="">Sin usuario vinculado</option>
              {selectedUserIsMissing ? (
                <option value={values.userId}>
                  {values.userEmailAddress || 'Usuario actual'}
                </option>
              ) : null}
              {assignableUsers.map((user) => (
                <option
                  key={user.userId}
                  value={user.userId}
                >
                  {user.emailAddress} · {user.roleName}
                </option>
              ))}
            </SelectField>

            <TextInput
              id="employee-profile-image-path"
              name="profileImagePath"
              label="Ruta de foto"
              maxLength={500}
              value={values.profileImagePath}
              error={errors.profileImagePath}
              onChange={onChange}
            />
          </div>

          <div className={employeeStyles.formActions}>
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

function SelectField({
  id,
  name,
  label,
  value,
  error,
  helperText,
  required = false,
  children,
  onChange,
}) {
  const descriptionId = error || helperText
    ? `${id}-description`
    : undefined

  return (
    <div className={employeeStyles.field}>
      <label className={employeeStyles.label} htmlFor={id}>
        {label}
        {required ? <span aria-hidden="true"> *</span> : null}
      </label>
      <select
        id={id}
        name={name}
        className={
          error ? employeeStyles.selectError : employeeStyles.select
        }
        required={required}
        value={value}
        aria-invalid={Boolean(error)}
        aria-describedby={descriptionId}
        onChange={onChange}
      >
        {children}
      </select>
      {error ? (
        <p
          id={descriptionId}
          className={employeeStyles.errorText}
        >
          {error}
        </p>
      ) : helperText ? (
        <p
          id={descriptionId}
          className={employeeStyles.fieldFooter}
        >
          {helperText}
        </p>
      ) : null}
    </div>
  )
}

function EmployeeStatusModal({
  employee,
  isSaving,
  onClose,
  onConfirm,
}) {
  const nextStatus = !employee.isActive
  const title = nextStatus
    ? 'Activar empleado'
    : 'Desactivar empleado'
  const actionText = nextStatus
    ? 'Activar'
    : 'Desactivar'

  return (
    <div className={employeeStyles.modalOverlay}>
      <section
        className={`${employeeStyles.modal} ${employeeStyles.confirmModal}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="employee-status-title"
      >
        <div className={employeeStyles.modalHeader}>
          <div>
            <h2 id="employee-status-title">{title}</h2>
            <p>
              {employee.identificationNumber} · {employee.firstName}{' '}
              {employee.lastName}
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

        <div className={employeeStyles.confirmBody}>
          <p>
            {nextStatus
              ? 'El empleado volverá a estar disponible para las operaciones administrativas.'
              : 'El empleado dejará de estar disponible para nuevas operaciones administrativas.'}
          </p>
          {!nextStatus ? (
            <p className={employeeStyles.warningText}>
              Si tiene usuario vinculado, no podrá acceder al sistema.
            </p>
          ) : null}
        </div>

        <div className={employeeStyles.confirmActions}>
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

function SalaryHistoryModal({
  employee,
  filters,
  salaryHistory,
  isLoading,
  error,
  onChange,
  onSubmit,
  onClose,
}) {
  return (
    <div className={employeeStyles.modalOverlay}>
      <section
        className={`${employeeStyles.modal} ${employeeStyles.historyModal}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby="employee-salary-history-title"
      >
        <div className={employeeStyles.modalHeader}>
          <div>
            <h2 id="employee-salary-history-title">
              Historial salarial
            </h2>
            <p>
              {employee.identificationNumber} · {employee.firstName}{' '}
              {employee.lastName}
            </p>
          </div>
          <Button
            variant="ghost"
            size="small"
            aria-label="Cerrar"
            disabled={isLoading}
            onClick={onClose}
          >
            <X size={18} aria-hidden="true" />
          </Button>
        </div>

        <form
          className={employeeStyles.historyToolbar}
          onSubmit={onSubmit}
        >
          <div className={employeeStyles.historyFilters}>
            <TextInput
              id="salary-history-from"
              name="effectiveFromDate"
              label="Desde"
              type="date"
              value={filters.effectiveFromDate}
              onChange={onChange}
            />
            <TextInput
              id="salary-history-to"
              name="effectiveToDate"
              label="Hasta"
              type="date"
              value={filters.effectiveToDate}
              onChange={onChange}
            />
          </div>
          <Button
            type="submit"
            variant="secondary"
            isLoading={isLoading}
            loadingText="Consultando..."
          >
            <Search size={18} aria-hidden="true" />
            Consultar
          </Button>
        </form>

        <div className={employeeStyles.historyTableShell}>
          {error ? (
            <Alert variant="danger">{error}</Alert>
          ) : null}

          <table className={employeeStyles.historyTable}>
            <thead>
              <tr>
                <th>Salario</th>
                <th>Desde</th>
                <th>Hasta</th>
                <th>Estado</th>
                <th>Actualizado</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td
                    colSpan="5"
                    className={employeeStyles.loadingCell}
                  >
                    Cargando historial
                  </td>
                </tr>
              ) : null}

              {!isLoading && salaryHistory.length === 0 ? (
                <tr>
                  <td
                    colSpan="5"
                    className={employeeStyles.emptyCell}
                  >
                    No hay movimientos salariales para mostrar
                  </td>
                </tr>
              ) : null}

              {!isLoading
                ? salaryHistory.map((item) => (
                  <tr key={item.employeeSalaryHistoryId}>
                    <td>{formatCurrency(item.baseSalary)}</td>
                    <td>
                      {formatDateOnly(item.effectiveFromDate)}
                    </td>
                    <td>
                      {item.effectiveToDate
                        ? formatDateOnly(item.effectiveToDate)
                        : 'Actual'}
                    </td>
                    <td>
                      <Badge
                        variant={
                          item.isCurrent ? 'success' : 'neutral'
                        }
                      >
                        {item.isCurrent ? 'Actual' : 'Histórico'}
                      </Badge>
                    </td>
                    <td className={employeeStyles.muted}>
                      {formatDate(
                        item.updatedAtUtc ?? item.createdAtUtc,
                      )}
                    </td>
                  </tr>
                ))
                : null}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}

function validateEmployeeForm(values, identificationTypes) {
  const errors = {}
  const userId = toPositiveInteger(values.userId)
  const departmentId = toPositiveInteger(values.departmentId)
  const baseSalary = Number(values.baseSalary)
  const hireDate = values.hireDate
  const terminationDate = values.terminationDate
  const identificationType = getIdentificationTypeByCode(
    identificationTypes,
    values.identificationType,
  )

  if (values.userId && !userId) {
    errors.userId = 'Selecciona un usuario válido.'
  }

  if (!departmentId) {
    errors.departmentId = 'El departamento es requerido.'
  }

  if (!values.identificationType) {
    errors.identificationType =
      'El tipo de identificación es requerido.'
  } else if (
    identificationTypes.length > 0
    && !identificationType
  ) {
    errors.identificationType =
      'Selecciona un tipo de identificación válido.'
  }

  validateIdentificationNumber(
    errors,
    values.identificationNumber,
    identificationType,
  )

  validateRequiredText(
    errors,
    values.firstName,
    'firstName',
    'El nombre es requerido.',
    100,
  )
  validateRequiredText(
    errors,
    values.lastName,
    'lastName',
    'Los apellidos son requeridos.',
    150,
  )
  validateRequiredText(
    errors,
    values.jobTitle,
    'jobTitle',
    'El puesto es requerido.',
    100,
  )

  const phoneNumber = values.phoneNumber.trim()

  if (phoneNumber && !/^\d{8}$/.test(phoneNumber)) {
    errors.phoneNumber = 'Debe tener 8 dígitos.'
  }

  if (values.profileImagePath.trim().length > 500) {
    errors.profileImagePath = 'Máximo 500 caracteres.'
  }

  if (!hireDate) {
    errors.hireDate = 'La fecha de ingreso es requerida.'
  }

  if (terminationDate && hireDate && terminationDate < hireDate) {
    errors.terminationDate =
      'La fecha de salida no puede ser anterior al ingreso.'
  }

  if (!values.baseSalary) {
    errors.baseSalary = 'El salario base es requerido.'
  } else if (!Number.isFinite(baseSalary) || baseSalary <= 0) {
    errors.baseSalary = 'El salario debe ser mayor a cero.'
  }

  return errors
}

function validateRequiredText(
  errors,
  value,
  key,
  requiredMessage,
  maxLength,
) {
  const normalizedValue = value.trim()

  if (!normalizedValue) {
    errors[key] = requiredMessage
  } else if (normalizedValue.length > maxLength) {
    errors[key] = `Máximo ${maxLength} caracteres.`
  }
}

function createEmployeePayload(values) {
  return {
    userId: toPositiveInteger(values.userId),
    departmentId: toPositiveInteger(values.departmentId),
    identificationType: values.identificationType,
    identificationNumber: values.identificationNumber.trim(),
    firstName: values.firstName.trim(),
    lastName: values.lastName.trim(),
    phoneNumber: normalizeOptionalText(values.phoneNumber),
    birthDate: normalizeOptionalText(values.birthDate),
    hireDate: values.hireDate,
    terminationDate: normalizeOptionalText(values.terminationDate),
    jobTitle: values.jobTitle.trim(),
    baseSalary: Number(values.baseSalary),
    profileImagePath: normalizeOptionalText(values.profileImagePath),
  }
}

function createEmptyForm(identificationTypes) {
  return {
    ...emptyForm,
    identificationType:
      getDefaultIdentificationType(identificationTypes),
  }
}

function getDefaultIdentificationType(identificationTypes) {
  return identificationTypes[0]?.identificationType
    ?? emptyForm.identificationType
}

function getIdentificationTypeByCode(
  identificationTypes,
  identificationType,
) {
  return identificationTypes.find(
    (item) => item.identificationType === identificationType,
  ) ?? null
}

function getIdentificationTypeLabel(
  identificationTypes,
  identificationType,
) {
  const selectedIdentificationType = getIdentificationTypeByCode(
    identificationTypes,
    identificationType,
  )

  return selectedIdentificationType?.name
    ?? identificationType
    ?? 'Sin tipo'
}

function getIdentificationHelperText(identificationType) {
  if (!identificationType) {
    return 'Selecciona el tipo para validar el formato.'
  }

  const lengthText = identificationType.minLength
    === identificationType.maxLength
    ? `${identificationType.minLength} caracteres`
    : `${identificationType.minLength} a ${identificationType.maxLength} caracteres`

  return identificationType.isNumericOnly
    ? `Debe contener ${lengthText}, solo números.`
    : `Debe contener ${lengthText}, solo letras y números.`
}

function sanitizeIdentificationNumber(value, identificationType) {
  const maxLength = identificationType?.maxLength ?? 20
  const normalizedValue = value.trim()

  if (identificationType?.isNumericOnly) {
    return normalizedValue
      .replace(/\D/g, '')
      .slice(0, maxLength)
  }

  return normalizedValue
    .replace(/[^a-z0-9]/gi, '')
    .toUpperCase()
    .slice(0, maxLength)
}

function validateIdentificationNumber(
  errors,
  identificationNumber,
  identificationType,
) {
  const normalizedIdentificationNumber =
    identificationNumber.trim()

  if (!normalizedIdentificationNumber) {
    errors.identificationNumber =
      'La identificación es requerida.'
    return
  }

  if (!identificationType) {
    if (normalizedIdentificationNumber.length > 20) {
      errors.identificationNumber = 'Máximo 20 caracteres.'
    }

    return
  }

  if (
    normalizedIdentificationNumber.length
      < identificationType.minLength
    || normalizedIdentificationNumber.length
      > identificationType.maxLength
  ) {
    errors.identificationNumber =
      identificationType.minLength === identificationType.maxLength
        ? `Debe tener ${identificationType.minLength} caracteres.`
        : `Debe tener entre ${identificationType.minLength} y ${identificationType.maxLength} caracteres.`
    return
  }

  if (
    !identificationType.allowsLeadingZero
    && normalizedIdentificationNumber.startsWith('0')
  ) {
    errors.identificationNumber =
      'No puede iniciar con cero.'
    return
  }

  if (
    identificationType.isNumericOnly
    && !/^\d+$/.test(normalizedIdentificationNumber)
  ) {
    errors.identificationNumber =
      'Debe contener solo números.'
    return
  }

  if (
    !identificationType.isNumericOnly
    && !/^[a-z0-9]+$/i.test(normalizedIdentificationNumber)
  ) {
    errors.identificationNumber =
      'Debe contener solo letras y números.'
  }
}

function normalizeOptionalText(value) {
  const normalizedValue = value.trim()

  return normalizedValue ? normalizedValue : null
}

function toPositiveInteger(value) {
  if (value === null || value === undefined || value === '') {
    return null
  }

  const numberValue = Number(value)

  return Number.isInteger(numberValue) && numberValue > 0
    ? numberValue
    : null
}

function toDateInputValue(value) {
  if (!value) {
    return ''
  }

  return String(value).slice(0, 10)
}

function formatCurrency(value) {
  return new Intl.NumberFormat('es-CR', {
    style: 'currency',
    currency: 'CRC',
    maximumFractionDigits: 2,
  }).format(value)
}

function formatDateOnly(value) {
  if (!value) {
    return 'Sin fecha'
  }

  const dateValue = typeof value === 'string'
    ? `${value.slice(0, 10)}T00:00:00`
    : value

  return new Intl.DateTimeFormat('es-CR', {
    dateStyle: 'medium',
  }).format(new Date(dateValue))
}

function formatDate(value) {
  if (!value) {
    return 'Sin cambios'
  }

  return new Intl.DateTimeFormat('es-CR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}
