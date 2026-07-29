import { Link, Outlet } from 'react-router-dom'
import './Layout.css'

export default function Layout() {
  return (
    <div className="layout">
      <header className="layout__header">
        <Link to="/competitions" className="layout__brand">
          PlayPredict
        </Link>
        <span className="layout__subtitle">Panel administrativo — Fixture</span>
      </header>
      <main className="layout__content">
        <Outlet />
      </main>
    </div>
  )
}
