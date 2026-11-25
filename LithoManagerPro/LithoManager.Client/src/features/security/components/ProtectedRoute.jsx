import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../hooks/useAuth.js'
import { SessionRestoringScreen } from './SessionRestoringScreen.jsx'

export function ProtectedRoute({ children }) {
  const location = useLocation()
  const { isAuthenticated, isRestoringSession } = useAuth()

  // Wait for the refresh attempt before deciding to send the user to login,
  // so a page reload keeps them on the page they were viewing.
  if (isRestoringSession) {
    return <SessionRestoringScreen />
  }

  if (!isAuthenticated) {
    return (
      <Navigate
        to="/login"
        state={{ from: location }}
        replace
      />
    )
  }

  return children
}
