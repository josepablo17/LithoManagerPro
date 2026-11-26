import { apiRequest } from '../../../services/apiClient.js'

const basePath = '/api/payroll'

function withQuery(path, params) {
  const query = new URLSearchParams()

  Object.entries(params).forEach(([key, value]) => {
    if (value !== null && value !== undefined && value !== '') {
      query.set(key, String(value))
    }
  })

  return query.size > 0 ? `${path}?${query.toString()}` : path
}

// Catalogs

export function getOvertimeRules({ accessToken, signal }) {
  return apiRequest(
    withQuery(`${basePath}/configuration/overtime-rules`, { isActive: true }),
    { accessToken, signal },
  )
}

export function getDisabilityTypes({ accessToken, signal }) {
  return apiRequest(
    withQuery(`${basePath}/configuration/disability-types`, { isActive: true }),
    { accessToken, signal },
  )
}

export function getWorkShiftTypes({ accessToken, signal }) {
  return apiRequest(
    withQuery(`${basePath}/configuration/work-shift-types`, { isActive: true }),
    { accessToken, signal },
  )
}

// Overtime

export function getOvertimeRecords({
  accessToken,
  approvalStatus,
  dateFrom,
  dateTo,
  signal,
}) {
  return apiRequest(
    withQuery(`${basePath}/overtime-records`, {
      approvalStatus,
      dateFrom,
      dateTo,
    }),
    { accessToken, signal },
  )
}

export function createOvertimeRecord({
  accessToken,
  employeeId,
  overtimeRuleId,
  overtimeDate,
  hours,
  notes,
}) {
  return apiRequest(`${basePath}/overtime-records`, {
    method: 'POST',
    accessToken,
    body: {
      employeeId,
      overtimeRuleId,
      overtimeDate,
      hours,
      notes: notes || null,
    },
  })
}

export function respondOvertimeRecord({
  accessToken,
  overtimeRecordId,
  isApproved,
  rejectionReason,
  expectedRowVersion,
}) {
  return apiRequest(
    `${basePath}/overtime-records/${overtimeRecordId}/response`,
    {
      method: 'PATCH',
      accessToken,
      body: {
        isApproved,
        rejectionReason: isApproved ? null : rejectionReason,
        expectedRowVersion,
      },
    },
  )
}

export function cancelOvertimeRecord({
  accessToken,
  overtimeRecordId,
  expectedRowVersion,
}) {
  return apiRequest(
    `${basePath}/overtime-records/${overtimeRecordId}/cancel`,
    {
      method: 'PATCH',
      accessToken,
      body: { expectedRowVersion },
    },
  )
}

// Disabilities

export function getEmployeeDisabilities({
  accessToken,
  disabilityStatus,
  signal,
}) {
  return apiRequest(
    withQuery(`${basePath}/employee-disabilities`, { disabilityStatus }),
    { accessToken, signal },
  )
}

export function createEmployeeDisability({
  accessToken,
  employeeId,
  disabilityTypeId,
  issuerInstitution,
  startDate,
  endDate,
  referenceNumber,
  employerPaidAmount,
  subsidyAmount,
  notes,
}) {
  return apiRequest(`${basePath}/employee-disabilities`, {
    method: 'POST',
    accessToken,
    body: {
      employeeId,
      disabilityTypeId,
      issuerInstitution,
      startDate,
      endDate,
      referenceNumber: referenceNumber || null,
      employerPaidAmount: employerPaidAmount === '' ? null : employerPaidAmount,
      subsidyAmount: subsidyAmount === '' ? null : subsidyAmount,
      notes: notes || null,
    },
  })
}

export function approveEmployeeDisability({
  accessToken,
  employeeDisabilityId,
  expectedRowVersion,
}) {
  return apiRequest(
    `${basePath}/employee-disabilities/${employeeDisabilityId}/approval`,
    {
      method: 'PATCH',
      accessToken,
      body: { expectedRowVersion },
    },
  )
}

export function cancelEmployeeDisability({
  accessToken,
  employeeDisabilityId,
  cancellationReason,
  expectedRowVersion,
}) {
  return apiRequest(
    `${basePath}/employee-disabilities/${employeeDisabilityId}/cancel`,
    {
      method: 'PATCH',
      accessToken,
      body: { cancellationReason, expectedRowVersion },
    },
  )
}

// Work schedules

export function getWorkSchedules({ accessToken, signal }) {
  return apiRequest(`${basePath}/work-schedules`, { accessToken, signal })
}

export function setWorkSchedule({
  accessToken,
  employeeId,
  workShiftTypeId,
  effectiveFromDate,
}) {
  return apiRequest(`${basePath}/work-schedules`, {
    method: 'POST',
    accessToken,
    body: {
      employeeId,
      workShiftTypeId,
      effectiveFromDate,
    },
  })
}
