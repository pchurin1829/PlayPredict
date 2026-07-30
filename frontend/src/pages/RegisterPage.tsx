import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AuthResponse } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from '../components/StatusMessage'

export default function RegisterPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})

    try {
      const response = await api.post<AuthResponse>('/auth/register', {
        firstName,
        lastName,
        email,
        password,
      })
      login(response.token, response.user)
      navigate('/competitions', { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.fieldErrors)
      } else {
        setError('Ocurrió un error inesperado al registrarte.')
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="form-card" onSubmit={handleSubmit}>
        <h1>Crear cuenta</h1>

        {error && <StatusMessage kind="error" message={error} />}

        <div className="form-row">
          <div className="form-field">
            <label htmlFor="firstName">Nombre</label>
            <input
              id="firstName"
              type="text"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
            />
            {fieldErrors.firstName && (
              <span className="form-field-error">{fieldErrors.firstName[0]}</span>
            )}
          </div>

          <div className="form-field">
            <label htmlFor="lastName">Apellido</label>
            <input
              id="lastName"
              type="text"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
            />
            {fieldErrors.lastName && (
              <span className="form-field-error">{fieldErrors.lastName[0]}</span>
            )}
          </div>
        </div>

        <div className="form-field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            type="text"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="username"
          />
          {fieldErrors.email && <span className="form-field-error">{fieldErrors.email[0]}</span>}
        </div>

        <div className="form-field">
          <label htmlFor="password">Contraseña</label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
          />
          {fieldErrors.password && (
            <span className="form-field-error">{fieldErrors.password[0]}</span>
          )}
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Creando cuenta...' : 'Crear cuenta'}
          </button>
        </div>

        <p className="auth-alt-link">
          ¿Ya tenés cuenta? <Link to="/login">Iniciar sesión</Link>
        </p>
      </form>
    </div>
  )
}
