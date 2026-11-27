import { Navigate, Route, Routes } from 'react-router-dom'
import { DashboardPage } from './app/DashboardPage.jsx'
import { ForbiddenPage } from './app/ForbiddenPage.jsx'
import { NotFoundPage } from './app/NotFoundPage.jsx'
import { PlaceholderPage } from './app/PlaceholderPage.jsx'
import { DocumentsPage } from './features/documents/pages/DocumentsPage.jsx'
import { DepartmentsPage } from './features/humanResources/pages/DepartmentsPage.jsx'
import { EmployeesPage } from './features/humanResources/pages/EmployeesPage.jsx'
import { LeaveBalancesPage } from './features/leaveManagement/pages/LeaveBalancesPage.jsx'
import { LeaveRequestsPage } from './features/leaveManagement/pages/LeaveRequestsPage.jsx'
import { PayrollOperationsPage } from './features/payroll/pages/PayrollOperationsPage.jsx'
import { PayrollRunDetailPage } from './features/payroll/pages/PayrollRunDetailPage.jsx'
import { PayrollRunsPage } from './features/payroll/pages/PayrollRunsPage.jsx'
import { PayslipPage } from './features/payroll/pages/PayslipPage.jsx'
import { ProtectedRoute } from './features/security/components/ProtectedRoute.jsx'
import { LoginPage } from './features/security/pages/LoginPage.jsx'
import { AppLayout } from './layouts/AppLayout.jsx'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route
          path="human-resources/departments"
          element={<DepartmentsPage />}
        />
        <Route
          path="human-resources/employees"
          element={<EmployeesPage />}
        />
        <Route
          path="leave-management/requests"
          element={<LeaveRequestsPage />}
        />
        <Route
          path="leave-management/balances"
          element={<LeaveBalancesPage />}
        />
        <Route path="documents" element={<DocumentsPage />} />
        <Route
          path="forms"
          element={
            <PlaceholderPage
              title="Formularios"
              description="Gestiona formularios internos y respuestas."
            />
          }
        />
        <Route path="payroll" element={<PayrollRunsPage />} />
        <Route
          path="payroll/operations"
          element={<PayrollOperationsPage />}
        />
        <Route
          path="payroll/runs/:payrollRunId"
          element={<PayrollRunDetailPage />}
        />
        <Route
          path="payroll/runs/:payrollRunId/employees/:employeeId/payslip"
          element={<PayslipPage />}
        />
        <Route
          path="chat"
          element={
            <PlaceholderPage
              title="Mensajes"
              description="Comunicación interna del sistema."
            />
          }
        />
        <Route
          path="security/users"
          element={
            <PlaceholderPage
              title="Usuarios"
              description="Administra cuentas y accesos del sistema."
            />
          }
        />
        <Route path="forbidden" element={<ForbiddenPage />} />
      </Route>
      <Route path="/not-found" element={<NotFoundPage />} />
      <Route path="*" element={<Navigate to="/not-found" replace />} />
    </Routes>
  )
}

export default App
