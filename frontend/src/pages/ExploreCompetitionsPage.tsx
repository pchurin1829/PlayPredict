import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Competition, Edition, Round, LeagueSummary } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

interface ExploreItem {
  competition: Competition
  activeEdition: Edition | null
  roundsCount: number
  officialLeagues: LeagueSummary[]
}

export default function ExploreCompetitionsPage() {
  const [items, setItems] = useState<ExploreItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [joiningId, setJoiningId] = useState<number | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    async function load() {
      let competitions: Competition[] | null = null
      let officialLeagues: LeagueSummary[] | null = null

      const [compResult, leaguesResult] = await Promise.allSettled([
        api.get<Competition[]>('/competitions'),
        api.get<LeagueSummary[]>('/leagues/officials'),
      ])

      if (cancelled) return

      if (compResult.status === 'fulfilled') {
        competitions = compResult.value
      } else {
        setError(compResult.reason?.message ?? 'No se pudieron cargar las Competencias.')
        return
      }

      if (leaguesResult.status === 'fulfilled') {
        officialLeagues = leaguesResult.value
      } else {
        setError(leaguesResult.reason?.message ?? 'No se pudieron cargar las Ligas Oficiales.')
        return
      }

      const active = competitions.filter((c) => c.isActive)

      const enriched = await Promise.all(
        active.map(async (competition) => {
          const editions = await api.get<Edition[]>(`/competitions/${competition.id}/editions`)
          const activeEdition = editions.find((e) => e.status === 'Active') ?? null

          let roundsCount = 0
          if (activeEdition) {
            const rounds = await api.get<Round[]>(`/editions/${activeEdition.id}/rounds`)
            roundsCount = rounds.length
          }

          const compLeagues = (officialLeagues ?? []).filter(
            (l) => l.competitionId === competition.id
          )

          return { competition, activeEdition, roundsCount, officialLeagues: compLeagues }
        }),
      )

      if (!cancelled) setItems(enriched)
    }

    load()
    return () => { cancelled = true }
  }, [])

  async function handleJoinOfficial(leagueId: number, competitionName: string) {
    setJoiningId(leagueId)
    setMessage(null)
    try {
      await api.post<LeagueSummary>(`/leagues/${leagueId}/join`, {})
      setMessage(`Te uniste correctamente a ${competitionName} (Oficial).`)
      // Refresh to update isParticipant flags
      const updated = await api.get<LeagueSummary[]>('/leagues/officials')
      setItems((prev) => {
        if (!prev) return prev
        return prev.map((item) => ({
          ...item,
          officialLeagues: updated.filter((l) => l.competitionId === item.competition.id),
        }))
      })
    } catch (err) {
      setMessage(err instanceof ApiError ? err.message : 'Ocurrió un error al unirse.')
    } finally {
      setJoiningId(null)
      setTimeout(() => setMessage(null), 4000)
    }
  }

  return (
    <div>
      <div className="pp-header">
        <h1>Explorar Competencias Oficiales</h1>
        <p className="pp-header__subtitle">
          Participá en las Ligas Oficiales de PlayPredict o creá tu propia Liga con amigos usando los partidos de una competencia oficial.
        </p>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!items && !error && <StatusMessage kind="loading" message="Cargando Competencias..." />}
      {message && <StatusMessage kind={message.includes('error') || message.includes('incorrecto') ? 'error' : 'success'} message={message} />}

      {items && items.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">⚽</span>
          <p className="pp-empty__text">No hay competencias activas todavía.</p>
        </div>
      )}

      {items && items.length > 0 && (
        <div className="pp-grid">
          {items.map(({ competition, activeEdition, roundsCount, officialLeagues }) => {
            const joinedOfficial = officialLeagues.find((l) => l.isParticipant)
            const availableOfficials = officialLeagues.filter((l) => !l.isParticipant)

            return (
              <div key={competition.id} className="pp-comp-card">
                <h3 className="pp-comp-card__name">🏆 {competition.name}</h3>
                {activeEdition ? (
                  <span className="pp-comp-card__edition">📍 {activeEdition.name}</span>
                ) : (
                  <span style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                    Sin edición activa
                  </span>
                )}
                <div className="pp-comp-card__details">
                  <span>🏅 {competition.sport}</span>
                  {roundsCount > 0 && <span>📅 {roundsCount} fecha{roundsCount !== 1 ? 's' : ''}</span>}
                </div>
                <div className="pp-comp-card__actions">
                  {joinedOfficial ? (
                    <div className="pp-comp-card__official-state pp-comp-card__official-state--joined">
                      <strong>✓ Estás participando</strong>
                      <span>Entrá para ver tu Liga Oficial.</span>
                      <Link
                        to={`/leagues/${joinedOfficial.id}`}
                        className="pp-comp-card__action pp-comp-card__action--view"
                      >
                        Ver
                      </Link>
                    </div>
                  ) : availableOfficials.length > 0 ? (
                    <div className="pp-comp-card__official-state pp-comp-card__official-state--available">
                      <strong>Todavía no participás</strong>
                      <span>Tocá Participar para sumarte a la Liga Oficial.</span>
                      <button
                        type="button"
                        className="pp-comp-card__action"
                        disabled={joiningId === availableOfficials[0].id}
                        onClick={() => handleJoinOfficial(availableOfficials[0].id, competition.name)}
                      >
                        {joiningId === availableOfficials[0].id ? 'Uniéndose...' : 'Participar'}
                      </button>
                    </div>
                  ) : null}
                  <Link
                    to={`/leagues/new?competitionId=${competition.id}`}
                    className="pp-comp-card__action pp-comp-card__action--secondary"
                  >
                    + Crear Liga con amigos
                  </Link>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
