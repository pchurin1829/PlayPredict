import { useEffect, useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AuthResponse, PublicLoginAppearance } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from '../components/StatusMessage'
import './LoginPage.css'

const DEFAULT_APPEARANCE: PublicLoginAppearance = {
  version: 'default-v1',
  main: { imageUrl: '/assets/el-nene-login/copa-el-nene-panel-principal.png', fitMode: 'Contain' },
  adTop: { imageUrl: '/assets/el-nene-login/producto-1.png', fitMode: 'Cover' },
  adMiddle: { imageUrl: '/assets/el-nene-login/producto-2.png', fitMode: 'Cover' },
  adBottom: { imageUrl: '/assets/el-nene-login/producto-3.png', fitMode: 'Cover' },
}

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [appearance, setAppearance] = useState<PublicLoginAppearance>(DEFAULT_APPEARANCE)

  useEffect(() => {
    let active = true
    api
      .get<PublicLoginAppearance>('/public/login-appearance')
      .then((data) => { if (active) setAppearance(data) })
      .catch(() => { /* mantiene la apariencia por defecto si falla o no está configurada */ })
    return () => { active = false }
  }, [])

  const ads = [
    { ...appearance.adTop, alt: 'Publicidad destacada 1' },
    { ...appearance.adMiddle, alt: 'Publicidad destacada 2' },
    { ...appearance.adBottom, alt: 'Publicidad destacada 3' },
  ]

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)

    try {
      const response = await api.post<AuthResponse>('/auth/login', { email, password })
      login(response.token, response.user)
      const from = (location.state as { from?: { pathname: string } } | null)?.from?.pathname
      const isAdmin = response.user.roles.includes('ADMIN')
      const home = isAdmin ? '/admin' : '/'
      navigate(isAdmin ? home : from ?? home, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 401) {
          setError('Email o contraseña incorrectos.')
        } else if (err.status === 0 || err.status >= 500) {
          setError('No pudimos conectar con PlayPredict. Intentá nuevamente.')
        } else {
          setError(err.message)
        }
      } else {
        setError('No pudimos conectar con PlayPredict. Intentá nuevamente.')
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="pp-login">
      <div className="pp-login__stage">
        <img
          className="pp-login__hero"
          src={appearance.main.imageUrl}
          alt="Copa El Nene: competí, sumá puntos y ganá premios"
          style={{ objectFit: appearance.main.fitMode === 'Cover' ? 'cover' : 'contain' }}
        />

        <div className="pp-login__form-position">
          <form className="pp-login__form" onSubmit={handleSubmit}>
            <div className="pp-login__brand">
              <svg width="30" height="30" viewBox="0 0 48 46" fill="none" className="pp-login__logo-mark">
                <path
                  fill="currentColor"
                  d="M25.946 44.938c-.664.845-2.021.375-2.021-.698V33.937a2.26 2.26 0 0 0-2.262-2.262H10.287c-.92 0-1.456-1.04-.92-1.788l7.48-10.471c1.07-1.497 0-3.578-1.842-3.578H1.237c-.92 0-1.456-1.04-.92-1.788L10.013.474c.214-.297.556-.474.92-.474h28.894c.92 0 1.456 1.04.92 1.788l-7.48 10.471c-1.07 1.498 0 3.579 1.842 3.579h11.377c.943 0 1.473 1.088.89 1.83L25.947 44.94z"
                />
              </svg>
              <span>
                Play<strong>Predict</strong>
              </span>
            </div>
            <h1>Iniciar sesión</h1>

            {error && <StatusMessage kind="error" message={error} />}

            <div className="pp-login__field">
              <label htmlFor="email">Email</label>
              <div className="pp-login__input-wrap">
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
            </div>

            <div className="pp-login__field">
              <label htmlFor="password">Contraseña</label>
              <div className="pp-login__input-wrap">
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
                  autoComplete="current-password"
                />
                <button
                  type="button"
                  className="pp-login__eye"
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
            </div>

            <div className="pp-login__actions">
              <button type="submit" className="pp-login__submit" disabled={saving}>
                {saving ? 'Ingresando...' : 'Ingresar'}
              </button>
            </div>

            <p className="pp-login__alt">
              ¿No tenés cuenta? <Link to="/register">Registrate</Link>
            </p>

            <p className="pp-login__footer">PlayPredict © 2026 · Todos los derechos reservados</p>
          </form>
        </div>
      </div>

      <aside className="pp-login__ads" aria-label="Ofertas de Supermercados El Nene">
        {ads.map((ad) => (
          <div className="pp-login__ad" key={ad.imageUrl}>
            <img src={ad.imageUrl} alt={ad.alt} style={{ objectFit: ad.fitMode === 'Contain' ? 'contain' : 'cover' }} />
          </div>
        ))}
      </aside>
    </div>
  )
}
