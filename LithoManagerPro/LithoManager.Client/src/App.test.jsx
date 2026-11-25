import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { vi } from 'vitest'
import App from './App.jsx'
import { AuthProvider } from './features/security/components/AuthProvider.jsx'

vi.mock('./features/security/services/authService.js', () => ({
  login: vi.fn(),
  logout: vi.fn(),
  refreshSession: vi.fn(() =>
    Promise.reject(new Error('No refresh cookie'))),
}))

describe('App', () => {
  it('renders the login page for anonymous users', async () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider>
          <App />
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(
      await screen.findByRole('heading', { name: /iniciar sesión/i }),
    ).toBeInTheDocument()
  })
})
