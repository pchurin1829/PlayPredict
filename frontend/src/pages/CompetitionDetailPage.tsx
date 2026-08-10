import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { LEAGUE_SCOPE_LABELS, type Competition, type Edition, type LeagueSummary, type Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

export default function CompetitionDetailPage() {
  const { competitionId } = useParams()

  const [competition, setCompetition] = useState<Competition | null>(null)
  const [activeEdition, setActiveEdition] = useState<Edition | null>(null)
  const [roundsCount, setRoundsCount] = useState(0)
  const [myLeagues, setMyLeagues] = useState<LeagueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    Promise.all([
      api.get<Competition>(`/competitions/${competitionId}`),
      api.get<Edition[]>(`/competitions/${competitionId}/editions`),
      api.get<LeagueSummary[]>('/leagues/mine'),
    ])
      .then(async ([c, editions, leagues]) => {
        if (cancelled) return
        setCompetition(c)
        setMyLeagues(leagues.filter((l) => l.competitionId === Number(competitionId)))

        const active = editions.find((e) => e.status === 'Active') ?? null
        setActiveEdition(active)

        if (active) {
          const rounds = await api.get<Round[]>(`/editions/${active.id}/rounds`)
          if (!cancelled) setRoundsCount(rounds.length)
        }
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudo cargar la Competencia.')
      })

    return () => {
      cancelled = true
    }
  }, [competitionId])

  if (error) {
    return (
      <div>
        <Link to="/competitions/explore" className="pp-back">← Explorar Competencias</Link>
        <StatusMessage kind="error" message={error} />
      </div>
    )
  }

  if (!competition) {
    return <StatusMessage kind="loading" message="Cargando Competencia..." />
  }

  return (
    <div>
      <Link to="/competitions/explore" className="pp-back">← Explorar Competencias</Link>

      <div className="pp-info-card">
        <h1 className="pp-info-card__title" style={{ fontSize: '1.4rem' }}>🏆 {competition.name}</h1>
        {competition.description && (
          <p style={{ margin: '0.5rem 0 0', fontSize: '0.9rem', color: 'var(--color-text-secondary)' }}>
            {competition.description}
          </p>
        )}
        <div className="pp-info-card__meta" style={{ marginTop: '0.75rem' }}>
          <span className="pp-info-card__meta-item">🏅 {competition.sport}</span>
          {activeEdition && (
            <span className="pp-info-card__meta-item">📍 {activeEdition.name}</span>
          )}
          {roundsCount > 0 && (
            <span className="pp-info-card__meta-item">📅 {roundsCount} fecha{roundsCount !== 1 ? 's' : ''}</span>
          )}
        </div>
        <div className="pp-info-card__cta">
          <Link to={`/leagues/new?competitionId=${competition.id}`} className="pp-btn pp-btn--primary">
            + Crear nueva Liga
          </Link>
        </div>
      </div>

      <h2 style={{ margin: '0 0 0.75rem', fontSize: '1.1rem', fontWeight: 700 }}>
        Mis Ligas en esta Competencia
      </h2>

      {!myLeagues && <StatusMessage kind="loading" message="Cargando tus Ligas..." />}

      {myLeagues && myLeagues.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">🏆</span>
          <p className="pp-empty__text">
            Todavía no participás en ninguna Liga de esta Competencia.
          </p>
          <div className="pp-empty__actions">
            <Link to={`/leagues/new?competitionId=${competition.id}`} className="pp-btn pp-btn--primary">
              + Crear Liga
            </Link>
          </div>
        </div>
      )}

      {myLeagues && myLeagues.length > 0 && (
        <div className="pp-grid">
          {myLeagues.map((l) => (
            <div key={l.id} className="pp-league-card">
              <h3 className="pp-league-card__name">
                {l.name}
                {l.isCreator && <span style={{ fontWeight: 400, fontSize: '0.8rem', color: 'var(--color-text-muted)' }}> — creador</span>}
              </h3>
              <div className="pp-league-card__meta">
                <span>📋 {LEAGUE_SCOPE_LABELS[l.scopeType]}
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
