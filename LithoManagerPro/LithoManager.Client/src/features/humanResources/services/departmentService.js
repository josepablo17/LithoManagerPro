import { apiRequest } from '../../../services/apiClient.js'

const basePath = '/api/human-resources/departments'

export function getDepartments({
  accessToken,
  searchTerm,
  isActive,
  signal,
}) {
  const query = new URLSearchParams()
  const normalizedSearchTerm = searchTerm?.trim()

  if (normalizedSearchTerm) {
    query.set('searchTerm', normalizedSearchTerm)
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

export function createDepartment({
  accessToken,
  departmentCode,
  name,
  description,
  signal,
}) {
  return apiRequest(basePath, {
    method: 'POST',
    accessToken,
    body: {
      departmentCode,
      name,
      description,
    },
    signal,
  })
}

export function updateDepartment({
  accessToken,
  departmentId,
  departmentCode,
  name,
  description,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(`${basePath}/${departmentId}`, {
    method: 'PUT',
    accessToken,
    body: {
      departmentCode,
      name,
      description,
      expectedRowVersion,
    },
    signal,
  })
}

export function setDepartmentStatus({
  accessToken,
  departmentId,
  isActive,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(`${basePath}/${departmentId}/status`, {
    method: 'PATCH',
    accessToken,
    body: {
      isActive,
      expectedRowVersion,
    },
    signal,
  })
}
