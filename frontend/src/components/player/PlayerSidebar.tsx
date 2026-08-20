import { Link, useLocation } from 'react-router-dom'
import ComingSoonBadge from './ComingSoonBadge'
import './PlayerSidebar.css'

interface SidebarItem {
  label: string
  icon: string
  to?: string
  comingSoon?: boolean
}

const GENERAL_ITEMS: SidebarItem[] = [
  { label: 'Inicio', icon: '🏠', to: '/' },
  { label: 'Mis Ligas', icon: '🏆', to: '/leagues' },
  { label: 'Explorar Competencias', icon: '🔍', to: '/competitions/explore' },
  { label: 'Ranking General', icon: '📊', to: '/rankings' },
  { label: 'Fixture', icon: '📅', comingSoon: true },
]

const ACCOUNT_ITEMS: SidebarItem[] = [
  { label: 'Mi Perfil', icon: '👤', to: '/profile' },
  { label: 'Amigos', icon: '👥', comingSoon: true },
  { label: 'Notificaciones', icon: '🔔', comingSoon: true },
  { label: 'Configuración', icon: '⚙️', comingSoon: true },
  { label: 'Ayuda', icon: '❓', comingSoon: true },
]

interface PlayerSidebarProps {
  collapsed?: boolean
  onToggle?: () => void
}

export default function PlayerSidebar({ collapsed = false, onToggle }: PlayerSidebarProps) {
  const location = useLocation()

  function isActive(to?: string): boolean {
    if (!to) return false
    if (to === '/') return location.pathname === '/'
    return location.pathname.startsWith(to)
  }

  function handleComingSoonClick(e: React.MouseEvent) {
    e.preventDefault()
  }

  return (
    <aside className={`psidebar ${collapsed ? 'psidebar--collapsed' : ''}`}>
      <button type="button" className="psidebar__toggle" onClick={onToggle} aria-label="Toggle sidebar">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          {collapsed ? (
            <path d="M9 18l6-6-6-6" />
          ) : (
            <path d="M15 18l-6-6 6-6" />
          )}
        </svg>
      </button>

      {!collapsed && (
        <>
          <div className="psidebar__section">
            <span className="psidebar__section-title">GENERAL</span>
            <nav className="psidebar__nav">
              {GENERAL_ITEMS.map((item) =>
                item.to && !item.comingSoon ? (
                  <Link
                    key={item.label}
                    to={item.to}
                    className={`psidebar__item ${isActive(item.to) ? 'psidebar__item--active' : ''}`}
                  >
                    <span className="psidebar__item-icon">{item.icon}</span>
                    <span className="psidebar__item-label">{item.label}</span>
                  </Link>
                ) : (
                  <button
                    key={item.label}
                    type="button"
                    className="psidebar__item psidebar__item--soon"
                    onClick={handleComingSoonClick}
                  >
                    <span className="psidebar__item-icon">{item.icon}</span>
                    <span className="psidebar__item-label">{item.label}</span>
                    <ComingSoonBadge />
                  </button>
                ),
              )}
            </nav>
          </div>

          <div className="psidebar__section">
            <span className="psidebar__section-title">MI CUENTA</span>
            <nav className="psidebar__nav">
              {ACCOUNT_ITEMS.map((item) =>
                item.to && !item.comingSoon ? (
                  <Link
                    key={item.label}
                    to={item.to}
                    className={`psidebar__item ${isActive(item.to) ? 'psidebar__item--active' : ''}`}
                  >
                    <span className="psidebar__item-icon">{item.icon}</span>
                    <span className="psidebar__item-label">{item.label}</span>
                  </Link>
                ) : (
                  <button
                    key={item.label}
                    type="button"
                    className="psidebar__item psidebar__item--soon"
                    onClick={handleComingSoonClick}
                  >
                    <span className="psidebar__item-icon">{item.icon}</span>
                    <span className="psidebar__item-label">{item.label}</span>
                    <ComingSoonBadge />
                  </button>
                ),
              )}
            </nav>
          </div>

          <div className="psidebar__invite">
            <p className="psidebar__invite-text">
              <strong>¡Invitá amigos!</strong>
              Sumá amigos a tus ligas y hacé la competencia más divertida.
            </p>
            <button type="button" className="psidebar__invite-btn" onClick={handleComingSoonClick}>
              Invitar amigos <ComingSoonBadge />
            </button>
          </div>
        </>
      )}
    </aside>
  )
}
