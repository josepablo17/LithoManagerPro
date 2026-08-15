import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, vi } from 'vitest'
import { AuthContext } from '../../security/hooks/authContext.js'
import { DocumentsPage } from './DocumentsPage.jsx'
import {
  getDocumentEmployeeOptions,
  getDocumentTypes,
  getEmployeeDocuments,
} from '../services/documentService.js'

vi.mock('../services/documentService.js', () => ({
  createEmployeeDocument: vi.fn(),
  downloadEmployeeDocument: vi.fn(),
  ensureEmployeeRecord: vi.fn(),
  getDocumentEmployeeOptions: vi.fn(),
  getDocumentTypes: vi.fn(),
  getEmployeeDocuments: vi.fn(),
  setEmployeeDocumentStatus: vi.fn(),
  updateEmployeeDocument: vi.fn(),
}))

const documentTypes = [
  {
    documentTypeId: 1,
    documentTypeCode: 'CONTRACT',
    name: 'Contrato',
    defaultIsVisibleToEmployee: true,
    isActive: true,
    rowVersion: 'AAAAAAAAAAA=',
  },
]

const employeeOptions = [
  {
    employeeId: 7,
    identificationNumber: '1-1111-1111',
    firstName: 'Ana',
    lastName: 'Mora',
    departmentId: 3,
    departmentCode: 'HR',
    departmentName: 'Recursos humanos',
    jobTitle: 'Especialista RH',
  },
]

const documents = [
  {
    employeeDocumentId: 10,
    employeeRecordId: 2,
    employeeId: 7,
    identificationNumber: '1-1111-1111',
    firstName: 'Ana',
    lastName: 'Mora',
    departmentId: 3,
    departmentCode: 'HR',
    departmentName: 'Recursos humanos',
    documentTypeId: 1,
    documentTypeCode: 'CONTRACT',
    documentTypeName: 'Contrato',
    title: 'Contrato laboral',
    description: 'Contrato vigente',
    originalFileName: 'contrato.pdf',
    contentType: 'application/pdf',
    fileSizeBytes: 128,
    issuedDate: '2026-08-01T00:00:00',
    expirationDate: null,
    isVisibleToEmployee: true,
    isActive: true,
    createdAtUtc: '2026-08-12T08:00:00Z',
    updatedAtUtc: null,
    rowVersion: 'AAAAAAAAAAA=',
  },
]

describe('DocumentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    getDocumentTypes.mockResolvedValue(documentTypes)
    getDocumentEmployeeOptions.mockResolvedValue(employeeOptions)
    getEmployeeDocuments.mockResolvedValue(documents)
  })

  it('loads documents and opens the upload form for administrators', async () => {
    const user = userEvent.setup()

    renderPage({
      roleCode: 'SuperAdministrator',
    })

    expect(
      await screen.findByText('Contrato laboral'),
    ).toBeInTheDocument()

    expect(
      screen.getByText(
        'Administra expedientes y documentos laborales.',
      ),
    ).toBeInTheDocument()
    expect(screen.getByText('Ana Mora')).toBeInTheDocument()
    expect(screen.getAllByText('Contrato')).toHaveLength(2)
    expect(getDocumentTypes).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
        isActive: true,
      }),
    )
    expect(getDocumentEmployeeOptions).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
      }),
    )
    expect(getEmployeeDocuments).toHaveBeenCalledWith(
      expect.objectContaining({
        accessToken: 'fake-access-token',
        isActive: true,
        isVisibleToEmployee: null,
      }),
    )

    await user.click(
      screen.getByRole('button', { name: /^cargar$/i }),
    )

    expect(
      screen.getByRole('dialog', {
        name: /cargar documento/i,
      }),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('combobox', { name: /^empleado/i }),
    ).toHaveTextContent(
      'Ana Mora · 1-1111-1111 · Recursos humanos',
    )
  })

  it('renders employee documents without administrative actions', async () => {
    renderPage({
      roleCode: 'Employee',
      employeeId: 7,
    })

    expect(
      await screen.findByText('Contrato laboral'),
    ).toBeInTheDocument()

    expect(
      screen.getByText('Consulta tus documentos laborales.'),
    ).toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /^cargar$/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /editar/i }),
    ).not.toBeInTheDocument()
    expect(
      screen.queryByRole('button', { name: /desactivar/i }),
    ).not.toBeInTheDocument()
    expect(getDocumentTypes).not.toHaveBeenCalled()
    expect(getDocumentEmployeeOptions).not.toHaveBeenCalled()
  })
})

function renderPage(user) {
  return render(
    <AuthContext.Provider
      value={{
        accessToken: 'fake-access-token',
        user,
      }}
    >
      <DocumentsPage />
    </AuthContext.Provider>,
  )
}
