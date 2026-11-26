import {
  render,
  screen,
  within,
} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, vi } from 'vitest'
import { getEmployees } from '../../humanResources/services/employeeService.js'
import { AuthContext } from '../../security/hooks/authContext.js'
import {
  cancelEmployeeDisability,
  createEmployeeDisability,
  createOvertimeRecord,
  getDisabilityTypes,
  getEmployeeDisabilities,
  getOvertimeRecords,
  getOvertimeRules,
  getWorkSchedules,
  getWorkShiftTypes,
  respondOvertimeRecord,
  setWorkSchedule,
} from '../services/payrollOperationsService.js'
import { PayrollOperationsPage } from './PayrollOperationsPage.jsx'

vi.mock('../services/payrollOperationsService.js', () => ({
  approveEmployeeDisability: vi.fn(),
  cancelEmployeeDisability: vi.fn(),
  cancelOvertimeRecord: vi.fn(),
  createEmployeeDisability: vi.fn(),
  createOvertimeRecord: vi.fn(),
  getDisabilityTypes: vi.fn(),
  getEmployeeDisabilities: vi.fn(),
  getOvertimeRecords: vi.fn(),
  getOvertimeRules: vi.fn(),
  getWorkSchedules: vi.fn(),
  getWorkShiftTypes: vi.fn(),
  respondOvertimeRecord: vi.fn(),
  setWorkSchedule: vi.fn(),
}))

vi.mock('../../humanResources/services/employeeService.js', () => ({
  getEmployees: vi.fn(),
}))

describe('PayrollOperationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    // Same order as the API: sorted by code, so Holiday comes first.
    getOvertimeRules.mockResolvedValue([
      { overtimeRuleId: 2, overtimeRuleCode: 'Holiday', name: 'Holiday Overtime', hourMultiplier: 2 },
      { overtimeRuleId: 1, overtimeRuleCode: 'Standard', name: 'Standard Overtime', hourMultiplier: 1.5 },
    ])
    getDisabilityTypes.mockResolvedValue([
      { disabilityTypeId: 1, disabilityTypeCode: 'CommonIllness', name: 'Common Illness', reducesWorkedDays: true },
    ])
    getWorkShiftTypes.mockResolvedValue([
      { workShiftTypeId: 1, workShiftTypeCode: 'Day', name: 'Day Shift', maxOrdinaryHoursPerDay: 8, maxOrdinaryHoursPerWeek: 48 },
      { workShiftTypeId: 3, workShiftTypeCode: 'Night', name: 'Night Shift', maxOrdinaryHoursPerDay: 6, maxOrdinaryHoursPerWeek: 36 },
    ])
    getEmployees.mockResolvedValue([
      { employeeId: 1, firstName: 'Ana', lastName: 'Rojas', identificationNumber: '1-1111-1111' },
      { employeeId: 2, firstName: 'Luis', lastName: 'Mora', identificationNumber: '2-2222-2222' },
    ])
    getOvertimeRecords.mockResolvedValue([createOvertime()])
    getEmployeeDisabilities.mockResolvedValue([])
    getWorkSchedules.mockResolvedValue([
      {
        employeeWorkScheduleId: 5,
        employeeId: 1,
        firstName: 'Ana',
        lastName: 'Rojas',
        identificationNumber: '1-1111-1111',
        workShiftTypeId: 1,
        workShiftTypeCode: 'Day',
        workShiftTypeName: 'Day Shift',
        weeklyOrdinaryHours: 48,
        effectiveFromDate: '2026-01-01T00:00:00',
      },
    ])
  })

  it('lists pending overtime and approves it with the row version', async () => {
    const user = userEvent.setup()

    respondOvertimeRecord.mockResolvedValue({})

    renderPage('HumanResourcesAdministrator')

    expect(await screen.findByText('Ana Rojas')).toBeInTheDocument()
    expect(screen.getByText('Ordinarias')).toBeInTheDocument()
    expect(getOvertimeRecords).toHaveBeenCalledWith(
      expect.objectContaining({ approvalStatus: 'Pending' }),
    )

    await user.click(screen.getByRole('button', { name: /aprobar/i }))

    expect(respondOvertimeRecord).toHaveBeenCalledWith(
      expect.objectContaining({
        overtimeRecordId: 10,
        isApproved: true,
        expectedRowVersion: 'AAAAAAAAAAE=',
      }),
    )
    expect(
      await screen.findByText('Horas extra de Ana Rojas aprobadas.'),
    ).toBeInTheDocument()
  })

  it('requires a reason to reject overtime', async () => {
    const user = userEvent.setup()

    respondOvertimeRecord.mockResolvedValue({})

    renderPage('SuperAdministrator')

    await user.click(await screen.findByRole('button', { name: /rechazar/i }))

    const dialog = screen.getByRole('dialog', { name: /rechazar horas extra/i })

    await user.click(within(dialog).getByRole('button', { name: /^rechazar$/i }))
    expect(within(dialog).getByText(/indica el motivo/i)).toBeInTheDocument()
    expect(respondOvertimeRecord).not.toHaveBeenCalled()

    await user.type(within(dialog).getByLabelText('Motivo'), 'No fue autorizada')
    await user.click(within(dialog).getByRole('button', { name: /^rechazar$/i }))

    expect(respondOvertimeRecord).toHaveBeenCalledWith(
      expect.objectContaining({
        isApproved: false,
        rejectionReason: 'No fue autorizada',
      }),
    )
  })

  it('registers overtime for an employee', async () => {
    const user = userEvent.setup()

    createOvertimeRecord.mockResolvedValue(createOvertime({ firstName: 'Luis', lastName: 'Mora' }))

    renderPage('HumanResourcesAdministrator')

    await user.click(await screen.findByRole('button', { name: /registrar horas extra/i }))

    const dialog = screen.getByRole('dialog', { name: /registrar horas extra/i })

    // Standard overtime is preselected even though it is not first.
    expect(within(dialog).getByLabelText('Tipo')).toHaveValue('1')

    await user.selectOptions(within(dialog).getByLabelText('Empleado'), '2')
    await user.selectOptions(within(dialog).getByLabelText('Tipo'), '2')
    await user.type(within(dialog).getByLabelText('Horas'), '3')
    await user.click(within(dialog).getByRole('button', { name: /^registrar$/i }))

    expect(createOvertimeRecord).toHaveBeenCalledWith(
      expect.objectContaining({
        employeeId: 2,
        overtimeRuleId: 2,
        hours: 3,
      }),
    )
    expect(await screen.findByText(/registradas para luis mora/i)).toBeInTheDocument()
  })

  it('registers a disability with the employer paid amount', async () => {
    const user = userEvent.setup()

    createEmployeeDisability.mockResolvedValue({
      employeeDisabilityId: 3,
      firstName: 'Ana',
      lastName: 'Rojas',
    })

    renderPage('HumanResourcesAdministrator')

    await user.click(await screen.findByRole('tab', { name: 'Incapacidades' }))
    await user.click(await screen.findByRole('button', { name: /registrar incapacidad/i }))

    const dialog = screen.getByRole('dialog', { name: /registrar incapacidad/i })

    expect(within(dialog).getByText(/se rebajan del salario/i)).toBeInTheDocument()

    await user.selectOptions(within(dialog).getByLabelText('Empleado'), '1')

    const start = within(dialog).getByLabelText('Desde')
    const end = within(dialog).getByLabelText('Hasta')

    await user.clear(start)
    await user.type(start, '2026-11-10')
    await user.clear(end)
    await user.type(end, '2026-11-12')

    expect(within(dialog).getByText('3 días')).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText('Monto pagado por el patrono'), '30000')
    await user.click(within(dialog).getByRole('button', { name: /^registrar$/i }))

    expect(createEmployeeDisability).toHaveBeenCalledWith(
      expect.objectContaining({
        employeeId: 1,
        disabilityTypeId: 1,
        issuerInstitution: 'CCSS',
        startDate: '2026-11-10',
        endDate: '2026-11-12',
        employerPaidAmount: 30000,
      }),
    )
  })

  it('requires a reason to cancel an approved disability and warns about payrolls', async () => {
    const user = userEvent.setup()

    getEmployeeDisabilities.mockResolvedValue([
      {
        employeeDisabilityId: 3,
        employeeId: 1,
        firstName: 'Ana',
        lastName: 'Rojas',
        identificationNumber: '1-1111-1111',
        disabilityTypeCode: 'CommonIllness',
        disabilityTypeName: 'Common Illness',
        issuerInstitution: 'CCSS',
        referenceNumber: null,
        startDate: '2026-11-10T00:00:00',
        endDate: '2026-11-12T00:00:00',
        employerPaidAmount: 30000,
        disabilityStatus: 'Approved',
        cancellationReason: null,
        rowVersion: 'AAAAAAAAAAI=',
      },
    ])
    cancelEmployeeDisability.mockResolvedValue({})

    renderPage('HumanResourcesAdministrator')

    await user.click(await screen.findByRole('tab', { name: 'Incapacidades' }))
    await user.click(await screen.findByRole('button', { name: /^cancelar$/i }))

    const dialog = screen.getByRole('dialog', { name: /cancelar incapacidad/i })

    expect(within(dialog).getByText(/recalcúlala/i)).toBeInTheDocument()

    await user.type(within(dialog).getByLabelText('Motivo'), 'Registrada por error')
    await user.click(within(dialog).getByRole('button', { name: /cancelar incapacidad/i }))

    expect(cancelEmployeeDisability).toHaveBeenCalledWith(
      expect.objectContaining({
        employeeDisabilityId: 3,
        cancellationReason: 'Registrada por error',
        expectedRowVersion: 'AAAAAAAAAAI=',
      }),
    )
  })

  it('highlights employees without a schedule and assigns one', async () => {
    const user = userEvent.setup()

    setWorkSchedule.mockResolvedValue({
      employeeId: 2,
      firstName: 'Luis',
      lastName: 'Mora',
      workShiftTypeCode: 'Night',
      workShiftTypeName: 'Night Shift',
      effectiveFromDate: '2026-11-26T00:00:00',
    })

    renderPage('HumanResourcesAdministrator')

    await user.click(await screen.findByRole('tab', { name: 'Horarios' }))

    expect(await screen.findByText('Sin horario')).toBeInTheDocument()
    expect(screen.getByText(/1 empleado no tiene horario/i)).toBeInTheDocument()

    await user.click(screen.getByRole('button', { name: /asignar horario de luis mora/i }))

    const dialog = screen.getByRole('dialog', { name: /asignar horario/i })

    await user.selectOptions(within(dialog).getByLabelText('Jornada'), '3')
    await user.click(within(dialog).getByRole('button', { name: /guardar horario/i }))

    expect(setWorkSchedule).toHaveBeenCalledWith(
      expect.objectContaining({ employeeId: 2, workShiftTypeId: 3 }),
    )
    expect(await screen.findByText(/jornada nocturna/i)).toBeInTheDocument()
  })

  it('is read-only for HR staff and does not list employees', async () => {
    renderPage('HumanResourcesStaff')

    expect(await screen.findByText('Ana Rojas')).toBeInTheDocument()
    expect(getEmployees).not.toHaveBeenCalled()
    expect(screen.queryByRole('button', { name: /registrar/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /aprobar/i })).not.toBeInTheDocument()
  })
})

function renderPage(roleCode) {
  return render(
    <AuthContext.Provider
      value={{
        accessToken: 'fake-access-token',
        user: { roleCode, employeeId: null },
      }}
    >
      <MemoryRouter>
        <PayrollOperationsPage />
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

function createOvertime(overrides) {
  return {
    overtimeRecordId: 10,
    employeeId: 1,
    firstName: 'Ana',
    lastName: 'Rojas',
    identificationNumber: '1-1111-1111',
    overtimeRuleCode: 'Standard',
    overtimeRuleName: 'Standard Overtime',
    hourMultiplier: 1.5,
    overtimeDate: '2026-11-05T00:00:00',
    hours: 4,
    approvalStatus: 'Pending',
    rejectionReason: null,
    rowVersion: 'AAAAAAAAAAE=',
    ...overrides,
  }
}
