import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import App from './App.jsx'
import { AuthProvider } from './features/security/components/AuthProvider.jsx'

describe('App', () => {
  it('renders the login page for anonymous users', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <AuthProvider>
          <App />
        </AuthProvider>
      </MemoryRouter>,
    )

    expect(
      screen.getByRole('heading', { name: /iniciar sesión/i }),
    ).toBeInTheDocument()
  })
})
