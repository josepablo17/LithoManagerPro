import { apiRequest } from '../../../services/apiClient.js'

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

export function getCurrentUser({ accessToken, signal }) {
  return apiRequest('/api/auth/me', {
    accessToken,
    signal,
  })
}
