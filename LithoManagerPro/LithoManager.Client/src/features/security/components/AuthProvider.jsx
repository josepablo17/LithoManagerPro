import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react'
import { AuthContext } from '../hooks/authContext.js'
import { login as requestLogin } from '../services/authService.js'
import { unauthorizedSessionEventName } from '../../../services/apiClient.js'

export function AuthProvider({ children }) {
  const [session, setSession] = useState(null)
  const [isAuthenticating, setIsAuthenticating] = useState(false)

  const login = useCallback(async ({ emailAddress, password }) => {
    setIsAuthenticating(true)

    try {
      const response = await requestLogin({
        emailAddress,
        password,
      })

      if (response.requiresPasswordChange) {
        setSession({
          user: response.user,
          accessToken: null,
          requiresPasswordChange: true,
          passwordChangeToken: response.passwordChangeToken,
        })

        return response
      }

      setSession({
        user: response.user,
        accessToken: response.accessToken,
        requiresPasswordChange: false,
        accessTokenExpiresAtUtc:
          response.accessTokenExpiresAtUtc,
      })

      return response
    } finally {
      setIsAuthenticating(false)
    }
  }, [])

  const logout = useCallback(() => {
    setSession(null)
  }, [])

  useEffect(() => {
    window.addEventListener(
      unauthorizedSessionEventName,
      logout,
    )

    return () => {
      window.removeEventListener(
        unauthorizedSessionEventName,
        logout,
      )
    }
  }, [logout])

  const value = useMemo(
    () => ({
      user: session?.user ?? null,
      accessToken: session?.accessToken ?? null,
      requiresPasswordChange:
        session?.requiresPasswordChange ?? false,
      isAuthenticated: Boolean(session?.accessToken),
      isAuthenticating,
      login,
      logout,
    }),
    [isAuthenticating, login, logout, session],
  )

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  )
}
