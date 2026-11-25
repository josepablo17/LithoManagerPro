import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import { AuthContext } from '../hooks/authContext.js'
import {
  login as requestLogin,
  logout as requestLogout,
  refreshSession,
} from '../services/authService.js'
import { unauthorizedSessionEventName } from '../../../services/apiClient.js'

// Renew the access token this long before it expires.
const refreshLeadTimeMs = 60_000
const minimumRefreshDelayMs = 5_000
const unauthorizedRetryWindowMs = 10_000

export function AuthProvider({ children }) {
  const [session, setSession] = useState(null)
  const [isAuthenticating, setIsAuthenticating] = useState(false)
  const [isRestoringSession, setIsRestoringSession] = useState(true)

  // The access token lives only in memory. On page load, try to obtain a
  // new one from the HttpOnly refresh-token cookie set at login.
  useEffect(() => {
    let isActive = true

    refreshSession()
      .then((response) => {
        if (isActive) {
          setSession(createAuthenticatedSession(response))
        }
      })
      .catch(() => {
        // No cookie, or it was expired or revoked: start anonymous.
      })
      .finally(() => {
        if (isActive) {
          setIsRestoringSession(false)
        }
      })

    return () => {
      isActive = false
    }
  }, [])

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

      setSession(createAuthenticatedSession(response))

      return response
    } finally {
      setIsAuthenticating(false)
    }
  }, [])

  const logout = useCallback(async () => {
    setSession(null)

    try {
      // Revoke the refresh token so a reload does not restore the session.
      await requestLogout()
    } catch {
      // The local session is already cleared; nothing else to do.
    }
  }, [])

  const lastRenewedAtRef = useRef(0)

  const renewSession = useCallback(async () => {
    try {
      const response = await refreshSession()

      lastRenewedAtRef.current = Date.now()
      setSession(createAuthenticatedSession(response))
    } catch {
      setSession(null)
    }
  }, [])

  const handleUnauthorized = useCallback(() => {
    // If the API rejects a token we just renewed, renewing again would
    // loop forever. End the session instead.
    if (Date.now() - lastRenewedAtRef.current < unauthorizedRetryWindowMs) {
      logout()
      return
    }

    renewSession()
  }, [logout, renewSession])

  // Renew the access token shortly before it expires.
  const accessTokenExpiresAtUtc = session?.accessTokenExpiresAtUtc

  useEffect(() => {
    if (!accessTokenExpiresAtUtc) {
      return undefined
    }

    const expiresAt = new Date(accessTokenExpiresAtUtc).getTime()

    if (Number.isNaN(expiresAt)) {
      return undefined
    }

    const delay = Math.max(
      expiresAt - Date.now() - refreshLeadTimeMs,
      minimumRefreshDelayMs,
    )

    const timeoutId = window.setTimeout(renewSession, delay)

    return () => window.clearTimeout(timeoutId)
  }, [accessTokenExpiresAtUtc, renewSession])

  // A 401 from the API means the access token is no longer valid. Try to
  // renew it once; pages reload their data when the token changes.
  useEffect(() => {
    window.addEventListener(
      unauthorizedSessionEventName,
      handleUnauthorized,
    )

    return () => {
      window.removeEventListener(
        unauthorizedSessionEventName,
        handleUnauthorized,
      )
    }
  }, [handleUnauthorized])

  const value = useMemo(
    () => ({
      user: session?.user ?? null,
      accessToken: session?.accessToken ?? null,
      requiresPasswordChange:
        session?.requiresPasswordChange ?? false,
      isAuthenticated: Boolean(session?.accessToken),
      isAuthenticating,
      isRestoringSession,
      login,
      logout,
    }),
    [isAuthenticating, isRestoringSession, login, logout, session],
  )

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  )
}

function createAuthenticatedSession(response) {
  return {
    user: response.user,
    accessToken: response.accessToken,
    requiresPasswordChange: false,
    accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
  }
}
