import { useAuth } from '../../auth/AuthContext'
import './DashboardHero.css'

interface DashboardHeroProps {
  pendingCount?: number
  bestPosition?: number | null
  totalPoints?: number | null
}

export default function DashboardHero({
  pendingCount = 0,
  bestPosition,
  totalPoints,
}: DashboardHeroProps) {
  const { user } = useAuth()
  const firstName = user?.firstName ?? 'Jugador'

  return (
    <div className="dhero">
      <div className="dhero__main">
        <h1 className="dhero__title">¡Bienvenido, {firstName}!</h1>
        <p className="dhero__subtitle">
          {pendingCount > 0
            ? `Tenés ${pendingCount} pronóstico${pendingCount !== 1 ? 's' : ''} pendiente${pendingCount !== 1 ? 's' : ''}`
            : 'No tenés pronósticos pendientes por el momento'}
        </p>
        {pendingCount > 0 && (
          <a href="#proximos-partidos" className="dhero__cta">
            Pronosticar ahora
          </a>
        )}
      </div>
      <div className="dhero__stats">
        <div className="dhero__stat">
          <span className="dhero__stat-label">Tu mejor posición</span>
          <span className="dhero__stat-value">
            {bestPosition != null ? `${bestPosition}°` : '—'}
          </span>
        </div>
        <div className="dhero__stat">
          <span className="dhero__stat-label">Puntos totales</span>
          <span className="dhero__stat-value">
            {totalPoints != null ? totalPoints : '—'}
          </span>
        </div>
      </div>
    </div>
  )
}
