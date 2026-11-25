import { apiRequest } from '../../../services/apiClient.js'

let pendingRefresh = null

export function login({ emailAddress, password, signal }) {
  return apiRequest('/api/auth/login', {
    method: 'POST',
    body: {
      emailAddress,
      password,
    },
    signal,
  })
}

/**
 * Exchanges the HttpOnly refresh-token cookie for a new access token.
 *
 * The backend rotates refresh tokens: each one is revoked as soon as it is
 * used. Concurrent calls (for example React StrictMode running effects
 * twice, or several requests hitting a 401 at once) must therefore share a
 * single request; otherwise the second call would present an already
 * revoked token and the server would end the session.
 */
export function refreshSession() {
  if (!pendingRefresh) {
    pendingRefresh = apiRequest('/api/auth/refresh', {
      method: 'POST',
    }).finally(() => {
      pendingRefresh = null
    })
  }

  return pendingRefresh
}

/** Revokes the refresh token on the server and clears its cookie. */
export function logout() {
  return apiRequest('/api/auth/logout', {
    method: 'POST',
    // Let the request finish even if the user closes or reloads the page.
    keepalive: true,
  })
}

export function getCurrentUser({ accessToken, signal }) {
  return apiRequest('/api/auth/me', {
    accessToken,
    signal,
  })
}
