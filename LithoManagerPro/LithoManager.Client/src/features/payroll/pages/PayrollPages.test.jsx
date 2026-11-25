import {
  render,
  screen,
  within,
} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  MemoryRouter,
  Route,
  Routes,
  useParams,
} from 'react-router-dom'
import { beforeEach, vi } from 'vitest'
import { AuthContext } from '../../security/hooks/authContext.js'
import {
  calculatePayrollRun,
  closePayrollRun,
  createPayrollRun,
  getPayrollRun,
  getPayrollRunEmployeeLines,
  getPayrollRuns,
} from '../services/payrollRunService.js'
import { PayrollRunDetailPage } from './PayrollRunDetailPage.jsx'
import { PayrollRunsPage } from './PayrollRunsPage.jsx'

vi.mock('../services/payrollRunService.js', () => ({
  calculatePayrollRun: vi.fn(),
  closePayrollRun: vi.fn(),
  createPayrollRun: vi.fn(),
  getPayrollRun: vi.fn(),
  getPayrollRunEmployeeLines: vi.fn(),
  getPayrollRuns: vi.fn(),
}))

describe('Payroll pages', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    getPayrollRuns.mockResolvedValue([
      createRun({ payrollRunId: 7, status: 'Closed', totalNetAmount: 1_234_567.5 }),
    ])

    getPayrollRun.mockResolvedValue({
      payrollRun: createRun({ status: 'Open', employeeCount: 0 }),
      employees: [],
      warnings: [],
    })
  })

  it('lists payroll runs with their status and net amount', async () => {
    renderAt('/payroll', 'HumanResourcesStaff')

    expect(await screen.findByText('Noviembre 2026')).toBeInTheDocument()
    expect(screen.getByText('Cerrada')).toBeInTheDocument()
    // es-CR groups thousands with a (non-breaking) space.
    expect(screen.getByText(/1\s234\s567,50/)).toBeInTheDocument()

    // HR staff can read but not create.
    expect(
      screen.queryByRole('button', { name: /nueva planilla/i }),
    ).not.toBeInTheDocument()
  })

  it('creates a monthly payroll and opens it', async () => {
    const user = userEvent.setup()

    createPayrollRun.mockResolvedValue({
      payrollRun: createRun({ payrollRunId: 9, status: 'Open' }),
      employees: [],
      warnings: [],
    })

    renderAt('/payroll', 'HumanResourcesAdministrator')

    await user.click(
      await screen.findByRole('button', { name: /nueva planilla/i }),
    )

    const dialog = screen.getByRole('dialog', { name: /nueva planilla/i })

    await user.selectOptions(within(dialog).getByLabelText('Mes'), '11')
    await user.click(
      within(dialog).getByRole('button', { name: /crear planilla/i }),
    )

    expect(createPayrollRun).toHaveBeenCalledWith(
      expect.objectContaining({
        runType: 'Regular',
        periodMonth: 11,
      }),
    )
    expect(await screen.findByText('Detalle de planilla 9')).toBeInTheDocument()
  })

  it('calculates a payroll and shows totals and warnings', async () => {
    const user = userEvent.setup()

    calculatePayrollRun.mockResolvedValue({
      payrollRun: createRun({
        status: 'Calculated',
        employeeCount: 1,
        totalGrossAmount: 600_000,
        totalEmployeeContributionsAmount: 64_980,
        totalNetAmount: 535_020,
        totalEmployerContributionsAmount: 160_980,
        rowVersion: 'AAAAAAAAAAI=',
      }),
      employees: [createEmployee()],
      warnings: ['Ana Prueba: no tiene horario asignado.'],
    })

    renderAt('/payroll/runs/1', 'HumanResourcesAdministrator', true)

    await user.click(await screen.findByRole('button', { name: /^calcular$/i }))

    expect(calculatePayrollRun).toHaveBeenCalledWith(
      expect.objectContaining({
        payrollRunId: '1',
        expectedRowVersion: 'AAAAAAAAAAE=',
      }),
    )
    expect(await screen.findByText(/no tiene horario asignado/i)).toBeInTheDocument()
    expect(screen.getByText('Ana Prueba')).toBeInTheDocument()
    expect(screen.getAllByText(/535\s020,00/).length).toBeGreaterThan(0)
    expect(screen.getByRole('button', { name: /recalcular/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /cerrar planilla/i })).toBeInTheDocument()
  })

  it('closes a calculated payroll after confirmation', async () => {
    const user = userEvent.setup()

    getPayrollRun.mockResolvedValue({
      payrollRun: createRun({ status: 'Calculated', employeeCount: 1 }),
      employees: [createEmployee()],
      warnings: [],
    })

    closePayrollRun.mockResolvedValue({
      payrollRun: createRun({
        status: 'Closed',
        closedAtUtc: '2026-11-30T12:00:00Z',
        closedByEmailAddress: 'admin@lithomanager.local',
      }),
      employees: [createEmployee()],
      warnings: [],
    })

    renderAt('/payroll/runs/1', 'SuperAdministrator', true)

    await user.click(
      await screen.findByRole('button', { name: /cerrar planilla/i }),
    )

    const dialog = screen.getByRole('dialog', { name: /cerrar planilla/i })

    await user.click(
      within(dialog).getByRole('button', { name: /cerrar planilla/i }),
    )

    expect(closePayrollRun).toHaveBeenCalledTimes(1)
    expect(await screen.findByText(/ya no se puede recalcular/i)).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /calcular/i })).not.toBeInTheDocument()
  })

  it('shows the line breakdown of an employee grouped by kind', async () => {
    const user = userEvent.setup()

    getPayrollRun.mockResolvedValue({
      payrollRun: createRun({ status: 'Calculated', employeeCount: 1 }),
      employees: [createEmployee()],
      warnings: [],
    })

    getPayrollRunEmployeeLines.mockResolvedValue([
      createLine(1, 'BaseSalary', 'Earning', 'Salario base', 600_000, { quantity: 30 }),
      createLine(2, 'EmployeeSocialContribution', 'Deduction', 'CCSS Salud (SEM)', 33_000, { rate: 0.055, baseAmount: 600_000 }),
      createLine(3, 'EmployerSocialContribution', 'EmployerContribution', 'CCSS Salud (SEM)', 55_500, { rate: 0.0925, baseAmount: 600_000 }),
    ])

    renderAt('/payroll/runs/1', 'HumanResourcesStaff', true)

    await user.click(
      await screen.findByRole('button', { name: /ver detalle de ana prueba/i }),
    )

    const dialog = screen.getByRole('dialog', { name: 'Ana Prueba' })

    expect(await within(dialog).findByText('Ingresos')).toBeInTheDocument()
    expect(within(dialog).getByText('Deducciones')).toBeInTheDocument()
    expect(within(dialog).getByText(/aportes patronales/i)).toBeInTheDocument()
    expect(within(dialog).getByText('30 días')).toBeInTheDocument()
    expect(within(dialog).getByText(/5,5\s?% de/)).toBeInTheDocument()

    // HR staff cannot calculate.
    expect(screen.queryByRole('button', { name: /calcular/i })).not.toBeInTheDocument()
  })
})

function renderAt(path, roleCode, withDetailRoute = false) {
  return render(
    <AuthContext.Provider
      value={{
        accessToken: 'fake-access-token',
        user: { roleCode, employeeId: null },
      }}
    >
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path="/payroll" element={<PayrollRunsPage />} />
          <Route
            path="/payroll/runs/:payrollRunId"
            element={withDetailRoute ? <PayrollRunDetailPage /> : <RunIdProbe />}
          />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

function RunIdProbe() {
  const { payrollRunId } = useParams()

  return <p>{`Detalle de planilla ${payrollRunId}`}</p>
}

function createRun(overrides) {
  return {
    payrollRunId: 1,
    runType: 'Regular',
    periodYear: 2026,
    periodMonth: 11,
    periodStartDate: '2026-11-01',
    periodEndDate: '2026-11-30',
    paymentDate: '2026-11-30',
    status: 'Open',
    employeeCount: 0,
    totalGrossAmount: 0,
    totalEmployeeContributionsAmount: 0,
    totalIncomeTaxAmount: 0,
    totalNetAmount: 0,
    totalEmployerContributionsAmount: 0,
    calculatedAtUtc: null,
    calculatedByEmailAddress: null,
    closedAtUtc: null,
    closedByEmailAddress: null,
    createdAtUtc: '2026-11-25T12:00:00Z',
    rowVersion: 'AAAAAAAAAAE=',
    ...overrides,
  }
}

function createEmployee() {
  return {
    employeeId: 30,
    identificationNumber: '1-1111-1111',
    employeeName: 'Ana Prueba',
    departmentName: 'Producción',
    jobTitle: 'Operaria',
    monthlyBaseSalary: 600_000,
    paidDays: 30,
    grossAmount: 600_000,
    employeeContributionsAmount: 64_980,
    incomeTaxAmount: 0,
    netAmount: 535_020,
    employerContributionsAmount: 160_980,
  }
}

function createLine(id, code, kind, description, amount, extra = {}) {
  return {
    payrollRunLineId: id,
    payrollConceptCode: code,
    conceptKind: kind,
    referenceCode: null,
    description,
    quantity: null,
    rate: null,
    baseAmount: null,
    amount,
    sortOrder: id,
    ...extra,
  }
}
