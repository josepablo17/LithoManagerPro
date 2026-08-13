import {
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, vi } from 'vitest'
import { AuthContext } from '../../security/hooks/authContext.js'
import {
  getLeaveRequests,
  getLeaveRequestStatuses,
  getMyLeaveBalance,
  getMyLeaveRequests,
  respondLeaveRequest,
} from '../services/leaveManagementService.js'
import { LeaveRequestsPage } from './LeaveRequestsPage.jsx'

vi.mock('../services/leaveManagementService.js', () => ({
  cancelLeaveRequest: vi.fn(),
  createLeaveRequest: vi.fn(),
  getLeaveRequests: vi.fn(),
  getLeaveRequestStatuses: vi.fn(),
  getMyLeaveBalance: vi.fn(),
  getMyLeaveRequests: vi.fn(),
  respondLeaveRequest: vi.fn(),
}))

describe('LeaveRequestsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    getLeaveRequestStatuses.mockResolvedValue([
      {
        leaveRequestStatusCode: 'Pending',
        name: 'Pendiente',
      },
      {
        leaveRequestStatusCode: 'Approved',
        name: 'Aprobada',
      },
    ])

    getMyLeaveBalance.mockResolvedValue({
      availableDays: 9,
      pendingDays: 3,
      usedDays: 0,
    })

    getMyLeaveRequests.mockResolvedValue([
      createLeaveRequestResponse({
        leaveRequestId: 1,
        leaveRequestStatusCode: 'Pending',
        leaveRequestStatusName: 'Pendiente',
      }),
    ])

    getLeaveRequests.mockResolvedValue([
      createLeaveRequestResponse({
        leaveRequestId: 2,
        firstName: 'María',
        lastName: 'Rojas',
        leaveRequestStatusCode: 'Pending',
        leaveRequestStatusName: 'Pendiente',
      }),
    ])
  })

  it('loads my leave requests and opens the create form', async () => {
    const user = userEvent.setup()

    renderPage({
      roleCode: 'Employee',
      employeeId: 10,
    })

    expect(
      await screen.findByText('Juan Vargas'),
    ).toBeInTheDocument()

    expect(screen.getByText('9')).toBeInTheDocument()
    expect(getMyLeaveRequests).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
        statusCode: null,
      }),
    )
    expect(getLeaveRequests).not.toHaveBeenCalled()

    await user.click(
      screen.getByRole('button', { name: /solicitar/i }),
    )

    expect(
      screen.getByRole('dialog', {
        name: /nueva solicitud/i,
      }),
    ).toBeInTheDocument()
  })

  it('shows administration actions for authorized roles', async () => {
    const user = userEvent.setup()

    renderPage({
      roleCode: 'HumanResourcesAdministrator',
      employeeId: 20,
    })

    await user.click(
      await screen.findByRole('tab', {
        name: /administración/i,
      }),
    )

    expect(
      await screen.findByText('María Rojas'),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: /aprobar/i,
      }),
    ).toBeInTheDocument()
  })

  it('does not send a status filter when administration selects all statuses', async () => {
    const user = userEvent.setup()

    renderPage({
      roleCode: 'HumanResourcesAdministrator',
      employeeId: 20,
    })

    await user.click(
      await screen.findByRole('tab', {
        name: /administración/i,
      }),
    )

    await user.selectOptions(
      screen.getByLabelText(/filtrar solicitudes por estado/i),
      '',
    )

    await waitFor(() => {
      expect(getLeaveRequests).toHaveBeenLastCalledWith(
        expect.objectContaining({
          statusCode: null,
        }),
      )
    })
  })

  it('opens administration directly when an administrator has no employee profile', async () => {
    renderPage({
      roleCode: 'SuperAdministrator',
      employeeId: null,
    })

    expect(
      await screen.findByText('María Rojas'),
    ).toBeInTheDocument()

    expect(
      screen.queryByRole('button', { name: /solicitar/i }),
    ).not.toBeInTheDocument()

    expect(getMyLeaveBalance).not.toHaveBeenCalled()
    expect(getMyLeaveRequests).not.toHaveBeenCalled()
    expect(getLeaveRequests).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
        statusCode: 'Pending',
      }),
    )
  })

  it('shows a clear message when a request was already changed in another session', async () => {
    const user = userEvent.setup()

    respondLeaveRequest.mockRejectedValueOnce({
      errorCode: 'concurrency_conflict',
      message:
        'La información fue modificada. Actualiza los datos e inténtalo de nuevo.',
    })

    renderPage({
      roleCode: 'HumanResourcesAdministrator',
      employeeId: 20,
    })

    await user.click(
      await screen.findByRole('tab', {
        name: /administración/i,
      }),
    )

    await user.click(
      await screen.findByRole('button', {
        name: /aprobar/i,
      }),
    )

    const dialog = screen.getByRole('dialog', {
      name: /aprobar solicitud/i,
    })

    await user.click(
      within(dialog).getByRole('button', {
        name: /aprobar/i,
      }),
    )

    expect(
      await screen.findByText(
        /la solicitud ya fue actualizada en otra sesión/i,
      ),
    ).toBeInTheDocument()

    await waitFor(() => {
      expect(getLeaveRequests.mock.calls.length)
        .toBeGreaterThanOrEqual(2)
    })
  })
})

function renderPage({ roleCode, employeeId }) {
  return render(
    <AuthContext.Provider
      value={{
        accessToken: 'fake-access-token',
        user: {
          roleCode,
          employeeId,
        },
      }}
    >
      <LeaveRequestsPage />
    </AuthContext.Provider>,
  )
}

function createLeaveRequestResponse(overrides) {
  return {
    leaveRequestId: 1,
    employeeId: 10,
    identificationNumber: 'EMP-001',
    firstName: 'Juan',
    lastName: 'Vargas',
    departmentId: 20,
    departmentCode: 'HR',
    departmentName: 'Recursos Humanos',
    leaveTypeId: 1,
    leaveTypeCode: 'Vacation',
    leaveTypeName: 'Vacaciones',
    leaveRequestStatusCode: 'Pending',
    leaveRequestStatusName: 'Pendiente',
    startDate: '2026-09-14',
    endDate: '2026-09-16',
    requestedDays: 3,
    respondedAtUtc: null,
    respondedByUserId: null,
    respondedByEmailAddress: null,
    cancelledAtUtc: null,
    cancelledByUserId: null,
    cancelledByEmailAddress: null,
    createdAtUtc: '2026-08-13T12:00:00Z',
    createdByUserId: 1,
    createdByEmailAddress: 'admin@lithomanager.local',
    updatedAtUtc: null,
    updatedByUserId: null,
    updatedByEmailAddress: null,
    rowVersion: 'AAAAAAAAAAA=',
    ...overrides,
  }
}
