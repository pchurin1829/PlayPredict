import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { LEAGUE_SCOPE_LABELS, LEAGUE_TYPE_LABELS, type LeagueSummary, type LeagueType } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

export default function LeaguesMinePage() {
  const [leagues, setLeagues] = useState<LeagueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)
    api
      .get<LeagueSummary[]>('/leagues/mine')
      .then((data) => {
        if (!cancelled) setLeagues(data)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar tus Ligas.')
      })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div>
      <div className="pp-header">
        <h1>Mis Ligas</h1>
        <div className="pp-header__actions">
          <Link to="/leagues/new" className="pp-btn pp-btn--primary" style={{ fontSize: '1rem', padding: '0.6rem 1.5rem' }}>
            + Crear Liga
          </Link>
          <Link to="/competitions/explore" className="pp-btn pp-btn--secondary">
            🔍 Explorar Competencias
          </Link>
          <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
            ✋ Unirse por código
          </Link>
        </div>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!leagues && !error && <StatusMessage kind="loading" message="Cargando tus Ligas..." />}

      {leagues && leagues.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">🏆</span>
          <p className="pp-empty__text">
            No participás en ninguna Liga todavía.
            <br />
            Explorá competencias o unite con un código de invitación.
          </p>
          <div className="pp-empty__actions">
            <Link to="/leagues/new" className="pp-btn pp-btn--primary">
              + Crear Liga
            </Link>
            <Link to="/competitions/explore" className="pp-btn pp-btn--secondary">
              Explorar Competencias
            </Link>
            <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
              Unirse por código
            </Link>
          </div>
        </div>
      )}

      {leagues && leagues.length > 0 && (
        <div className="pp-grid">
          {leagues.map((l) => (
            <div key={l.id} className="pp-league-card">
              <div className="pp-league-card__header">
                <h3 className="pp-league-card__name">{l.name}</h3>
                {l.leagueType === 'Official' ? (
                  <span className="pp-league-card__badge pp-league-card__badge--official">
                    🏆 {LEAGUE_TYPE_LABELS[l.leagueType]}
                  </span>
                ) : l.isCreator ? (
                  <span className="pp-league-card__badge pp-league-card__badge--mine">
                    MI LIGA
                  </span>
                ) : (
                  <span className="pp-league-card__badge pp-league-card__badge--private">
                    {LEAGUE_TYPE_LABELS[l.leagueType]}
                  </span>
                )}
              </div>
              <span className="pp-league-card__comp">⚽ {l.competitionName}</span>
              <div className="pp-league-card__meta">
                <span>
                  📋 {LEAGUE_SCOPE_LABELS[l.scopeType]}
                  {l.scopeType === 'RoundRange' && l.roundFromName && l.roundToName && (
                    <> ({l.roundFromName} → {l.roundToName})</>
                  )}
                </span>
                <span>👥 {l.participantsCount} participante{l.participantsCount !== 1 ? 's' : ''}</span>
              </div>
              <div className="pp-league-card__footer">
                <span className={`pp-league-card__status ${l.isActive ? 'pp-league-card__status--active' : 'pp-league-card__status--inactive'}`}>
                  {l.isActive ? 'Activa' : 'Inactiva'}
                </span>
                <Link to={`/leagues/${l.id}`} className="pp-league-card__action">
                  Entrar
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
