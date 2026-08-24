import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Competition, Edition, Round, LeagueSummary } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { useCompanySettings } from '../company/CompanySettingsContext'
import './PlayerPages.css'

interface ExploreItem {
  competition: Competition
  activeEdition: Edition | null
  roundsCount: number
  officialLeagues: LeagueSummary[]
}

export default function ExploreCompetitionsPage() {
  const { company } = useCompanySettings()
  const companyName = company.shortName || 'PlayPredict'
  const [items, setItems] = useState<ExploreItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [joiningId, setJoiningId] = useState<number | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  function editionLabel(name: string): string {
    return name.match(/\b(?:19|20)\d{2}\b/)?.[0] ?? name
  }

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
        setError(leaguesResult.reason?.message ?? `No se pudieron cargar las competencias ${companyName}.`)
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

  async function handleJoinOfficial(leagueId: number, leagueName: string) {
    setJoiningId(leagueId)
    setMessage(null)
    try {
      await api.post<LeagueSummary>(`/leagues/${leagueId}/join`, {})
      setMessage(`Te uniste correctamente a ${leagueName} (Oficial).`)
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
        <h1>Explorar Competencias</h1>
        <p className="pp-header__subtitle">
          Participá en las competencias {companyName} o creá tu propia Liga con amigos usando los partidos de una competencia de referencia.
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
          {items.flatMap(({ competition, activeEdition, roundsCount, officialLeagues }) => {
            const sourceDetails = (
              <>
                <div className="pp-comp-card__details">
                  <span>🏅 {competition.sport}</span>
                  {roundsCount > 0 && <span>📅 {roundsCount} fecha{roundsCount !== 1 ? 's' : ''}</span>}
                </div>
                <Link to={`/leagues/new?competitionId=${competition.id}`} className="pp-comp-card__action pp-comp-card__action--secondary">
                  + Crear Liga con amigos
                </Link>
              </>
            )

            if (officialLeagues.length === 0) {
              return [(
                <div key={`competition-${competition.id}`} className="pp-comp-card">
                  <h3 className="pp-comp-card__name">{competition.name}</h3>
                  <span className="pp-comp-card__edition">{activeEdition?.name ?? 'Sin edición activa'}</span>
                  <p className="pp-comp-card__source-note">Todavía no tiene una competencia {companyName} activa.</p>
                  <div className="pp-comp-card__actions">{sourceDetails}</div>
                </div>
              )]
            }

            return officialLeagues.map((league) => (
              <div key={league.id} className="pp-comp-card pp-comp-card--official-league">
                <div className="pp-comp-card__title-row">
                  <h3 className="pp-comp-card__name">{league.name}</h3>
                  <span className="pp-league-card__badge pp-league-card__badge--official">OFICIAL</span>
                </div>
                <span className="pp-comp-card__edition">{competition.name} · {editionLabel(league.editionName)}</span>
                <div className="pp-comp-card__actions">
                  <div className={`pp-comp-card__official-state ${league.isParticipant ? 'pp-comp-card__official-state--joined' : 'pp-comp-card__official-state--available'}`}>
                    <strong>{league.isParticipant ? '✓ Estás participando' : 'Todavía no participás'}</strong>
                    {league.isParticipant ? (
                      <Link to={`/leagues/${league.id}`} className="pp-comp-card__action pp-comp-card__action--view">Ver</Link>
                    ) : (
                      <button type="button" className="pp-comp-card__action" disabled={joiningId === league.id} onClick={() => handleJoinOfficial(league.id, league.name)}>
                        {joiningId === league.id ? 'Uniéndose...' : 'Participar'}
                      </button>
                    )}
                  </div>
                  {sourceDetails}
                </div>
              </div>
            ))
          })}
        </div>
      )}
    </div>
  )
}
