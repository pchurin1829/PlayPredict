import { Outlet, Link, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import PlayerHeader from './player/PlayerHeader'
import PlayerSidebar from './player/PlayerSidebar'
import { useState } from 'react'
import './Layout.css'
import './player/PlayerHeader.css'
import './player/PlayerSidebar.css'
import './player/PlayerLayout.css'
import './player/PlayerTheme.css'

const ADMIN_PATHS = ['/competitions', '/admin', '/editions', '/rounds', '/matches']

function isAdminPath(pathname: string): boolean {
  if (pathname === '/competitions/explore') return false
  if (pathname.startsWith('/competitions/') && !pathname.includes('/edit') && !pathname.includes('/editions')) return false
  return ADMIN_PATHS.some((p) => pathname.startsWith(p))
}

export default function Layout() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const isAdmin = user?.roles.includes('ADMIN') ?? false
  const showAdminLayout = isAdmin && isAdminPath(location.pathname)

  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  if (showAdminLayout) {
    return (
      <div className="layout">
        <header className="layout__header">
          <Link to="/competitions" className="layout__brand">PlayPredict</Link>
          <span className="layout__subtitle">Administración</span>
          <nav className="layout__nav">
            <Link to="/competitions">Competencias</Link>
            <Link to="/admin/prizes">Premios</Link>
            <Link to="/admin/users">Usuarios</Link>
            <Link to="/admin/experiences">Experiencias</Link>
            <Link to="/profile">{user ? `${user.firstName} ${user.lastName}` : 'Perfil'}</Link>
            <button type="button" className="btn btn-secondary layout__logout" onClick={logout}>
              Salir
            </button>
          </nav>
        </header>
        <main className="layout__content">
          <Outlet />
        </main>
      </div>
    )
  }

  return (
    <div className="playout">
      <PlayerHeader />
      <div className="playout__body">
        <PlayerSidebar
          collapsed={sidebarCollapsed}
          onToggle={() => setSidebarCollapsed((v) => !v)}
        />
        <main className="playout__content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
