import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Alert } from '../../../components/ui/Alert.jsx'
import { Button } from '../../../components/ui/Button.jsx'
import { TextInput } from '../../../components/ui/TextInput.jsx'
import { useAuth } from '../hooks/useAuth.js'
import styles from './LoginPage.module.css'

export function LoginPage() {
  const [emailAddress, setEmailAddress] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const { login, isAuthenticating } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const destination = location.state?.from?.pathname ?? '/'

  async function handleSubmit(event) {
    event.preventDefault()
    setError('')

    if (!emailAddress.trim() || !password) {
      setError('Ingresa tu correo y contraseña.')
      return
    }

    try {
      const response = await login({
        emailAddress: emailAddress.trim(),
        password,
      })

      if (response.requiresPasswordChange) {
        setError(
          'Tu cuenta requiere cambio de contraseña. Este flujo se agregará en el siguiente bloque de seguridad visual.',
        )
        return
      }

      navigate(destination, { replace: true })
    } catch (requestError) {
      setError(
        requestError.message
          || 'No pudimos iniciar sesión. Inténtalo de nuevo.',
      )
    }
  }

  return (
    <main className={styles.page}>
      <section className={styles.panel} aria-labelledby="login-title">
        <div className={styles.brand}>
          <div className={styles.brandMark} aria-hidden="true">
            LM
          </div>
          <div>
            <p>LithoManagerPro</p>
            <span>Administración corporativa</span>
          </div>
        </div>

        <div className={styles.header}>
          <h1 id="login-title">Iniciar sesión</h1>
          <p>Accede para administrar departamentos, empleados y operaciones internas.</p>
        </div>

        {error ? <Alert variant="error">{error}</Alert> : null}

        <form className={styles.form} onSubmit={handleSubmit}>
          <TextInput
            id="emailAddress"
            label="Correo electrónico"
            type="email"
            autoComplete="email"
            value={emailAddress}
            onChange={(event) => setEmailAddress(event.target.value)}
            placeholder="empleado@empresa.com"
            required
          />

          <TextInput
            id="password"
            label="Contraseña"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />

          <Button
            type="submit"
            size="large"
            isLoading={isAuthenticating}
            loadingText="Ingresando..."
          >
            Ingresar
          </Button>
        </form>
      </section>
    </main>
  )
}
