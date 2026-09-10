import { Link, useLocation } from 'react-router-dom'
import './PlayerSidebar.css'

interface SidebarItem {
  label: string
  icon: string
  to: string
}

// P0 DEMO: solo entradas con funcionalidad MVP real. Sin ruta player para
// Fixture global (vive dentro de cada Liga), Amigos, Notificaciones,
// Configuración ni Ayuda: ocultas hasta que existan (no botones muertos).
const GENERAL_ITEMS: SidebarItem[] = [
  { label: 'Inicio', icon: '🏠', to: '/' },
  { label: 'Competencias Oficiales', icon: '🔍', to: '/competitions/explore' },
  { label: 'Mis Ligas', icon: '🏆', to: '/leagues' },
  { label: 'Ranking General', icon: '📊', to: '/rankings' },
]

const ACCOUNT_ITEMS: SidebarItem[] = [
  { label: 'Mi Perfil', icon: '👤', to: '/profile' },
  { label: 'Mis jugadores preferidos', icon: '⭐', to: '/preferred-players' },
]

interface PlayerSidebarProps {
  collapsed?: boolean
  onToggle?: () => void
  onNavigate?: () => void
}

export default function PlayerSidebar({ collapsed = false, onToggle, onNavigate }: PlayerSidebarProps) {
  const location = useLocation()

  function isActive(to: string): boolean {
    if (to === '/') return location.pathname === '/'
    return location.pathname.startsWith(to)
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
              {GENERAL_ITEMS.map((item) => (
                <Link
                  key={item.label}
                  to={item.to}
                  className={`psidebar__item ${isActive(item.to) ? 'psidebar__item--active' : ''}`}
                  onClick={onNavigate}
                >
                  <span className="psidebar__item-icon">{item.icon}</span>
                  <span className="psidebar__item-label">{item.label}</span>
                </Link>
              ))}
            </nav>
          </div>

          <div className="psidebar__section">
            <span className="psidebar__section-title">MI CUENTA</span>
            <nav className="psidebar__nav">
              {ACCOUNT_ITEMS.map((item) => (
                <Link
                  key={item.label}
                  to={item.to}
                  className={`psidebar__item ${isActive(item.to) ? 'psidebar__item--active' : ''}`}
                  onClick={onNavigate}
                >
                  <span className="psidebar__item-icon">{item.icon}</span>
                  <span className="psidebar__item-label">{item.label}</span>
                </Link>
              ))}
            </nav>
          </div>
        </>
      )}
    </aside>
  )
}
