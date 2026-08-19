import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { LEAGUE_SCOPE_LABELS, LEAGUE_TYPE_LABELS, type LeagueSummary } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

export default function LeaguesMinePage() {
  const [myLeagues, setMyLeagues] = useState<LeagueSummary[] | null>(null)
  const [officialLeagues, setOfficialLeagues] = useState<LeagueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [joiningId, setJoiningId] = useState<number | null>(null)
  const [joinMessage, setJoinMessage] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)
    Promise.all([
      api.get<LeagueSummary[]>('/leagues/mine'),
      api.get<LeagueSummary[]>('/leagues/officials'),
    ])
      .then(([mine, officials]) => {
        if (cancelled) return
        setMyLeagues(mine)
        setOfficialLeagues(officials)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las Ligas.')
      })
    return () => { cancelled = true }
  }, [])

  async function handleJoinOfficial(leagueId: number) {
    setJoiningId(leagueId)
    setJoinMessage(null)
    try {
      await api.post<LeagueSummary>(`/leagues/${leagueId}/join`, {})
      setJoinMessage('Te uniste correctamente a la Liga Oficial.')
      const [mine, officials] = await Promise.all([
        api.get<LeagueSummary[]>('/leagues/mine'),
        api.get<LeagueSummary[]>('/leagues/officials'),
      ])
      setMyLeagues(mine)
      setOfficialLeagues(officials)
    } catch (err) {
      setJoinMessage(err instanceof ApiError ? err.message : 'Ocurrió un error al unirse.')
    } finally {
      setJoiningId(null)
      setTimeout(() => setJoinMessage(null), 4000)
    }
  }

  const loading = !myLeagues && !error

  return (
    <div>
      <div className="pp-header">
        <h1>Mis Ligas</h1>
        <div className="pp-header__actions">
          <Link to="/leagues/new" className="pp-btn pp-btn--primary" style={{ fontSize: '1rem', padding: '0.6rem 1.5rem' }}>
            + Crear Liga
          </Link>
          <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
            ✋ Unirme con código
          </Link>
        </div>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {loading && <StatusMessage kind="loading" message="Cargando Ligas..." />}
      {joinMessage && <StatusMessage kind="success" message={joinMessage} />}

      {/* ── Ligas Oficiales PlayPredict ── */}
      {officialLeagues && officialLeagues.length > 0 && (
        <>
          <div className="pp-section-title">
            <h2>🏆 Ligas Oficiales PlayPredict</h2>
            <p>Participá de las Ligas organizadas por PlayPredict</p>
          </div>
          <div className="pp-grid">
            {officialLeagues.map((l) => (
              <div key={l.id} className="pp-league-card pp-league-card--official">
                <div className="pp-league-card__header">
                  <h3 className="pp-league-card__name">{l.name}</h3>
                  <span className="pp-league-card__badge pp-league-card__badge--official">
                    🏆 OFICIAL
                  </span>
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
                  <span className="pp-league-card__status pp-league-card__status--active">Activa</span>
                  {l.isParticipant ? (
                    <Link to={`/leagues/${l.id}`} className="pp-league-card__action">
                      Entrar
                    </Link>
                  ) : (
                    <button
                      type="button"
                      className="pp-btn pp-btn--primary pp-btn--sm"
                      disabled={joiningId === l.id}
                      onClick={() => handleJoinOfficial(l.id)}
                    >
                      {joiningId === l.id ? 'Uniéndose...' : 'Participar'}
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </>
      )}

      {/* ── Mis Ligas (participando) ── */}
      {myLeagues && (
        <>
          <div className="pp-section-title">
            <h2>📋 Mis Ligas</h2>
            <p>Ligas donde participás</p>
          </div>
          {myLeagues.length === 0 ? (
            <div className="pp-empty">
              <span className="pp-empty__icon">🏆</span>
              <p className="pp-empty__text">
                No participás en ninguna Liga todavía.
                <br />
                Creá una Liga de Amigos o participá en una Liga Oficial arriba.
              </p>
              <div className="pp-empty__actions">
                <Link to="/leagues/new" className="pp-btn pp-btn--primary">
                  + Crear Liga de Amigos
                </Link>
                <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
                  Unirme con código
                </Link>
              </div>
            </div>
          ) : (
            <div className="pp-grid">
              {myLeagues.map((l) => (
                <div key={l.id} className="pp-league-card">
                  <div className="pp-league-card__header">
                    <h3 className="pp-league-card__name">{l.name}</h3>
                    {l.leagueType === 'Official' ? (
                      <span className="pp-league-card__badge pp-league-card__badge--official">
                        🏆 OFICIAL
                      </span>
                    ) : l.isCreator ? (
                      <span className="pp-league-card__badge pp-league-card__badge--mine">
                        MI LIGA
                      </span>
                    ) : (
                      <span className="pp-league-card__badge pp-league-card__badge--private">
                        AMIGOS
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
        </>
      )}
    </div>
  )
}
