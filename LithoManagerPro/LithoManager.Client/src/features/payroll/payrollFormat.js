// Roles allowed to create, calculate and close payroll runs
// (PayrollAdministrationMutation policy).
export const payrollMutationRoles = new Set([
  'SuperAdministrator',
  'HumanResourcesAdministrator',
])

const currencyFormatter = new Intl.NumberFormat('es-CR', {
  style: 'currency',
  currency: 'CRC',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

const numberFormatter = new Intl.NumberFormat('es-CR', {
  maximumFractionDigits: 2,
})

const percentFormatter = new Intl.NumberFormat('es-CR', {
  style: 'percent',
  maximumFractionDigits: 2,
})

export const monthNames = [
  'enero',
  'febrero',
  'marzo',
  'abril',
  'mayo',
  'junio',
  'julio',
  'agosto',
  'septiembre',
  'octubre',
  'noviembre',
  'diciembre',
]

export function formatCurrency(value) {
  if (value === null || value === undefined) {
    return '-'
  }

  return currencyFormatter.format(Number(value))
}

export function formatNumber(value) {
  if (value === null || value === undefined) {
    return '-'
  }

  return numberFormatter.format(Number(value))
}

export function formatPercent(value) {
  if (value === null || value === undefined) {
    return '-'
  }

  return percentFormatter.format(Number(value))
}

export function formatDate(value) {
  if (!value) {
    return '-'
  }

  return new Intl.DateTimeFormat('es-CR', {
    dateStyle: 'medium',
    timeZone: 'UTC',
  }).format(new Date(value))
}

export function formatPeriod(run) {
  if (!run) {
    return ''
  }

  if (run.runType === 'Aguinaldo') {
    return `Aguinaldo ${run.periodYear}`
  }

  const month = monthNames[run.periodMonth - 1] ?? ''

  return `${capitalize(month)} ${run.periodYear}`
}

export function getStatusLabel(status) {
  const labels = {
    Open: 'Abierta',
    Calculated: 'Calculada',
    Closed: 'Cerrada',
  }

  return labels[status] ?? status
}

export function getStatusVariant(status) {
  const variants = {
    Open: 'neutral',
    Calculated: 'warning',
    Closed: 'success',
  }

  return variants[status] ?? 'neutral'
}

// The payroll catalogs are seeded in English; these are the labels shown
// in the screens, falling back to the catalog name for unknown codes.
const overtimeRuleLabels = {
  Standard: 'Ordinarias',
  Holiday: 'En feriado',
}

const disabilityTypeLabels = {
  CommonIllness: 'Enfermedad común',
  OccupationalRisk: 'Riesgo del trabajo',
  Maternity: 'Maternidad',
}

const workShiftLabels = {
  Day: 'Diurna',
  Mixed: 'Mixta',
  Night: 'Nocturna',
}

export const issuerInstitutions = [
  { value: 'CCSS', label: 'CCSS' },
  { value: 'INS', label: 'INS' },
  { value: 'Employer', label: 'Patrono' },
  { value: 'Other', label: 'Otra' },
]

export function getOvertimeRuleLabel(code, fallback) {
  return overtimeRuleLabels[code] ?? fallback ?? code
}

export function getDisabilityTypeLabel(code, fallback) {
  return disabilityTypeLabels[code] ?? fallback ?? code
}

export function getWorkShiftLabel(code, fallback) {
  return workShiftLabels[code] ?? fallback ?? code
}

export function getIssuerInstitutionLabel(value) {
  return issuerInstitutions.find((item) => item.value === value)?.label ?? value
}

export function getApprovalStatusLabel(status) {
  const labels = {
    Pending: 'Pendiente',
    Approved: 'Aprobada',
    Rejected: 'Rechazada',
    Cancelled: 'Cancelada',
  }

  return labels[status] ?? status
}

export function getApprovalStatusVariant(status) {
  const variants = {
    Pending: 'warning',
    Approved: 'success',
    Rejected: 'danger',
    Cancelled: 'neutral',
  }

  return variants[status] ?? 'neutral'
}

/** Inclusive number of calendar days between two ISO dates. */
export function countDays(startDate, endDate) {
  if (!startDate || !endDate) {
    return 0
  }

  const start = Date.parse(`${String(startDate).slice(0, 10)}T00:00:00Z`)
  const end = Date.parse(`${String(endDate).slice(0, 10)}T00:00:00Z`)

  return Math.round((end - start) / 86_400_000) + 1
}

export function todayIsoDate() {
  const now = new Date()
  const offset = now.getTimezoneOffset() * 60_000

  return new Date(now.getTime() - offset).toISOString().slice(0, 10)
}

/** User-facing message for errors from the payroll operations API. */
export function getOperationErrorMessage(error) {
  const messages = {
    invalid_state:
      'El registro ya cambió de estado. Actualizamos la lista para mostrar el estado actual.',
    concurrency_conflict:
      'El registro fue modificado en otra sesión. Actualizamos la lista; inténtalo de nuevo.',
    date_overlap:
      'El empleado ya tiene un registro activo en esas fechas.',
    duplicate_record:
      'Ya existe un registro con ese número de referencia.',
    employee_inactive: 'El empleado no está activo.',
    configuration_not_found:
      'El tipo seleccionado no está vigente para esa fecha.',
  }

  return messages[error?.errorCode]
    ?? error?.message
    ?? 'No pudimos completar la operación. Inténtalo de nuevo.'
}

/** Errors after which the list should be reloaded. */
export function isStaleRecordError(error) {
  return error?.errorCode === 'invalid_state'
    || error?.errorCode === 'concurrency_conflict'
}

export function getRunTypeLabel(runType) {
  return runType === 'Aguinaldo' ? 'Aguinaldo' : 'Mensual'
}

function capitalize(text) {
  return text ? text[0].toUpperCase() + text.slice(1) : text
}

export function payslipPath(payrollRunId, employeeId) {
  return `/payroll/runs/${payrollRunId}/employees/${employeeId}/payslip`
}

/** Short detail for a payroll line: days, hours, rate x base... */
export function describeLine(line) {
  const kind = line.conceptKind

  if (kind === 'Deduction' || kind === 'EmployerContribution') {
    if (line.rate !== null && line.baseAmount !== null) {
      return `${formatPercent(line.rate)} de ${formatCurrency(line.baseAmount)}`
    }

    if (line.baseAmount !== null) {
      return `Base gravable ${formatCurrency(line.baseAmount)}`
    }

    return ''
  }

  // Disability deductions are base salary lines with the disability type
  // as reference. Their quantity is in 30-day commercial days (2.9 for
  // 3 days of December), which is confusing; the description already has
  // the dates.
  if (line.payrollConceptCode === 'BaseSalary' && line.referenceCode) {
    return ''
  }

  if (line.payrollConceptCode === 'Overtime') {
    return `${formatNumber(line.quantity)} h`
  }

  if (line.payrollConceptCode === 'Aguinaldo') {
    return `Base ${formatCurrency(line.baseAmount)}`
  }

  if (line.quantity !== null) {
    return `${formatNumber(line.quantity)} días`
  }

  return ''
}
