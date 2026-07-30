import { Link, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import './Layout.css'

export default function Layout() {
  const { user, logout } = useAuth()

  return (
    <div className="layout">
      <header className="layout__header">
        <Link to="/competitions" className="layout__brand">
          PlayPredict
        </Link>
        <span className="layout__subtitle">Panel administrativo</span>

        <nav className="layout__nav">
          <Link to="/competitions">Fixture</Link>
          <Link to="/predictions">Pronósticos</Link>
          {user?.roles.includes('ADMIN') && <Link to="/admin/users">Usuarios</Link>}
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
