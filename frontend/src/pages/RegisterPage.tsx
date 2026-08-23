import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AuthResponse } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from '../components/StatusMessage'
import './LoginPage.css'
import './RegisterPage.css'

export default function RegisterPage() {
  const { login } = useAuth()
  const navigate = useNavigate()

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setFieldErrors({})

    if (!confirmPassword.trim()) {
      setFieldErrors({ confirmPassword: ['Repetí la contraseña para confirmar.'] })
      return
    }
    if (password !== confirmPassword) {
      setFieldErrors({ confirmPassword: ['Las contraseñas no coinciden.'] })
      return
    }

    setSaving(true)

    try {
      const response = await api.post<AuthResponse>('/auth/register', {
        firstName,
        lastName,
        email,
        password,
      })
      login(response.token, response.user)
      const target = response.user.roles.includes('ADMIN') ? '/admin' : '/'
      navigate(target, { replace: true })
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

  const passwordMismatch = confirmPassword.length > 0 && password !== confirmPassword

  return (
    <div className="pp-register">
      <div className="pp-register__bg-photo" aria-hidden="true" />
      <div className="pp-register__bg-overlay" aria-hidden="true" />
      <div className="pp-register__card">
        <div className="pp-register__brand">
          <svg width="28" height="28" viewBox="0 0 48 46" fill="none" className="pp-login__logo-mark">
            <path
              fill="currentColor"
              d="M25.946 44.938c-.664.845-2.021.375-2.021-.698V33.937a2.26 2.26 0 0 0-2.262-2.262H10.287c-.92 0-1.456-1.04-.92-1.788l7.48-10.471c1.07-1.497 0-3.578-1.842-3.578H1.237c-.92 0-1.456-1.04-.92-1.788L10.013.474c.214-.297.556-.474.92-.474h28.894c.92 0 1.456 1.04.92 1.788l-7.48 10.471c-1.07 1.498 0 3.579 1.842 3.579h11.377c.943 0 1.473 1.088.89 1.83L25.947 44.94z"
            />
          </svg>
          <span>
            Play<strong>Predict</strong>
          </span>
        </div>

        <p className="pp-register__subtitle">Creá tu cuenta y empezá a competir</p>

        <form className="pp-register__form" onSubmit={handleSubmit}>
          <h1>Crear cuenta</h1>

          {error && <StatusMessage kind="error" message={error} />}

          <div className="pp-register__row">
            <div className="pp-register__field">
              <label htmlFor="firstName">Nombre</label>
              <div className="pp-register__input-wrap">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                  <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
                <input
                  id="firstName"
                  type="text"
                  placeholder="Tu nombre"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                />
              </div>
              {fieldErrors.firstName && (
                <span className="pp-register__field-error">{fieldErrors.firstName[0]}</span>
              )}
            </div>

            <div className="pp-register__field">
              <label htmlFor="lastName">Apellido</label>
              <div className="pp-register__input-wrap">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                  <circle cx="9" cy="7" r="4" />
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                  <path d="M16 3.13a4 4 0 0 1 0 7.75" />
                </svg>
                <input
                  id="lastName"
                  type="text"
                  placeholder="Tu apellido"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                />
              </div>
              {fieldErrors.lastName && (
                <span className="pp-register__field-error">{fieldErrors.lastName[0]}</span>
              )}
            </div>
          </div>

          <div className="pp-register__field">
            <label htmlFor="email">Email</label>
            <div className="pp-register__input-wrap">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                <rect x="2" y="4" width="20" height="16" rx="2" />
                <path d="m2 7 10 6 10-6" />
              </svg>
              <input
                id="email"
                type="text"
                placeholder="tu@email.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="username"
              />
            </div>
            {fieldErrors.email && <span className="pp-register__field-error">{fieldErrors.email[0]}</span>}
          </div>

          <div className="pp-register__field">
            <label htmlFor="password">Contraseña</label>
            <div className="pp-register__input-wrap">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                <rect x="4" y="11" width="16" height="9" rx="2" />
                <path d="M8 11V7a4 4 0 0 1 8 0v4" />
              </svg>
              <input
                id="password"
                type={showPassword ? 'text' : 'password'}
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                autoComplete="new-password"
              />
              <button
                type="button"
                className="pp-register__eye"
                onClick={() => setShowPassword((v) => !v)}
                aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
              >
                {showPassword ? (
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                    <path d="M3 3l18 18" />
                    <path d="M10.6 10.6a2 2 0 0 0 2.8 2.8" />
                    <path d="M9.3 5.5A10.7 10.7 0 0 1 12 5c5 0 9 4 10 7a12.5 12.5 0 0 1-3.1 4.1M6.5 6.6C4.6 8 3.2 9.8 2 12c1 3 5 7 10 7 1.3 0 2.5-.2 3.6-.6" />
                  </svg>
                ) : (
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                    <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7-10-7-10-7Z" />
                    <circle cx="12" cy="12" r="3" />
                  </svg>
                )}
              </button>
            </div>
            {fieldErrors.password && (
              <span className="pp-register__field-error">{fieldErrors.password[0]}</span>
            )}
          </div>

          <div className="pp-register__field">
            <label htmlFor="confirmPassword">Repetir contraseña</label>
            <div className={`pp-register__input-wrap${passwordMismatch ? ' pp-register__input-wrap--error' : ''}`}>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6">
                <rect x="4" y="11" width="16" height="9" rx="2" />
                <path d="M8 11V7a4 4 0 0 1 8 0v4" />
              </svg>
              <input
                id="confirmPassword"
                type={showPassword ? 'text' : 'password'}
                placeholder="Repetí tu contraseña"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                autoComplete="new-password"
              />
            </div>
            {passwordMismatch && (
              <span className="pp-register__field-error">Las contraseñas no coinciden.</span>
            )}
            {fieldErrors.confirmPassword && (
              <span className="pp-register__field-error">{fieldErrors.confirmPassword[0]}</span>
            )}
          </div>

          <div className="pp-register__actions">
            <button type="submit" className="pp-register__submit" disabled={saving || passwordMismatch}>
              {saving ? 'Creando cuenta...' : 'Crear cuenta'}
            </button>
          </div>

          <p className="pp-register__alt">
            ¿Ya tenés cuenta? <Link to="/login">Iniciar sesión</Link>
          </p>

          <p className="pp-register__footer">PlayPredict © 2026 · Todos los derechos reservados</p>
        </form>
      </div>
    </div>
  )
}
