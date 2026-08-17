import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, vi } from 'vitest'
import { AuthContext } from '../../security/hooks/authContext.js'
import { getDepartments } from '../services/departmentService.js'
import {
  getAssignableEmployeeUsers,
  getEmployeeIdentificationTypes,
  getEmployeeSalaryHistory,
  getEmployees,
} from '../services/employeeService.js'
import { EmployeesPage } from './EmployeesPage.jsx'

vi.mock('../services/departmentService.js', () => ({
  getDepartments: vi.fn(),
}))

vi.mock('../services/employeeService.js', () => ({
  createEmployee: vi.fn(),
  getAssignableEmployeeUsers: vi.fn(),
  getEmployeeIdentificationTypes: vi.fn(),
  getEmployeeSalaryHistory: vi.fn(),
  getEmployees: vi.fn(),
  setEmployeeStatus: vi.fn(),
  updateEmployee: vi.fn(),
}))

const departments = [
  {
    departmentId: 1,
    departmentCode: 'HR',
    name: 'Recursos Humanos',
    description: null,
    isActive: true,
    createdAtUtc: '2026-08-12T08:00:00Z',
    updatedAtUtc: null,
    rowVersion: 'AAAAAAAAAAA=',
  },
]

const employees = [
  {
    employeeId: 7,
    userId: 12,
    emailAddress: 'ana@lithomanager.test',
    departmentId: 1,
    departmentCode: 'HR',
    departmentName: 'Recursos Humanos',
    isDepartmentActive: true,
    identificationType: 'CEDULA_FISICA',
    identificationNumber: '123456789',
    firstName: 'Ana',
    lastName: 'Mora',
    phoneNumber: '88888888',
    birthDate: '1995-03-10T00:00:00',
    hireDate: '2026-01-15T00:00:00',
    terminationDate: null,
    jobTitle: 'Analista RH',
    baseSalary: 650000,
    profileImagePath: null,
    isActive: true,
    createdAtUtc: '2026-08-12T08:00:00Z',
    updatedAtUtc: null,
    rowVersion: 'AAAAAAAAAAA=',
  },
]

describe('EmployeesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    getDepartments.mockResolvedValue(departments)
    getAssignableEmployeeUsers.mockResolvedValue([
      {
        userId: 12,
        emailAddress: 'ana@lithomanager.test',
        roleId: 2,
        roleCode: 'EMPLOYEE',
        roleName: 'Empleado',
        assignedEmployeeId: null,
        assignedEmployeeFirstName: null,
        assignedEmployeeLastName: null,
      },
    ])
    getEmployeeIdentificationTypes.mockResolvedValue([
      {
        identificationType: 'CEDULA_FISICA',
        name: 'Cédula física',
        minLength: 9,
        maxLength: 9,
        isNumericOnly: true,
        allowsLeadingZero: false,
        sortOrder: 1,
      },
      {
        identificationType: 'PASAPORTE',
        name: 'Pasaporte',
        minLength: 6,
        maxLength: 20,
        isNumericOnly: false,
        allowsLeadingZero: true,
        sortOrder: 3,
      },
    ])
    getEmployees.mockResolvedValue(employees)
    getEmployeeSalaryHistory.mockResolvedValue([
      {
        employeeSalaryHistoryId: 20,
        employeeId: 7,
        identificationType: 'CEDULA_FISICA',
        identificationNumber: '123456789',
        firstName: 'Ana',
        lastName: 'Mora',
        departmentId: 1,
        departmentCode: 'HR',
        departmentName: 'Recursos Humanos',
        baseSalary: 650000,
        effectiveFromDate: '2026-01-15T00:00:00',
        effectiveToDate: null,
        isCurrent: true,
        createdAtUtc: '2026-08-12T08:00:00Z',
        updatedAtUtc: null,
        rowVersion: 'AAAAAAAAAAA=',
      },
    ])
  })

  it('loads employees and opens the create form', async () => {
    const user = userEvent.setup()

    renderPage()

    expect(
      await screen.findByText('Ana Mora'),
    ).toBeInTheDocument()

    expect(screen.getByText(/123456789/)).toBeInTheDocument()
    expect(screen.getByText('Analista RH')).toBeInTheDocument()
    expect(getEmployeeIdentificationTypes).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
      }),
    )
    expect(getAssignableEmployeeUsers).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
      }),
    )
    expect(getEmployees).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
        departmentId: null,
        isActive: null,
        searchTerm: '',
      }),
    )

    await user.click(
      screen.getByRole('button', { name: /agregar/i }),
    )

    const dialog = screen.getByRole('dialog', {
      name: /nuevo empleado/i,
    })

    expect(dialog).toBeInTheDocument()

    expect(
      within(dialog).getByRole('combobox', {
        name: /usuario vinculado/i,
      }),
    ).toHaveDisplayValue('Sin usuario vinculado')

    expect(
      within(dialog).getByRole('option', {
        name: 'ana@lithomanager.test · Empleado',
      }),
    ).toBeInTheDocument()

    expect(
      within(dialog).getByRole('combobox', {
        name: /tipo de identificación/i,
      }),
    ).toHaveDisplayValue('Cédula física')

    expect(
      within(dialog).getByRole('combobox', {
        name: /departamento/i,
      }),
    ).toHaveDisplayValue('Selecciona un departamento')

    expect(
      within(dialog).getByRole('option', {
        name: 'HR · Recursos Humanos',
      }),
    ).toBeInTheDocument()
  })

  it('opens salary history for an employee', async () => {
    const user = userEvent.setup()

    renderPage()

    expect(
      await screen.findByText('Ana Mora'),
    ).toBeInTheDocument()

    await user.click(
      screen.getByRole('button', { name: /historial/i }),
    )

    expect(
      await screen.findByRole('dialog', {
        name: /historial salarial/i,
      }),
    ).toBeInTheDocument()

    expect(getEmployeeSalaryHistory).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
        employeeId: 7,
      }),
    )
    expect(screen.getAllByText('Actual').length).toBeGreaterThan(0)
  })
})

function renderPage() {
  return render(
    <AuthContext.Provider
      value={{
        accessToken: 'fake-access-token',
      }}
    >
      <EmployeesPage />
    </AuthContext.Provider>,
  )
}
