import { apiRequest } from '../../../services/apiClient.js'

const requestsPath = '/api/leave-management/requests'
const balancesPath = '/api/leave-management/balances'
const catalogPath = '/api/leave-management/catalog'

export function getMyLeaveBalance({
  accessToken,
  leaveTypeCode,
  signal,
}) {
  const query = new URLSearchParams()

  if (leaveTypeCode?.trim()) {
    query.set('leaveTypeCode', leaveTypeCode.trim())
  }

  const path = query.size > 0
    ? `${balancesPath}/me?${query.toString()}`
    : `${balancesPath}/me`

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function getLeaveRequestStatuses({
  accessToken,
  isActive = true,
  signal,
}) {
  const query = new URLSearchParams()

  if (isActive !== null && isActive !== undefined) {
    query.set('isActive', String(isActive))
  }

  const path = query.size > 0
    ? `${catalogPath}/request-statuses?${query.toString()}`
    : `${catalogPath}/request-statuses`

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function getMyLeaveRequests({
  accessToken,
  statusCode,
  startDateFrom,
  startDateTo,
  signal,
}) {
  const query = createLeaveRequestQuery({
    statusCode,
    startDateFrom,
    startDateTo,
  })

  const path = query.size > 0
    ? `${requestsPath}/my?${query.toString()}`
    : `${requestsPath}/my`

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function getLeaveRequests({
  accessToken,
  statusCode,
  employeeId,
  departmentId,
  startDateFrom,
  startDateTo,
  searchTerm,
  signal,
}) {
  const query = createLeaveRequestQuery({
    statusCode,
    startDateFrom,
    startDateTo,
  })

  if (employeeId) {
    query.set('employeeId', String(employeeId))
  }

  if (departmentId) {
    query.set('departmentId', String(departmentId))
  }

  if (searchTerm?.trim()) {
    query.set('searchTerm', searchTerm.trim())
  }

  const path = query.size > 0
    ? `${requestsPath}?${query.toString()}`
    : requestsPath

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function createLeaveRequest({
  accessToken,
  startDate,
  endDate,
  leaveTypeCode,
  signal,
}) {
  return apiRequest(requestsPath, {
    method: 'POST',
    accessToken,
    body: {
      startDate,
      endDate,
      leaveTypeCode,
    },
    signal,
  })
}

export function cancelLeaveRequest({
  accessToken,
  leaveRequestId,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(
    `${requestsPath}/${leaveRequestId}/cancel`,
    {
      method: 'PATCH',
      accessToken,
      body: {
        expectedRowVersion,
      },
      signal,
    },
  )
}

export function respondLeaveRequest({
  accessToken,
  leaveRequestId,
  isApproved,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(
    `${requestsPath}/${leaveRequestId}/response`,
    {
      method: 'PATCH',
      accessToken,
      body: {
        isApproved,
        expectedRowVersion,
      },
      signal,
    },
  )
}

function createLeaveRequestQuery({
  statusCode,
  startDateFrom,
  startDateTo,
}) {
  const query = new URLSearchParams()

  if (statusCode?.trim()) {
    query.set('statusCode', statusCode.trim())
  }

  if (startDateFrom) {
    query.set('startDateFrom', startDateFrom)
  }

  if (startDateTo) {
    query.set('startDateTo', startDateTo)
  }

  return query
}
