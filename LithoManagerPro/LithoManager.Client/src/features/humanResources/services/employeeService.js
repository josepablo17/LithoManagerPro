import { apiRequest } from '../../../services/apiClient.js'

const basePath = '/api/human-resources/employees'

export function getEmployees({
  accessToken,
  searchTerm,
  departmentId,
  isActive,
  signal,
}) {
  const query = new URLSearchParams()
  const normalizedSearchTerm = searchTerm?.trim()

  if (normalizedSearchTerm) {
    query.set('searchTerm', normalizedSearchTerm)
  }

  if (departmentId) {
    query.set('departmentId', String(departmentId))
  }

  if (isActive !== null && isActive !== undefined) {
    query.set('isActive', String(isActive))
  }

  const path = query.size > 0
    ? `${basePath}?${query.toString()}`
    : basePath

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function getEmployeeIdentificationTypes({
  accessToken,
  signal,
}) {
  return apiRequest(`${basePath}/identification-types`, {
    accessToken,
    signal,
  })
}

export function getAssignableEmployeeUsers({
  accessToken,
  employeeId,
  signal,
}) {
  const query = new URLSearchParams()

  if (employeeId) {
    query.set('employeeId', String(employeeId))
  }

  const path = query.size > 0
    ? `${basePath}/assignable-users?${query.toString()}`
    : `${basePath}/assignable-users`

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function createEmployee({
  accessToken,
  employee,
  signal,
}) {
  return apiRequest(basePath, {
    method: 'POST',
    accessToken,
    body: employee,
    signal,
  })
}

export function updateEmployee({
  accessToken,
  employeeId,
  employee,
  signal,
}) {
  return apiRequest(`${basePath}/${employeeId}`, {
    method: 'PUT',
    accessToken,
    body: employee,
    signal,
  })
}

export function setEmployeeStatus({
  accessToken,
  employeeId,
  isActive,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(`${basePath}/${employeeId}/status`, {
    method: 'PATCH',
    accessToken,
    body: {
      isActive,
      expectedRowVersion,
    },
    signal,
  })
}

export function getEmployeeSalaryHistory({
  accessToken,
  employeeId,
  effectiveFromDate,
  effectiveToDate,
  signal,
}) {
  const query = new URLSearchParams()

  if (effectiveFromDate) {
    query.set('effectiveFromDate', effectiveFromDate)
  }

  if (effectiveToDate) {
    query.set('effectiveToDate', effectiveToDate)
  }

  const path = query.size > 0
    ? `${basePath}/${employeeId}/salary-history?${query.toString()}`
    : `${basePath}/${employeeId}/salary-history`

  return apiRequest(path, {
    accessToken,
    signal,
  })
}
