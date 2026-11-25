import {
  act,
  render,
  screen,
} from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { StrictMode } from 'react'
import {
  MemoryRouter,
  Route,
  Routes,
} from 'react-router-dom'
import { beforeEach, vi } from 'vitest'
import { unauthorizedSessionEventName } from '../../../services/apiClient.js'
import { useAuth } from '../hooks/useAuth.js'
import { LoginPage } from '../pages/LoginPage.jsx'
import {
  logout as requestLogout,
  refreshSession,
} from '../services/authService.js'
import { AuthProvider } from './AuthProvider.jsx'
import { ProtectedRoute } from './ProtectedRoute.jsx'

vi.mock('../services/authService.js', () => ({
  login: vi.fn(),
  logout: vi.fn(),
  refreshSession: vi.fn(),
}))

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    requestLogout.mockResolvedValue(null)
  })

  it('restores the session from the refresh cookie on page load', async () => {
    refreshSession.mockResolvedValue(createRefreshResponse())

    renderApp('/leave-management/balances')

    expect(
      screen.getByText(/restaurando tu sesión/i),
    ).toBeInTheDocument()

    expect(
      await screen.findByText('Página protegida: admin@lithomanager.local'),
    ).toBeInTheDocument()
    expect(screen.getByTestId('token')).toHaveTextContent('access-1')
  })

  it('sends anonymous users to login when there is no session to restore', async () => {
    refreshSession.mockRejectedValue(new Error('Unauthorized'))

    renderApp('/leave-management/balances')

    expect(
      await screen.findByRole('heading', { name: /iniciar sesión/i }),
    ).toBeInTheDocument()
  })

  it('restores the session under StrictMode double effects', async () => {
    // StrictMode mounts, unmounts and remounts; the first effect's result
    // must not be lost. The single shared request is covered in
    // authService.test.js.
    refreshSession.mockResolvedValue(createRefreshResponse())

    render(
      <StrictMode>
        <MemoryRouter initialEntries={['/']}>
          <AuthProvider>
            <ProtectedRoute>
              <ProtectedPage />
            </ProtectedRoute>
          </AuthProvider>
        </MemoryRouter>
      </StrictMode>,
    )

    expect(
      await screen.findByText(/página protegida/i),
    ).toBeInTheDocument()
  })

  it('revokes the refresh token on logout', async () => {
    const user = userEvent.setup()

    refreshSession.mockResolvedValue(createRefreshResponse())

    renderApp('/')

    await user.click(
      await screen.findByRole('button', { name: /cerrar sesión/i }),
    )

    expect(requestLogout).toHaveBeenCalledTimes(1)
    expect(
      await screen.findByRole('heading', { name: /iniciar sesión/i }),
    ).toBeInTheDocument()
  })

  it('renews the access token after a 401 from the API', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })

    try {
      refreshSession
        .mockResolvedValueOnce(createRefreshResponse())
        .mockResolvedValueOnce(
          createRefreshResponse({ accessToken: 'access-2' }),
        )

      renderApp('/')

      await screen.findByText(/página protegida/i)

      // Outside the window that protects against renewal loops.
      await act(async () => {
        vi.advanceTimersByTime(15_000)
        window.dispatchEvent(
          new CustomEvent(unauthorizedSessionEventName),
        )
      })

      expect(
        await screen.findByText('access-2'),
      ).toBeInTheDocument()
      expect(requestLogout).not.toHaveBeenCalled()
    } finally {
      vi.useRealTimers()
    }
  })

  it('ends the session when a freshly renewed token is rejected again', async () => {
    refreshSession
      .mockResolvedValueOnce(createRefreshResponse())
      .mockResolvedValueOnce(
        createRefreshResponse({ accessToken: 'access-2' }),
      )

    renderApp('/')

    await screen.findByText(/página protegida/i)

    await act(async () => {
      window.dispatchEvent(
        new CustomEvent(unauthorizedSessionEventName),
      )
    })

    await screen.findByText('access-2')

    await act(async () => {
      window.dispatchEvent(
        new CustomEvent(unauthorizedSessionEventName),
      )
    })

    expect(requestLogout).toHaveBeenCalledTimes(1)
    expect(
      await screen.findByRole('heading', { name: /iniciar sesión/i }),
    ).toBeInTheDocument()
  })

  it('renews the access token shortly before it expires', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })

    try {
      refreshSession
        .mockResolvedValueOnce(
          createRefreshResponse({
            accessTokenExpiresAtUtc:
              new Date(Date.now() + 5 * 60_000).toISOString(),
          }),
        )
        .mockResolvedValueOnce(
          createRefreshResponse({ accessToken: 'access-2' }),
        )

      renderApp('/')

      await screen.findByText('access-1')

      await act(async () => {
        vi.advanceTimersByTime(4 * 60_000 + 1_000)
      })

      expect(
        await screen.findByText('access-2'),
      ).toBeInTheDocument()
    } finally {
      vi.useRealTimers()
    }
  })
})

function renderApp(path) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="*"
            element={(
              <ProtectedRoute>
                <ProtectedPage />
              </ProtectedRoute>
            )}
          />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

function ProtectedPage() {
  const { user, accessToken, logout } = useAuth()

  return (
    <main>
      <p>Página protegida: {user?.emailAddress}</p>
      <p data-testid="token">{accessToken}</p>
      <button type="button" onClick={logout}>
        Cerrar sesión
      </button>
    </main>
  )
}

function createRefreshResponse(overrides) {
  return {
    requiresPasswordChange: false,
    tokenType: 'Bearer',
    accessToken: 'access-1',
    accessTokenExpiresAtUtc:
      new Date(Date.now() + 15 * 60_000).toISOString(),
    user: {
      userId: 1,
      emailAddress: 'admin@lithomanager.local',
      roleCode: 'SuperAdministrator',
      employeeId: null,
    },
    ...overrides,
  }
}
