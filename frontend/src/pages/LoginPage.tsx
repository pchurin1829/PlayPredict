import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AuthResponse } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from '../components/StatusMessage'
import './LoginPage.css'

const FEATURES = [
  { icon: '🏆', title: 'Competí', text: 'en diferentes ligas' },
  { icon: '📈', title: 'Sumá puntos', text: 'y subí en el ranking' },
  { icon: '🎁', title: 'Ganá premios', text: 'cada semana y cada mes' },
  { icon: '👥', title: 'Jugá con amigos', text: 'y demostrá quién sabe más' },
]

const SPONSOR_SLOTS = [
  { eyebrow: 'PUBLICIDAD', title: 'Tu marca aquí', text: 'Llegá a miles de fanáticos', cta: 'Conocé más' },
  { eyebrow: 'PUBLICIDAD', title: 'Anunciate en PlayPredict', text: 'Mostrá tu marca a nuestra comunidad', cta: 'Más información' },
  { eyebrow: 'PUBLICIDAD', title: 'Tu empresa puede estar acá', text: 'Sumá visibilidad en cada partido', cta: 'Más información' },
]

function StadiumScene() {
  return (
    <svg
      className="pp-login__scene"
      viewBox="0 0 800 900"
      preserveAspectRatio="xMidYMax slice"
      aria-hidden="true"
    >
      <defs>
        <radialGradient id="ppFloodlight" cx="50%" cy="0%" r="75%">
          <stop offset="0%" stopColor="#7a7a8c" stopOpacity="0.55" />
          <stop offset="45%" stopColor="#3a3a46" stopOpacity="0.25" />
          <stop offset="100%" stopColor="#0b0b10" stopOpacity="0" />
        </radialGradient>
        <linearGradient id="ppSky" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#1c1c24" />
          <stop offset="55%" stopColor="#111116" />
          <stop offset="100%" stopColor="#08080b" />
        </linearGradient>
        <linearGradient id="ppFade" x1="0" y1="0" x2="1" y2="0">
          <stop offset="0%" stopColor="#08080d" stopOpacity="0" />
          <stop offset="78%" stopColor="#08080d" stopOpacity="0.55" />
          <stop offset="100%" stopColor="#08080d" stopOpacity="1" />
        </linearGradient>
        <filter id="ppGrain">
          <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch" result="noise" />
          <feColorMatrix in="noise" type="saturate" values="0" />
          <feComponentTransfer>
            <feFuncA type="linear" slope="0.05" />
          </feComponentTransfer>
          <feComposite operator="over" in2="SourceGraphic" />
        </filter>
      </defs>

      <rect x="0" y="0" width="800" height="900" fill="url(#ppSky)" />
      <circle cx="140" cy="60" r="340" fill="url(#ppFloodlight)" />
      <circle cx="620" cy="20" r="260" fill="url(#ppFloodlight)" />

      {/* graderías / crowd suggestion */}
      <g opacity="0.5">
        {Array.from({ length: 7 }).map((_, row) =>
          Array.from({ length: 26 }).map((_, col) => (
            <circle
              key={`${row}-${col}`}
              cx={10 + col * 31 + (row % 2 === 0 ? 0 : 15)}
              cy={330 + row * 16}
              r="2.4"
              fill={row % 3 === 0 ? '#9a9aa8' : '#4c4c58'}
              opacity={0.25 + ((row + col) % 4) * 0.1}
            />
          )),
        )}
      </g>

      {/* pitch */}
      <rect x="0" y="470" width="800" height="430" fill="#101014" />
      <g opacity="0.16" stroke="#e8e8ee" strokeWidth="2">
        <line x1="0" y1="560" x2="800" y2="560" />
        <line x1="0" y1="650" x2="800" y2="650" />
        <line x1="0" y1="740" x2="800" y2="740" />
        <line x1="0" y1="830" x2="800" y2="830" />
      </g>

      {/* player silhouette, mid-kick */}
      <g transform="translate(230,470)">
        <ellipse cx="120" cy="392" rx="150" ry="16" fill="#000" opacity="0.5" />
        <path
          d="M60 40 C58 20 74 6 92 8 C110 10 118 26 114 44 C112 58 96 66 84 62 Z"
          fill="#141418"
        />
        <path
          d="M84 62 C70 78 58 96 66 120 C74 148 96 158 108 176 C118 192 100 214 78 226 L96 240 C128 224 150 196 140 168 C132 146 112 132 108 112 C104 96 118 84 132 74 C158 56 176 30 168 4 L142 10 C146 34 130 52 108 66 C98 72 90 68 84 62 Z"
          fill="#191921"
        />
        <path
          d="M96 240 L78 226 C48 244 10 250 -18 236 C-40 224 -46 198 -32 182 L-6 196 C-12 206 -8 216 2 220 C22 230 46 224 64 210 Z"
          fill="#101015"
        />
        <path
          d="M108 176 C140 182 172 176 196 152 C214 134 218 108 208 88 L182 100 C188 114 184 130 172 140 C156 154 132 158 112 152 Z"
          fill="#15151b"
        />
        <circle cx="222" cy="140" r="26" fill="#e6e6ea" opacity="0.92" />
        <path
          d="M222 122 L232 132 L228 146 L214 146 L210 132 Z"
          fill="#22222a"
          opacity="0.85"
        />
      </g>

      <rect x="0" y="0" width="800" height="900" filter="url(#ppGrain)" opacity="0.5" />
      <rect x="0" y="0" width="800" height="900" fill="url(#ppFade)" />
    </svg>
  )
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
        <div className="pp-login__bg-photo" aria-hidden="true" />
        <div className="pp-login__bg-overlay" aria-hidden="true" />
        <StadiumScene />

        <div className="pp-login__content">
          <div className="pp-login__intro">
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

            <p className="pp-login__tagline">Tu pasión. Tus predicciones. Tu competencia.</p>

            <ul className="pp-login__features">
              {FEATURES.map((f) => (
                <li key={f.title}>
                  <span className="pp-login__feature-icon">{f.icon}</span>
                  <span>
                    <strong>{f.title}</strong> {f.text}
                  </span>
                </li>
              ))}
            </ul>
          </div>

          <form className="pp-login__form" onSubmit={handleSubmit}>
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

      <aside className="pp-login__ads">
        {SPONSOR_SLOTS.map((slot) => (
          <div className="pp-login__ad" key={slot.title}>
            <span className="pp-login__ad-eyebrow">{slot.eyebrow}</span>
            <strong className="pp-login__ad-title">{slot.title}</strong>
            <span className="pp-login__ad-text">{slot.text}</span>
            <span className="pp-login__ad-cta">{slot.cta}</span>
          </div>
        ))}
      </aside>
    </div>
  )
}
