import { CheckCircle2, Plus, XCircle } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Badge } from '../../../components/ui/Badge.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import pageStyles from '../../../app/Page.module.css'
import {
  countDays,
  formatCurrency,
  formatDate,
  getApprovalStatusLabel,
  getApprovalStatusVariant,
  getDisabilityTypeLabel,
  getIssuerInstitutionLabel,
  getOperationErrorMessage,
  isStaleRecordError,
  issuerInstitutions,
  todayIsoDate,
} from '../payrollFormat.js'
import {
  approveEmployeeDisability,
  cancelEmployeeDisability,
  createEmployeeDisability,
  getEmployeeDisabilities,
} from '../services/payrollOperationsService.js'
import styles from '../pages/Payroll.module.css'
import { FormActions, FormField, PayrollModal } from './PayrollModal.jsx'

const statusFilters = [
  { value: 'Pending', label: 'Pendientes' },
  { value: 'Approved', label: 'Aprobadas' },
  { value: 'Cancelled', label: 'Canceladas' },
  { value: '', label: 'Todas' },
]

export function DisabilitiesSection({
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
  const [cancelTarget, setCancelTarget] = useState(null)
  const [busyRecordId, setBusyRecordId] = useState(null)

  const loadRecords = useCallback(async ({ signal } = {}) => {
    setIsLoading(true)
    setLoadError('')

    try {
      setRecords(await getEmployeeDisabilities({
        accessToken,
        disabilityStatus: statusFilter,
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
    setBusyRecordId(record.employeeDisabilityId)
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

  return (
    <>
      <div className={styles.sectionHeader}>
        <select
          className={pageStyles.select}
          aria-label="Filtrar incapacidades por estado"
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
            Registrar incapacidad
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
              <th>Tipo</th>
              <th>Período</th>
              <th className={styles.amountHeader}>Pagado por el patrono</th>
              <th>Estado</th>
              <th aria-label="Acciones" />
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan="6" className={styles.loadingCell}>Cargando incapacidades</td>
              </tr>
            ) : null}

            {!isLoading && records.length === 0 ? (
              <tr>
                <td colSpan="6" className={styles.emptyCell}>
                  No hay incapacidades con ese estado
                </td>
              </tr>
            ) : null}

            {!isLoading
              ? records.map((record) => (
                <tr key={record.employeeDisabilityId}>
                  <td>
                    <div className={styles.employeeCell}>
                      <strong>{record.firstName} {record.lastName}</strong>
                      <span>{record.identificationNumber}</span>
                    </div>
                  </td>
                  <td>
                    <div className={styles.employeeCell}>
                      <span>{getDisabilityTypeLabel(record.disabilityTypeCode, record.disabilityTypeName)}</span>
                      <span>
                        {getIssuerInstitutionLabel(record.issuerInstitution)}
                        {record.referenceNumber ? ` · ${record.referenceNumber}` : ''}
                      </span>
                    </div>
                  </td>
                  <td>
                    <div className={styles.employeeCell}>
                      <span>{formatDate(record.startDate)} al {formatDate(record.endDate)}</span>
                      <span>{countDays(record.startDate, record.endDate)} días</span>
                    </div>
                  </td>
                  <td className={styles.amount}>
                    {record.employerPaidAmount === null
                      ? '-'
                      : formatCurrency(record.employerPaidAmount)}
                  </td>
                  <td>
                    <Badge variant={getApprovalStatusVariant(record.disabilityStatus)}>
                      {getApprovalStatusLabel(record.disabilityStatus)}
                    </Badge>
                    {record.cancellationReason ? (
                      <div className={styles.meta}>{record.cancellationReason}</div>
                    ) : null}
                  </td>
                  <td>
                    {canManage && record.disabilityStatus !== 'Cancelled' ? (
                      <div className={styles.rowActions}>
                        <Button
                          variant="secondary"
                          size="small"
                          disabled={busyRecordId !== null}
                          onClick={() => {
                            setNotice('')
                            setCancelTarget(record)
                          }}
                        >
                          <XCircle size={16} aria-hidden="true" />
                          Cancelar
                        </Button>
                        {record.disabilityStatus === 'Pending' ? (
                          <Button
                            size="small"
                            isLoading={busyRecordId === record.employeeDisabilityId}
                            loadingText="..."
                            disabled={busyRecordId !== null}
                            onClick={() => runAction(
                              record,
                              () => approveEmployeeDisability({
                                accessToken,
                                employeeDisabilityId: record.employeeDisabilityId,
                                expectedRowVersion: record.rowVersion,
                              }),
                              `Incapacidad de ${record.firstName} ${record.lastName} aprobada.`,
                            )}
                          >
                            <CheckCircle2 size={16} aria-hidden="true" />
                            Aprobar
                          </Button>
                        ) : null}
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
        <CreateDisabilityModal
          accessToken={accessToken}
          employees={employees}
          disabilityTypes={catalogs.disabilityTypes}
          onClose={() => setCreateOpen(false)}
          onCreated={async (record) => {
            setCreateOpen(false)
            setNotice(
              `Incapacidad registrada para ${record.firstName} ${record.lastName}. Queda pendiente de aprobación.`,
            )
            setStatusFilter('Pending')
            await loadRecords()
          }}
        />
      ) : null}

      {cancelTarget ? (
        <CancelDisabilityModal
          record={cancelTarget}
          onClose={() => setCancelTarget(null)}
          onConfirm={async (reason) => {
            const record = cancelTarget
            setCancelTarget(null)
            await runAction(
              record,
              () => cancelEmployeeDisability({
                accessToken,
                employeeDisabilityId: record.employeeDisabilityId,
                cancellationReason: reason,
                expectedRowVersion: record.rowVersion,
              }),
              'Incapacidad cancelada.',
            )
          }}
        />
      ) : null}
    </>
  )
}

function CreateDisabilityModal({
  accessToken,
  employees,
  disabilityTypes,
  onClose,
  onCreated,
}) {
  const today = todayIsoDate()
  const [values, setValues] = useState({
    employeeId: '',
    disabilityTypeId: disabilityTypes[0]?.disabilityTypeId ?? '',
    issuerInstitution: 'CCSS',
    startDate: today,
    endDate: today,
    referenceNumber: '',
    employerPaidAmount: '',
    notes: '',
  })
  const [errors, setErrors] = useState({})
  const [submitError, setSubmitError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  const selectedType = disabilityTypes.find(
    (type) => String(type.disabilityTypeId) === String(values.disabilityTypeId),
  )
  const days = countDays(values.startDate, values.endDate)

  function handleChange(event) {
    const { name, value } = event.target

    setValues((current) => ({ ...current, [name]: value }))
    setErrors((current) => ({ ...current, [name]: undefined }))
  }

  async function handleSubmit(event) {
    event.preventDefault()

    const validation = {}

    if (!values.employeeId) {
      validation.employeeId = 'Selecciona un empleado.'
    }

    if (!values.startDate || !values.endDate) {
      validation.endDate = 'Las fechas son requeridas.'
    } else if (values.endDate < values.startDate) {
      validation.endDate = 'La fecha final no puede ser menor a la inicial.'
    }

    if (values.employerPaidAmount !== '' && !(Number(values.employerPaidAmount) >= 0)) {
      validation.employerPaidAmount = 'Ingresa un monto válido.'
    }

    if (Object.keys(validation).length > 0) {
      setErrors(validation)
      return
    }

    setIsSaving(true)
    setSubmitError('')

    try {
      const record = await createEmployeeDisability({
        accessToken,
        employeeId: Number(values.employeeId),
        disabilityTypeId: Number(values.disabilityTypeId),
        issuerInstitution: values.issuerInstitution,
        startDate: values.startDate,
        endDate: values.endDate,
        referenceNumber: values.referenceNumber.trim(),
        employerPaidAmount: values.employerPaidAmount === ''
          ? ''
          : Number(values.employerPaidAmount),
        subsidyAmount: '',
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
      title="Registrar incapacidad"
      subtitle="Queda pendiente hasta que un administrador la apruebe."
      isBusy={isSaving}
      onClose={onClose}
    >
      <form className={styles.form} onSubmit={handleSubmit}>
        {submitError ? <Alert variant="error">{submitError}</Alert> : null}

        <FormField id="disability-employee" label="Empleado" error={errors.employeeId}>
          <select
            id="disability-employee"
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
          <FormField
            id="disability-type"
            label="Tipo"
            hint={selectedType?.reducesWorkedDays
              ? 'Se rebajan del salario los días de incapacidad.'
              : undefined}
          >
            <select
              id="disability-type"
              name="disabilityTypeId"
              className={styles.input}
              value={values.disabilityTypeId}
              onChange={handleChange}
            >
              {disabilityTypes.map((type) => (
                <option key={type.disabilityTypeId} value={type.disabilityTypeId}>
                  {getDisabilityTypeLabel(type.disabilityTypeCode, type.name)}
                </option>
              ))}
            </select>
          </FormField>

          <FormField id="disability-issuer" label="Emitida por">
            <select
              id="disability-issuer"
              name="issuerInstitution"
              className={styles.input}
              value={values.issuerInstitution}
              onChange={handleChange}
            >
              {issuerInstitutions.map((issuer) => (
                <option key={issuer.value} value={issuer.value}>
                  {issuer.label}
                </option>
              ))}
            </select>
          </FormField>

          <FormField id="disability-start" label="Desde">
            <input
              id="disability-start"
              name="startDate"
              type="date"
              className={styles.input}
              value={values.startDate}
              onChange={handleChange}
            />
          </FormField>

          <FormField
            id="disability-end"
            label="Hasta"
            error={errors.endDate}
            hint={days > 0 ? `${days} ${days === 1 ? 'día' : 'días'}` : undefined}
          >
            <input
              id="disability-end"
              name="endDate"
              type="date"
              className={styles.input}
              value={values.endDate}
              onChange={handleChange}
            />
          </FormField>

          <FormField id="disability-reference" label="Número de boleta (opcional)">
            <input
              id="disability-reference"
              name="referenceNumber"
              maxLength={100}
              className={styles.input}
              value={values.referenceNumber}
              onChange={handleChange}
            />
          </FormField>

          <FormField
            id="disability-employer-amount"
            label="Monto pagado por el patrono"
            error={errors.employerPaidAmount}
            hint="Opcional. Se paga como subsidio, sin cargas ni renta."
          >
            <input
              id="disability-employer-amount"
              name="employerPaidAmount"
              type="number"
              min="0"
              step="0.01"
              className={styles.input}
              value={values.employerPaidAmount}
              onChange={handleChange}
            />
          </FormField>
        </div>

        <FormField id="disability-notes" label="Notas (opcional)">
          <textarea
            id="disability-notes"
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

function CancelDisabilityModal({ record, onClose, onConfirm }) {
  const [reason, setReason] = useState('')
  const [error, setError] = useState('')
  const [isSaving, setIsSaving] = useState(false)

  async function handleSubmit(event) {
    event.preventDefault()

    if (!reason.trim()) {
      setError('Indica el motivo de la cancelación.')
      return
    }

    setIsSaving(true)
    await onConfirm(reason.trim())
  }

  return (
    <PayrollModal
      title="Cancelar incapacidad"
      subtitle={`${record.firstName} ${record.lastName} · ${formatDate(record.startDate)} al ${formatDate(record.endDate)}`}
      isBusy={isSaving}
      onClose={onClose}
    >
      <form className={styles.form} onSubmit={handleSubmit}>
        {record.disabilityStatus === 'Approved' ? (
          <Alert variant="warning">
            Está aprobada: si ya entró en una planilla calculada, recalcúlala
            para quitar el rebajo.
          </Alert>
        ) : null}
        <FormField id="disability-cancel-reason" label="Motivo" error={error}>
          <textarea
            id="disability-cancel-reason"
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
          submitText="Cancelar incapacidad"
          submitVariant="danger"
          onCancel={onClose}
        />
      </form>
    </PayrollModal>
  )
}
