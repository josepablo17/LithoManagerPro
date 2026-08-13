import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, vi } from 'vitest'
import { AuthContext } from '../../security/hooks/authContext.js'
import { DepartmentsPage } from './DepartmentsPage.jsx'
import { getDepartments } from '../services/departmentService.js'

vi.mock('../services/departmentService.js', () => ({
  createDepartment: vi.fn(),
  getDepartments: vi.fn(),
  setDepartmentStatus: vi.fn(),
  updateDepartment: vi.fn(),
}))

describe('DepartmentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    getDepartments.mockResolvedValue([
      {
        departmentId: 1,
        departmentCode: 'FIN',
        name: 'Finanzas',
        description: 'Administración financiera',
        isActive: true,
        createdAtUtc: '2026-08-12T08:00:00Z',
        updatedAtUtc: null,
        rowVersion: 'AAAAAAAAAAA=',
      },
    ])
  })

  it('loads departments and opens the create form', async () => {
    const user = userEvent.setup()

    renderPage()

    expect(
      await screen.findByText('Finanzas'),
    ).toBeInTheDocument()

    expect(screen.getByText('FIN')).toBeInTheDocument()
    expect(screen.getByText('Activo')).toBeInTheDocument()
    expect(getDepartments).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
        isActive: null,
        searchTerm: '',
      }),
    )

    await user.click(
      screen.getByRole('button', { name: /agregar/i }),
    )

    expect(
      screen.getByRole('dialog', {
        name: /nuevo departamento/i,
      }),
    ).toBeInTheDocument()
  })

  it('opens a styled status confirmation dialog', async () => {
    const user = userEvent.setup()

    renderPage()

    expect(
      await screen.findByText('Finanzas'),
    ).toBeInTheDocument()

    await user.click(
      screen.getByRole('button', { name: /desactivar/i }),
    )

    expect(
      screen.getByRole('dialog', {
        name: /desactivar departamento/i,
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByText(/revisa que no tenga empleados activos/i),
    ).toBeInTheDocument()
  })
})

function renderPage() {
  return render(
    <AuthContext.Provider
      value={{
        accessToken: 'fake-access-token',
      }}
    >
      <DepartmentsPage />
    </AuthContext.Provider>,
  )
}
