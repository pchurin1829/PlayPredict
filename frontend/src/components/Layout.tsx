import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import PlayerHeader from './player/PlayerHeader'
import PlayerSidebar from './player/PlayerSidebar'
import { useEffect, useRef, useState } from 'react'
import { useCompanySettings } from '../company/CompanySettingsContext'
import './Layout.css'
import './player/PlayerHeader.css'
import './player/PlayerSidebar.css'
import './player/PlayerLayout.css'
import './player/PlayerTheme.css'

function getAdminNav(companyShortName: string) { return [
  { title: 'ADMINISTRACIÓN', items: [{ label: 'Dashboard', to: '/admin' }] },
  { title: 'FUENTES DEPORTIVAS', items: [
    { label: 'Organizaciones deportivas', soon: true },
    { label: 'Competencias de referencia', to: '/competitions' },
    { label: 'Equipos', to: '/admin/teams' },
  ] },
  { title: 'OPERACIÓN', items: [
    { label: 'Fixture / Partidos', to: '/admin/fixture' },
    { label: `Competencias ${companyShortName}`, to: '/admin/official-leagues' },
    { label: 'Resultados', to: '/admin/results' },
  ] },
  { title: 'JUEGO', items: [
    { label: 'Rankings', to: '/rankings' },
    { label: 'Configuración', to: '/admin/settings' },
  ] },
] }

const PLAYER_SIDEBAR_BREAKPOINT = 1024

function isMobilePlayerViewport(): boolean {
  return typeof window !== 'undefined' && window.innerWidth <= PLAYER_SIDEBAR_BREAKPOINT
}

export default function Layout() {
  const { user, logout, viewMode, setViewMode } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const isAdmin = user?.roles.includes('ADMIN') ?? false
  const showAdminLayout = isAdmin && viewMode === 'admin'
  const { company } = useCompanySettings()
  const adminNav = getAdminNav(company.shortName || 'PlayPredict')

  const [sidebarCollapsed, setSidebarCollapsed] = useState(isMobilePlayerViewport)
  const wasMobileViewport = useRef(isMobilePlayerViewport())

  useEffect(() => {
    if (isMobilePlayerViewport()) setSidebarCollapsed(true)
  }, [location.pathname])

  useEffect(() => {
    function handleResize() {
      const isMobile = isMobilePlayerViewport()
      if (isMobile !== wasMobileViewport.current) {
        setSidebarCollapsed(isMobile)
        wasMobileViewport.current = isMobile
      }
    }

    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  useEffect(() => {
    if (isAdmin && location.pathname.startsWith('/admin') && viewMode !== 'admin') {
      setViewMode('admin')
    }
  }, [isAdmin, location.pathname, setViewMode, viewMode])

  function switchToPlayer() {
    setViewMode('player')
    navigate('/')
  }

  function switchToAdmin() {
    setViewMode('admin')
    navigate('/admin')
  }

  if (showAdminLayout) {
    return (
      <div className="layout">
        <header className="layout__header">
          <Link to="/admin" className="layout__brand">PlayPredict</Link>
          <span className="layout__admin-badge">ADMIN</span>
          <details className="layout__profile">
            <summary>
              <span className="layout__profile-avatar">{user ? `${user.firstName[0]}${user.lastName[0]}` : 'A'}</span>
              <span className="layout__profile-name">{user ? `${user.firstName} ${user.lastName}` : 'Administrador'}</span>
            </summary>
            <div className="layout__profile-menu">
              <Link to="/admin">Administración</Link>
              <button type="button" onClick={switchToPlayer}>Vista jugador</button>
              <Link to="/profile">Mi perfil</Link>
              <button type="button" className="layout__profile-logout" onClick={logout}>Cerrar sesión</button>
            </div>
          </details>
        </header>
        <div className="layout__body">
          <aside className="layout__sidebar" aria-label="Navegación administrativa">
            <nav className="layout__admin-nav">
              {adminNav.map((group) => (
                <div key={group.title} className="layout__admin-group">
                  <div className="layout__sidebar-title">{group.title}</div>
                  {group.items.map((item) => item.to ? (
                    <Link key={item.label} to={item.to} className={location.pathname === item.to ? 'layout__admin-link layout__admin-link--active' : 'layout__admin-link'}>{item.label}</Link>
                  ) : (
                    <span key={item.label} className="layout__admin-link layout__admin-link--soon"><span>{item.label}</span><small>PRÓXIMAMENTE</small></span>
                  ))}
                </div>
              ))}
            </nav>
          </aside>
          <main className="layout__content"><Outlet /></main>
        </div>
      </div>
    )
  }

  return (
    <div className="playout">
      <PlayerHeader
        menuOpen={!sidebarCollapsed}
        onMenuToggle={() => setSidebarCollapsed((value) => !value)}
        onAdminReturn={isAdmin ? switchToAdmin : undefined}
      />
      <div className="playout__body">
        <PlayerSidebar
          collapsed={sidebarCollapsed}
          onToggle={() => setSidebarCollapsed((v) => !v)}
          onNavigate={() => {
            if (isMobilePlayerViewport()) setSidebarCollapsed(true)
          }}
        />
        {!sidebarCollapsed && (
          <button
            type="button"
            className="playout__backdrop"
            aria-label="Cerrar menú"
            onClick={() => setSidebarCollapsed(true)}
          />
        )}
        <main className="playout__content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
