import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AuthResponse } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from '../components/StatusMessage'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)

    try {
      const response = await api.post<AuthResponse>('/auth/login', { email, password })
      login(response.token, response.user)
      const from = (location.state as { from?: { pathname: string } } | null)?.from?.pathname
      const home = response.user.roles.includes('ADMIN') ? '/competitions' : '/'
      navigate(from ?? home, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError('Ocurrió un error inesperado al iniciar sesión.')
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="form-card" onSubmit={handleSubmit}>
        <h1>Iniciar sesión</h1>

        {error && <StatusMessage kind="error" message={error} />}

        <div className="form-field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="text"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="username"
          />
        </div>

        <div className="form-field">
          <label htmlFor="password">Contraseña</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
          />
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Ingresando...' : 'Ingresar'}
          </button>
        </div>

        <p className="auth-alt-link">
          ¿No tenés cuenta? <Link to="/register">Registrate</Link>
        </p>
      </form>
    </div>
  )
}
