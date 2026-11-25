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

export function getRunTypeLabel(runType) {
  return runType === 'Aguinaldo' ? 'Aguinaldo' : 'Mensual'
}

function capitalize(text) {
  return text ? text[0].toUpperCase() + text.slice(1) : text
}
