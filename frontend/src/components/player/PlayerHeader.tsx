import { Link, useLocation } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import './PlayerHeader.css'

interface NavItem {
  label: string
  to: string
}

// P0 DEMO: Premios sí tiene ruta player (/prizes). Fixture global no:
// el fixture vive dentro de cada Liga. Campana sin funcionalidad: oculta.
const PLAYER_NAV: NavItem[] = [
  { label: 'Inicio', to: '/' },
  { label: 'Mis Ligas', to: '/leagues' },
  { label: 'Ranking', to: '/rankings' },
  { label: 'Premios', to: '/prizes' },
]

interface PlayerHeaderProps {
  menuOpen?: boolean
  onMenuToggle?: () => void
  onAdminReturn?: () => void
}

export default function PlayerHeader({ menuOpen = false, onMenuToggle, onAdminReturn }: PlayerHeaderProps) {
  const { user, logout } = useAuth()
  const location = useLocation()

  return (
    <header className="pheader">
      <button
        type="button"
        className="pheader__menu-btn"
        aria-label={menuOpen ? 'Cerrar menú' : 'Abrir menú'}
        aria-expanded={menuOpen}
        onClick={onMenuToggle}
      >
        <span aria-hidden="true">{menuOpen ? '×' : '☰'}</span>
      </button>
      <div className="pheader__left">
        <Link to="/" className="pheader__brand">
          <svg width="28" height="28" viewBox="0 0 48 46" fill="none" className="pheader__logo">
            <path fill="currentColor" d="M25.946 44.938c-.664.845-2.021.375-2.021-.698V33.937a2.26 2.26 0 0 0-2.262-2.262H10.287c-.92 0-1.456-1.04-.92-1.788l7.48-10.471c1.07-1.497 0-3.578-1.842-3.578H1.237c-.92 0-1.456-1.04-.92-1.788L10.013.474c.214-.297.556-.474.92-.474h28.894c.92 0 1.456 1.04.92 1.788l-7.48 10.471c-1.07 1.498 0 3.579 1.842 3.579h11.377c.943 0 1.473 1.088.89 1.83L25.947 44.94z" />
          </svg>
          PlayPredict
        </Link>
      </div>

      <nav className="pheader__nav">
        {PLAYER_NAV.map((item) => (
          <Link
            key={item.label}
            to={item.to}
            className={`pheader__nav-item${location.pathname === item.to ? ' pheader__nav-item--active' : ''}`}
          >
            {item.label}
          </Link>
        ))}
      </nav>

      <div className="pheader__right">
        <div className="pheader__user">
          <div className="pheader__avatar">
            {user ? `${user.firstName[0]}${user.lastName[0]}` : 'U'}
          </div>
          <div className="pheader__user-info">
            <span className="pheader__user-name">
              {user ? `${user.firstName} ${user.lastName}` : 'Usuario'}
            </span>
          </div>
          <div className="pheader__user-menu">
            {onAdminReturn && (
              <button type="button" className="pheader__user-menu-item pheader__user-menu-item--admin" onClick={onAdminReturn}>
                Volver a Administración
              </button>
            )}
            <Link to="/profile" className="pheader__user-menu-item">Mi Perfil</Link>
            <button type="button" className="pheader__user-menu-item pheader__user-menu-item--logout" onClick={logout}>
              Salir
            </button>
          </div>
        </div>
      </div>
    </header>
  )
}
