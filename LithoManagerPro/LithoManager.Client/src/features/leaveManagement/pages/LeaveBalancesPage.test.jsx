import {
  render,
  screen,
  within,
} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, vi } from 'vitest'
import { getEmployees } from '../../humanResources/services/employeeService.js'
import { AuthContext } from '../../security/hooks/authContext.js'
import {
  adjustEmployeeLeaveBalance,
  getEmployeeLeaveBalance,
  getMyLeaveBalance,
} from '../services/leaveManagementService.js'
import { LeaveBalancesPage } from './LeaveBalancesPage.jsx'

vi.mock('../services/leaveManagementService.js', () => ({
  adjustEmployeeLeaveBalance: vi.fn(),
  getEmployeeLeaveBalance: vi.fn(),
  getMyLeaveBalance: vi.fn(),
}))

vi.mock('../../humanResources/services/employeeService.js', () => ({
  getEmployees: vi.fn(),
}))

describe('LeaveBalancesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    getMyLeaveBalance.mockResolvedValue(
      createBalance({
        accruedDays: 12,
        adjustedDays: 1.5,
        pendingDays: 3,
        usedDays: 2,
        availableDays: 8.5,
      }),
    )

    getEmployees.mockResolvedValue([
      {
        employeeId: 30,
        firstName: 'María',
        lastName: 'Rojas',
        departmentName: 'Producción',
        identificationNumber: '1-1111-1111',
      },
    ])

    getEmployeeLeaveBalance.mockResolvedValue(
      createBalance({
        employeeId: 30,
        availableDays: 4,
      }),
    )
  })

  it('shows the breakdown of my balance for an employee', async () => {
    renderPage({ roleCode: 'Employee', employeeId: 10 })

    const list = await screen.findByLabelText('Desglose del saldo')

    expect(within(list).getByText('Acumulados')).toBeInTheDocument()
    expect(within(list).getByText('12')).toBeInTheDocument()
    expect(within(list).getByText('+1,5')).toBeInTheDocument()
    expect(within(list).getByText('−3')).toBeInTheDocument()
    expect(within(list).getByText('−2')).toBeInTheDocument()
    expect(within(list).getByText('8,5')).toBeInTheDocument()

    expect(
      screen.getByText(/12 días por cada 2 semanas de trabajo continuo/i),
    ).toBeInTheDocument()
    expect(screen.queryByRole('tab')).not.toBeInTheDocument()
    expect(getEmployees).not.toHaveBeenCalled()
  })

  it('warns users that are not linked to an employee', () => {
    renderPage({ roleCode: 'HumanResourcesStaff', employeeId: null })

    expect(
      screen.getByText(/no está vinculado a un empleado/i),
    ).toBeInTheDocument()
    expect(getMyLeaveBalance).not.toHaveBeenCalled()
    expect(getEmployees).not.toHaveBeenCalled()
  })

  it('lets administrators adjust an employee balance', async () => {
    const user = userEvent.setup()

    // The adjustment endpoint returns only counters, without policy data.
    adjustEmployeeLeaveBalance.mockResolvedValue({
      employeeLeaveBalanceId: 1,
      employeeId: 30,
      leaveTypeCode: 'Vacation',
      entitlementDays: 0,
      entitlementWeeks: 0,
      accruedDays: 10,
      adjustedDays: 2,
      pendingDays: 0,
      usedDays: 6,
      availableDays: 6,
    })

    getEmployeeLeaveBalance
      .mockResolvedValueOnce(
        createBalance({ employeeId: 30, availableDays: 4 }),
      )
      .mockResolvedValueOnce(
        createBalance({
          employeeId: 30,
          adjustedDays: 2,
          availableDays: 6,
        }),
      )

    renderPage({
      roleCode: 'HumanResourcesAdministrator',
      employeeId: null,
    })

    await user.click(
      await screen.findByRole('button', {
        name: /ver saldo de maría rojas/i,
      }),
    )

    expect(getEmployeeLeaveBalance).toHaveBeenCalledWith(
      expect.objectContaining({ employeeId: 30 }),
    )

    await user.click(
      await screen.findByRole('button', { name: /ajustar saldo/i }),
    )

    const dialog = screen.getByRole('dialog', {
      name: /ajustar saldo/i,
    })

    await user.type(
      within(dialog).getByLabelText(/días a sumar o restar/i),
      '2',
    )

    expect(
      within(dialog).getByText(/después del ajuste/i),
    ).toHaveTextContent('6')

    await user.click(
      within(dialog).getByRole('button', { name: /aplicar ajuste/i }),
    )

    expect(adjustEmployeeLeaveBalance).toHaveBeenCalledWith(
      expect.objectContaining({
        employeeId: 30,
        leaveTypeCode: 'Vacation',
        adjustedDaysDelta: 2,
      }),
    )

    expect(
      await screen.findByText(/saldo ajustado en \+2 días/i),
    ).toBeInTheDocument()
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()

    // Policy data comes from the reloaded balance, not the slim response.
    expect(getEmployeeLeaveBalance).toHaveBeenCalledTimes(2)
    expect(
      screen.getByText(/12 días por cada 2 semanas/i),
    ).toBeInTheDocument()
  })

  it('blocks adjustments that would leave a negative balance', async () => {
    const user = userEvent.setup()

    renderPage({
      roleCode: 'SuperAdministrator',
      employeeId: null,
    })

    await user.click(
      await screen.findByRole('button', {
        name: /ver saldo de maría rojas/i,
      }),
    )

    await user.click(
      await screen.findByRole('button', { name: /ajustar saldo/i }),
    )

    const dialog = screen.getByRole('dialog')

    await user.type(
      within(dialog).getByLabelText(/días a sumar o restar/i),
      '-5',
    )

    await user.click(
      within(dialog).getByRole('button', { name: /aplicar ajuste/i }),
    )

    expect(
      within(dialog).getByText(/no puede quedar negativo/i),
    ).toBeInTheDocument()
    expect(adjustEmployeeLeaveBalance).not.toHaveBeenCalled()
  })

  it('explains when an employee has no balance yet', async () => {
    const user = userEvent.setup()

    getEmployeeLeaveBalance.mockRejectedValue(
      Object.assign(new Error('Not found'), {
        errorCode: 'leave_balance_not_found',
      }),
    )

    renderPage({
      roleCode: 'HumanResourcesAdministrator',
      employeeId: null,
    })

    await user.click(
      await screen.findByRole('button', {
        name: /ver saldo de maría rojas/i,
      }),
    )

    expect(
      await screen.findByText(/todavía no tiene un saldo/i),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: /ajustar saldo/i }),
    ).toBeDisabled()
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
      <LeaveBalancesPage />
    </AuthContext.Provider>,
  )
}

function createBalance(overrides) {
  return {
    employeeLeaveBalanceId: 1,
    employeeId: 10,
    leaveTypeId: 1,
    leaveTypeCode: 'Vacation',
    leaveTypeName: 'Vacaciones',
    affectsVacationBalance: true,
    leavePolicyId: 1,
    leavePolicyCode: 'CR-STANDARD',
    leavePolicyName: 'Código de Trabajo',
    entitlementDays: 12,
    entitlementWeeks: 2,
    // Weeks of continuous work required to earn entitlementDays.
    usesBusinessDays: true,
    accruedDays: 10,
    adjustedDays: 0,
    pendingDays: 0,
    usedDays: 6,
    availableDays: 4,
    createdAtUtc: '2026-08-13T12:00:00Z',
    updatedAtUtc: null,
    rowVersion: 'AAAAAAAAAAA=',
    ...overrides,
  }
}
