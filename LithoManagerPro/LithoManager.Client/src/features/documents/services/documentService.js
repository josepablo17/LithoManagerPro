import {
  apiDownload,
  apiRequest,
} from '../../../services/apiClient.js'

const basePath = '/api/documents'

export function getDocumentTypes({
  accessToken,
  isActive = true,
  signal,
}) {
  const query = new URLSearchParams()

  if (isActive !== null && isActive !== undefined) {
    query.set('isActive', String(isActive))
  }

  const path = query.size > 0
    ? `${basePath}/types?${query.toString()}`
    : `${basePath}/types`

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function getDocumentEmployeeOptions({
  accessToken,
  searchTerm,
  signal,
}) {
  const query = new URLSearchParams()

  if (searchTerm?.trim()) {
    query.set('searchTerm', searchTerm.trim())
  }

  const path = query.size > 0
    ? `${basePath}/employees?${query.toString()}`
    : `${basePath}/employees`

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function getEmployeeDocuments({
  accessToken,
  employeeId,
  documentTypeId,
  isActive,
  isVisibleToEmployee,
  createdFromUtc,
  createdToUtc,
  searchTerm,
  signal,
}) {
  const query = new URLSearchParams()

  if (employeeId) {
    query.set('employeeId', String(employeeId))
  }

  if (documentTypeId) {
    query.set('documentTypeId', String(documentTypeId))
  }

  if (isActive !== null && isActive !== undefined) {
    query.set('isActive', String(isActive))
  }

  if (
    isVisibleToEmployee !== null
    && isVisibleToEmployee !== undefined
  ) {
    query.set(
      'isVisibleToEmployee',
      String(isVisibleToEmployee),
    )
  }

  if (createdFromUtc) {
    query.set('createdFromUtc', toUtcStart(createdFromUtc))
  }

  if (createdToUtc) {
    query.set('createdToUtc', toUtcEnd(createdToUtc))
  }

  if (searchTerm?.trim()) {
    query.set('searchTerm', searchTerm.trim())
  }

  const path = query.size > 0
    ? `${basePath}?${query.toString()}`
    : basePath

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function ensureEmployeeRecord({
  accessToken,
  employeeId,
  signal,
}) {
  return apiRequest(
    `${basePath}/employee-records/${employeeId}/ensure`,
    {
      method: 'POST',
      accessToken,
      signal,
    },
  )
}

export function createEmployeeDocument({
  accessToken,
  employeeId,
  documentTypeId,
  title,
  description,
  issuedDate,
  expirationDate,
  isVisibleToEmployee,
  file,
  signal,
}) {
  const formData = new FormData()

  formData.set('employeeId', String(employeeId))
  formData.set('documentTypeId', String(documentTypeId))
  formData.set('title', title)
  formData.set('description', description ?? '')

  if (issuedDate) {
    formData.set('issuedDate', issuedDate)
  }

  if (expirationDate) {
    formData.set('expirationDate', expirationDate)
  }

  formData.set('isVisibleToEmployee', String(isVisibleToEmployee))
  formData.set('file', file)

  return apiRequest(basePath, {
    method: 'POST',
    accessToken,
    body: formData,
    signal,
  })
}

export function updateEmployeeDocument({
  accessToken,
  employeeDocumentId,
  documentTypeId,
  title,
  description,
  issuedDate,
  expirationDate,
  isVisibleToEmployee,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(`${basePath}/${employeeDocumentId}`, {
    method: 'PUT',
    accessToken,
    body: {
      documentTypeId,
      title,
      description,
      issuedDate: issuedDate || null,
      expirationDate: expirationDate || null,
      isVisibleToEmployee,
      expectedRowVersion,
    },
    signal,
  })
}

export function setEmployeeDocumentStatus({
  accessToken,
  employeeDocumentId,
  isActive,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(`${basePath}/${employeeDocumentId}/status`, {
    method: 'PATCH',
    accessToken,
    body: {
      isActive,
      expectedRowVersion,
    },
    signal,
  })
}

export function downloadEmployeeDocument({
  accessToken,
  employeeDocumentId,
  signal,
}) {
  return apiDownload(
    `${basePath}/${employeeDocumentId}/download`,
    {
      accessToken,
      signal,
    },
  )
}

function toUtcStart(value) {
  return `${value}T00:00:00.000Z`
}

function toUtcEnd(value) {
  return `${value}T23:59:59.999Z`
}
