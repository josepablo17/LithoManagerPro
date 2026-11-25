import { beforeEach, vi } from 'vitest'
import { apiRequest } from '../../../services/apiClient.js'
import { refreshSession } from './authService.js'

vi.mock('../../../services/apiClient.js', () => ({
  apiRequest: vi.fn(),
}))

describe('refreshSession', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shares one request between concurrent callers', async () => {
    let resolveRequest

    apiRequest.mockReturnValue(
      new Promise((resolve) => {
        resolveRequest = resolve
      }),
    )

    const first = refreshSession()
    const second = refreshSession()

    resolveRequest({ accessToken: 'access-1' })

    await expect(first).resolves.toEqual({ accessToken: 'access-1' })
    await expect(second).resolves.toEqual({ accessToken: 'access-1' })
    expect(apiRequest).toHaveBeenCalledTimes(1)
    expect(apiRequest).toHaveBeenCalledWith(
      '/api/auth/refresh',
      { method: 'POST' },
    )
  })

  it('starts a new request once the previous one finished', async () => {
    apiRequest
      .mockResolvedValueOnce({ accessToken: 'access-1' })
      .mockRejectedValueOnce(new Error('Unauthorized'))

    await refreshSession()
    await expect(refreshSession()).rejects.toThrow('Unauthorized')

    expect(apiRequest).toHaveBeenCalledTimes(2)
  })
})
