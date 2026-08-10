import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/AuthContext'
import ComingSoonBadge from './ComingSoonBadge'
import './PlayerHeader.css'

interface NavItem {
  label: string
  to?: string
  comingSoon?: boolean
}

const PLAYER_NAV: NavItem[] = [
  { label: 'Inicio', to: '/' },
  { label: 'Mis Ligas', to: '/leagues' },
  { label: 'Ranking', to: '/rankings' },
  { label: 'Fixture', comingSoon: true },
  { label: 'Premios', to: '/prizes' },
]

export default function PlayerHeader() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  function handleNavClick(item: NavItem) {
    if (item.comingSoon) return
    if (item.to) navigate(item.to)
  }

  return (
    <header className="pheader">
      <div className="pheader__left">
        <Link to="/" className="pheader__brand">
          <svg width="28" height="28" viewBox="0 0 48 46" fill="none" className="pheader__logo">
            <path fill="currentColor" d="M25.946 44.938c-.664.845-2.021.375-2.021-.698V33.937a2.26 2.26 0 0 0-2.262-2.262H10.287c-.92 0-1.456-1.04-.92-1.788l7.48-10.471c1.07-1.497 0-3.578-1.842-3.578H1.237c-.92 0-1.456-1.04-.92-1.788L10.013.474c.214-.297.556-.474.92-.474h28.894c.92 0 1.456 1.04.92 1.788l-7.48 10.471c-1.07 1.498 0 3.579 1.842 3.579h11.377c.943 0 1.473 1.088.89 1.83L25.947 44.94z" />
          </svg>
          PlayPredict
        </Link>
      </div>

      <nav className="pheader__nav">
        {PLAYER_NAV.map((item) => {
          const isActive = item.to && location.pathname === item.to
          const cls = [
            'pheader__nav-item',
            isActive && 'pheader__nav-item--active',
            item.comingSoon && 'pheader__nav-item--soon',
          ]
            .filter(Boolean)
            .join(' ')

          return item.to && !item.comingSoon ? (
            <Link key={item.label} to={item.to} className={cls}>
              {item.label}
            </Link>
          ) : (
            <button
              key={item.label}
              type="button"
              className={cls}
              onClick={() => handleNavClick(item)}
              disabled={item.comingSoon}
            >
              {item.label}
              {item.comingSoon && <ComingSoonBadge />}
            </button>
          )
        })}
      </nav>

      <div className="pheader__right">
        <button type="button" className="pheader__icon-btn pheader__icon-btn--soon" disabled>
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
            <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
            <path d="M13.73 21a2 2 0 0 1-3.46 0" />
          </svg>
          <ComingSoonBadge />
        </button>

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
