import { useAuth } from '../../auth/AuthContext'
import './DashboardHero.css'

interface DashboardHeroProps {
  bestPosition?: number | null
  totalPoints?: number | null
}

export default function DashboardHero({
  bestPosition,
  totalPoints,
}: DashboardHeroProps) {
  const { user } = useAuth()
  const firstName = user?.firstName ?? 'Jugador'

  return (
    <div className="dhero">
      <div className="dhero__main">
        <h1 className="dhero__title">¡Bienvenido, {firstName}!</h1>
        <p className="dhero__subtitle">Este es tu resumen de juego.</p>
      </div>
      <div className="dhero__stats">
        <div className={`dhero__stat ${bestPosition == null ? 'dhero__stat--empty' : ''}`}>
          <span className="dhero__stat-label">Tu mejor posición</span>
          <span className="dhero__stat-value">
            {bestPosition != null ? `${bestPosition}°` : '—'}
          </span>
        </div>
        <div className={`dhero__stat ${totalPoints == null ? 'dhero__stat--empty' : ''}`}>
          <span className="dhero__stat-label">Puntos totales</span>
          <span className="dhero__stat-value">
            {totalPoints != null ? totalPoints : '—'}
          </span>
        </div>
      </div>
    </div>
  )
}
