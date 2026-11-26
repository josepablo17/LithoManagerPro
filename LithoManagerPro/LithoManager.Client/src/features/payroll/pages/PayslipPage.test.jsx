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
} from 'react-router-dom'
import { afterEach, beforeEach, vi } from 'vitest'
import { AuthContext } from '../../security/hooks/authContext.js'
import {
  getPayrollRun,
  getPayrollRunEmployeeLines,
} from '../services/payrollRunService.js'
import { PayslipPage } from './PayslipPage.jsx'

vi.mock('../services/payrollRunService.js', () => ({
  getPayrollRun: vi.fn(),
  getPayrollRunEmployeeLines: vi.fn(),
}))

describe('PayslipPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    getPayrollRun.mockResolvedValue({
      payrollRun: createRun({ status: 'Closed', closedAtUtc: '2026-12-31T18:00:00Z' }),
      employees: [createEmployee()],
      warnings: [],
    })

    getPayrollRunEmployeeLines.mockResolvedValue([
      line(1, 'BaseSalary', 'Earning', 'Salario base', 500_000, { quantity: 30 }),
      line(2, 'BaseSalary', 'Earning', 'Rebajo por incapacidad (enfermedad común)', -48_387.1, { quantity: 2.9, referenceCode: 'CommonIllness' }),
      line(3, 'DisabilitySubsidy', 'Informational', 'Subsidio pagado por el patrono (enfermedad común)', 25_000),
      line(4, 'EmployeeSocialContribution', 'Deduction', 'CCSS Salud (SEM)', 24_838.71, { rate: 0.055, baseAmount: 451_612.9 }),
      line(5, 'EmployeeSocialContribution', 'Deduction', 'CCSS Pensiones (IVM)', 19_554.84, { rate: 0.0433, baseAmount: 451_612.9 }),
      line(6, 'EmployerSocialContribution', 'EmployerContribution', 'CCSS Salud (SEM)', 41_774.19, { rate: 0.0925, baseAmount: 451_612.9 }),
    ])
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('shows employee data, earnings, deductions and net pay', async () => {
    renderPage()

    const slip = await screen.findByRole('article', { name: 'Boleta de pago' })

    expect(within(slip).getByText('Ana Rojas')).toBeInTheDocument()
    expect(within(slip).getByText('1-1111-1111')).toBeInTheDocument()
    expect(within(slip).getByText('Planilla de diciembre 2026')).toBeInTheDocument()
    expect(within(slip).getByText('Ingresos')).toBeInTheDocument()
    expect(within(slip).getByText('Deducciones')).toBeInTheDocument()

    // The employer-paid subsidy is part of the earnings received.
    expect(within(slip).getByText(/subsidio pagado por el patrono/i)).toBeInTheDocument()

    // Disability deductions hide their commercial-day quantity.
    expect(within(slip).queryByText('2,9 días')).not.toBeInTheDocument()

    // Deductions are shown as negative amounts.
    expect(within(slip).getAllByText(/^-₡/).length).toBeGreaterThan(0)

    expect(within(slip).getByText(/427\s703,22/)).toBeInTheDocument()
    expect(within(slip).queryByText(/borrador/i)).not.toBeInTheDocument()
  })

  it('hides employer contributions unless requested', async () => {
    const user = userEvent.setup()

    renderPage()

    await screen.findByRole('article', { name: 'Boleta de pago' })

    expect(screen.queryByText(/aportes patronales \(pagados/i)).not.toBeInTheDocument()

    await user.click(screen.getByLabelText(/incluir aportes patronales/i))

    expect(screen.getByText(/aportes patronales \(pagados/i)).toBeInTheDocument()
    // Line and section total (a single employer line here).
    expect(screen.getAllByText(/41\s774,19/)).toHaveLength(2)
  })

  it('marks the slip as a draft when the payroll is not closed', async () => {
    getPayrollRun.mockResolvedValue({
      payrollRun: createRun({ status: 'Calculated' }),
      employees: [createEmployee()],
      warnings: [],
    })

    renderPage()

    expect(await screen.findByRole('note')).toHaveTextContent(/borrador/i)
  })

  it('prints with the browser', async () => {
    const user = userEvent.setup()
    const print = vi.spyOn(window, 'print').mockImplementation(() => {})

    renderPage()

    await user.click(await screen.findByRole('button', { name: /imprimir/i }))

    expect(print).toHaveBeenCalledTimes(1)
  })

  it('explains when the employee is not in the payroll', async () => {
    getPayrollRun.mockResolvedValue({
      payrollRun: createRun({ status: 'Closed' }),
      employees: [],
      warnings: [],
    })

    renderPage()

    expect(await screen.findByText(/no está en esta planilla/i)).toBeInTheDocument()
  })
})

function renderPage() {
  return render(
    <AuthContext.Provider
      value={{
        accessToken: 'fake-access-token',
        user: { roleCode: 'HumanResourcesAdministrator', employeeId: null },
      }}
    >
      <MemoryRouter initialEntries={['/payroll/runs/5/employees/1/payslip']}>
        <Routes>
          <Route
            path="/payroll/runs/:payrollRunId/employees/:employeeId/payslip"
            element={<PayslipPage />}
          />
        </Routes>
      </MemoryRouter>
    </AuthContext.Provider>,
  )
}

function createRun(overrides) {
  return {
    payrollRunId: 5,
    runType: 'Regular',
    periodYear: 2026,
    periodMonth: 12,
    periodStartDate: '2026-12-01',
    periodEndDate: '2026-12-31',
    paymentDate: '2026-12-31',
    status: 'Closed',
    closedAtUtc: null,
    ...overrides,
  }
}

function createEmployee() {
  return {
    employeeId: 1,
    identificationNumber: '1-1111-1111',
    employeeName: 'Ana Rojas',
    departmentName: 'Producción',
    jobTitle: 'Operaria',
    monthlyBaseSalary: 500_000,
    paidDays: 27.1,
    grossAmount: 451_612.9,
    employeeContributionsAmount: 48_909.68,
    incomeTaxAmount: 0,
    netAmount: 427_703.22,
    employerContributionsAmount: 121_167.73,
  }
}

function line(id, code, kind, description, amount, extra = {}) {
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
