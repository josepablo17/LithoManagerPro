import { apiRequest } from '../../../services/apiClient.js'

const runsPath = '/api/payroll/runs'

export function getPayrollRuns({
  accessToken,
  periodYear,
  signal,
}) {
  const query = new URLSearchParams()

  if (periodYear) {
    query.set('periodYear', String(periodYear))
  }

  const path = query.size > 0
    ? `${runsPath}?${query.toString()}`
    : runsPath

  return apiRequest(path, {
    accessToken,
    signal,
  })
}

export function getPayrollRun({
  accessToken,
  payrollRunId,
  signal,
}) {
  return apiRequest(`${runsPath}/${payrollRunId}`, {
    accessToken,
    signal,
  })
}

export function getPayrollRunEmployeeLines({
  accessToken,
  payrollRunId,
  employeeId,
  signal,
}) {
  return apiRequest(
    `${runsPath}/${payrollRunId}/employees/${employeeId}/lines`,
    {
      accessToken,
      signal,
    },
  )
}

export function createPayrollRun({
  accessToken,
  runType,
  periodYear,
  periodMonth,
  paymentDate,
  signal,
}) {
  return apiRequest(runsPath, {
    method: 'POST',
    accessToken,
    body: {
      runType,
      periodYear,
      periodMonth: runType === 'Regular' ? periodMonth : null,
      paymentDate: paymentDate || null,
    },
    signal,
  })
}

export function calculatePayrollRun({
  accessToken,
  payrollRunId,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(`${runsPath}/${payrollRunId}/calculate`, {
    method: 'POST',
    accessToken,
    body: { expectedRowVersion },
    signal,
  })
}

export function closePayrollRun({
  accessToken,
  payrollRunId,
  expectedRowVersion,
  signal,
}) {
  return apiRequest(`${runsPath}/${payrollRunId}/close`, {
    method: 'PATCH',
    accessToken,
    body: { expectedRowVersion },
    signal,
  })
}
