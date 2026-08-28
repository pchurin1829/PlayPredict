import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { UserLeaguePosition } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import './DashboardHero.css'

interface DashboardHeroProps { positions: UserLeaguePosition[] }

export default function DashboardHero({ positions }: DashboardHeroProps) {
  const { user } = useAuth()
  const [showAll, setShowAll] = useState(false)
  const firstName = user?.firstName ?? 'Jugador'
  const visible = showAll ? positions : positions.slice(0, 4)

  return (
    <div className="dhero">
      <div className="dhero__main">
        <h1 className="dhero__title">¡Bienvenido, {firstName}!</h1>
        <p className="dhero__subtitle">Este es tu resumen de juego.</p>
      </div>
      <section className="dhero__positions" aria-labelledby="dashboard-positions-title">
        <h2 id="dashboard-positions-title">Mis posiciones</h2>
        {visible.length === 0 ? <p className="dhero__positions-empty">Todavía no hay posiciones disponibles.</p> : visible.map(position => (
          <Link key={position.leagueId} className="dhero__position" to={`/leagues/${position.leagueId}?tab=ranking`}>
            <strong>{position.leagueName}</strong>
            <span><b>{position.densePosition}°{position.sharedCount > 0 ? ` (compartido con ${position.sharedCount} más)` : ''}</b><em>{position.points} pts</em></span>
          </Link>
        ))}
        {positions.length > 4 && <button type="button" className="dhero__show-all" onClick={() => setShowAll(current => !current)}>{showAll ? 'Ver menos' : `Ver todas (${positions.length})`}</button>}
      </section>
    </div>
  )
}
