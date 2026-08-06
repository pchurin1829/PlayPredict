import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import './Layout.css'

export default function Layout() {
  const { user, logout } = useAuth()
  const isAdmin = user?.roles.includes('ADMIN') ?? false

  return (
    <div className="layout">
      <header className="layout__header">
        <Link to={isAdmin ? '/competitions' : '/leagues'} className="layout__brand">
          PlayPredict
        </Link>
        <span className="layout__subtitle">Panel administrativo</span>

        <nav className="layout__nav">
          <Link to="/leagues">Mis Ligas</Link>
          <Link to="/competitions/explore">Explorar Competencias</Link>
          <Link to="/leagues/join">Unirse por código</Link>
          <Link to="/rankings">Rankings</Link>
          {isAdmin && <Link to="/competitions">Fixture</Link>}
          {isAdmin && <Link to="/prizes">Premios</Link>}
          {isAdmin && <Link to="/admin/users">Usuarios</Link>}
          {isAdmin && <Link to="/admin/prizes">Administrar Premios</Link>}
          {isAdmin && <Link to="/admin/experiences">Experiencias</Link>}
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
