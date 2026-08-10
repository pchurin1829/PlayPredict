import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { LeagueDetail, MatchWithPrediction } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import TeamBadge from '../components/player/TeamBadge'
import './PlayerPages.css'

interface RowState {
  homeInput: string
  awayInput: string
  saving: boolean
  error: string | null
  savedMessage: string | null
}

function sanitizeDigits(value: string): string {
  return value.replace(/\D/g, '')
}

function buildInitialRow(match: MatchWithPrediction): RowState {
  return {
    homeInput: match.myPrediction ? String(match.myPrediction.predictedHomeScore) : '',
    awayInput: match.myPrediction ? String(match.myPrediction.predictedAwayScore) : '',
    saving: false,
    error: null,
    savedMessage: null,
  }
}

export default function PredictionsMatchesPage() {
  const { leagueId } = useParams()

  const [league, setLeague] = useState<LeagueDetail | null>(null)
  const [matches, setMatches] = useState<MatchWithPrediction[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [rows, setRows] = useState<Record<number, RowState>>({})

  useEffect(() => {
    let cancelled = false

    Promise.all([
      api.get<LeagueDetail>(`/leagues/${leagueId}`),
      api.get<MatchWithPrediction[]>(`/leagues/${leagueId}/matches`),
    ])
      .then(([l, ms]) => {
        if (cancelled) return
        setLeague(l)
        setMatches(ms)
        const initialRows: Record<number, RowState> = {}
        ms.forEach((m) => {
          initialRows[m.id] = buildInitialRow(m)
        })
        setRows(initialRows)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar los partidos.')
      })

    return () => {
      cancelled = true
    }
  }, [leagueId])

  function updateRow(matchId: number, patch: Partial<RowState>) {
    setRows((prev) => ({ ...prev, [matchId]: { ...prev[matchId], ...patch } }))
  }

  async function savePrediction(match: MatchWithPrediction) {
    const row = rows[match.id]
    if (!row) return

    const homeScore = Number(row.homeInput)
    const awayScore = Number(row.awayInput)

    if (
      row.homeInput.trim() === '' ||
      row.awayInput.trim() === '' ||
      !Number.isInteger(homeScore) ||
      !Number.isInteger(awayScore) ||
      homeScore < 0 ||
      awayScore < 0
    ) {
      updateRow(match.id, { error: 'Ingresá un resultado válido (0 o mayor).', savedMessage: null })
      return
    }

    updateRow(match.id, { saving: true, error: null, savedMessage: null })

    try {
      const isUpdate = !!match.myPrediction
      const updated = isUpdate
        ? await api.put<MatchWithPrediction['myPrediction']>(`/predictions/${match.myPrediction!.id}`, {
            predictedHomeScore: homeScore,
            predictedAwayScore: awayScore,
          })
        : await api.post<MatchWithPrediction['myPrediction']>('/predictions', {
            leagueId: Number(leagueId),
            matchId: match.id,
            predictedHomeScore: homeScore,
            predictedAwayScore: awayScore,
          })

      setMatches((prev) => (prev ? prev.map((m) => (m.id === match.id ? { ...m, myPrediction: updated } : m)) : prev))
      updateRow(match.id, { saving: false, savedMessage: isUpdate ? 'Pronóstico actualizado correctamente.' : 'Pronóstico guardado correctamente.' })
      setTimeout(() => updateRow(match.id, { savedMessage: null }), 4000)
    } catch (err) {
      updateRow(match.id, {
        saving: false,
        error: err instanceof ApiError ? err.message : 'Ocurrió un error inesperado al guardar.',
      })
    }
  }

  if (error) {
    return (
      <div>
        <Link to={league ? `/leagues/${league.id}` : '/leagues'} className="pp-back">
          ← {league?.name ?? 'Mis Ligas'}
        </Link>
        <StatusMessage kind="error" message={error} />
      </div>
    )
  }

  if (!league || !matches) {
    return <StatusMessage kind="loading" message="Cargando partidos..." />
  }

  const pendingMatches = matches.filter((m) => m.canPredict)
  const finishedMatches = matches.filter((m) => m.status === 'Finished')
  const otherMatches = matches.filter((m) => !m.canPredict && m.status !== 'Finished')

  return (
    <div>
      <Link to={`/leagues/${league.id}`} className="pp-back">← {league.name}</Link>

      <div className="pp-header">
        <div>
          <h1>Pronósticos</h1>
          <p className="pp-header__subtitle">{league.name} — {league.competitionName}</p>
        </div>
      </div>

      {matches.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">⚽</span>
          <p className="pp-empty__text">Esta Liga todavía no tiene partidos.</p>
        </div>
      )}

      {pendingMatches.length > 0 && (
        <div style={{ marginBottom: '2rem' }}>
          <h2 className="pdash__section-title" style={{ marginBottom: '0.75rem' }}>
            ⚽ Pronosticá
          </h2>
          <div className="pp-matches">
            {pendingMatches.map((m) => {
              const row = rows[m.id]
              if (!row) return null
              const startsAt = new Date(m.startsAtUtc)
              const hasPrediction = !!m.myPrediction

              return (
                <div key={m.id} className={`pp-match-card ${hasPrediction ? 'pp-match-card--saved' : 'pp-match-card--pending'}`}>
                  <div className="pp-match-card__teams">
                    <div className="pp-match-card__team">
                      <TeamBadge name={m.participantHome} size={40} />
                      <span className="pp-match-card__team-name">{m.participantHome}</span>
                    </div>
                    <span className="pp-match-card__vs">VS</span>
                    <div className="pp-match-card__team">
                      <TeamBadge name={m.participantAway} size={40} />
                      <span className="pp-match-card__team-name">{m.participantAway}</span>
                    </div>
                  </div>

                  <div className="pp-match-card__info">
                    <span>{startsAt.toLocaleDateString([], { day: 'numeric', month: 'short' })}</span>
                    <span>{startsAt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                  </div>

                  <div className="pp-match-card__prediction">
                    <span className="pp-match-card__prediction-label">
                      {hasPrediction ? 'TU PRONÓSTICO' : 'INGRESÁ TU PRONÓSTICO'}
                    </span>
                    <div className="pp-match-card__inputs">
                      <input
                        type="text"
                        inputMode="numeric"
                        pattern="[0-9]*"
                        placeholder="-"
                        aria-label={`Goles ${m.participantHome}`}
                        value={row.homeInput}
                        onChange={(e) => updateRow(m.id, { homeInput: sanitizeDigits(e.target.value), savedMessage: null, error: null })}
                        className="pp-match-card__input"
                      />
                      <span className="pp-match-card__separator">-</span>
                      <input
                        type="text"
                        inputMode="numeric"
                        pattern="[0-9]*"
                        placeholder="-"
                        aria-label={`Goles ${m.participantAway}`}
                        value={row.awayInput}
                        onChange={(e) => updateRow(m.id, { awayInput: sanitizeDigits(e.target.value), savedMessage: null, error: null })}
                        className="pp-match-card__input"
                      />
                    </div>
                    <button
                      type="button"
                      className="pp-btn pp-btn--primary"
                      style={{ fontSize: '0.85rem', padding: '0.4rem 1.25rem' }}
                      disabled={row.saving}
                      onClick={() => savePrediction(m)}
                    >
                      {row.saving ? 'Guardando...' : hasPrediction ? 'Guardar cambios' : '¡Pronosticá!'}
                    </button>
                    {hasPrediction && (
                      <span className="pp-match-card__hint">
                        Podés modificar tu pronóstico hasta el cierre del partido.
                      </span>
                    )}
                  </div>

                  {row.error && <div className="pp-match-card__error">{row.error}</div>}
                  {row.savedMessage && <div className="pp-match-card__saved">{row.savedMessage}</div>}
                </div>
              )
            })}
          </div>
        </div>
      )}

      {finishedMatches.length > 0 && (
        <div style={{ marginBottom: '2rem' }}>
          <h2 className="pdash__section-title" style={{ marginBottom: '0.75rem' }}>
            ✅ Resultados
          </h2>
          <div className="pp-matches">
            {finishedMatches.map((m) => (
              <div key={m.id} className="pp-match-card pp-match-card--finished">
                <div className="pp-match-card__teams">
                  <div className="pp-match-card__team">
                    <TeamBadge name={m.participantHome} size={36} />
                    <span className="pp-match-card__team-name">{m.participantHome}</span>
                  </div>
                  <span className="pp-match-card__vs">VS</span>
                  <div className="pp-match-card__team">
                    <TeamBadge name={m.participantAway} size={36} />
                    <span className="pp-match-card__team-name">{m.participantAway}</span>
                  </div>
                </div>

                <div className="pp-match-card__result-section">
                  <div className="pp-match-card__result-row">
                    <span className="pp-match-card__result-label">Resultado</span>
                    <span className="pp-match-card__result-score">
                      {m.homeGoals} - {m.awayGoals}
                    </span>
                  </div>
                  {m.myPrediction ? (
                    <>
                      <div className="pp-match-card__result-row">
                        <span className="pp-match-card__result-label">Mi pronóstico</span>
                        <span className="pp-match-card__result-value">
                          {m.myPrediction.predictedHomeScore} - {m.myPrediction.predictedAwayScore}
                        </span>
                      </div>
                      <div className="pp-match-card__result-row">
                        <span className="pp-match-card__result-label">Puntos</span>
                        <span className={`pp-match-card__result-points ${(m.myPrediction.points ?? 0) > 0 ? 'pp-match-card__result-points--positive' : ''}`}>
                          {m.myPrediction.points ?? 0} pts
                        </span>
                      </div>
                      {m.myPrediction.evaluationLabel && (
                        <div className="pp-match-card__result-row">
                          <span className="pp-match-card__result-label">Motivo</span>
                          <span className="pp-match-card__result-value">{m.myPrediction.evaluationLabel}</span>
                        </div>
                      )}
                    </>
                  ) : (
                    <div className="pp-match-card__result-row">
                      <span className="pp-match-card__result-label">Mi pronóstico</span>
                      <span className="pp-match-card__no-prediction">Sin pronóstico</span>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {otherMatches.length > 0 && (
        <div>
          <h2 className="pdash__section-title" style={{ marginBottom: '0.75rem' }}>
            🔒 Cerrados
          </h2>
          <div className="pp-matches">
            {otherMatches.map((m) => (
              <div key={m.id} className="pp-match-card pp-match-card--closed">
                <div className="pp-match-card__teams">
                  <div className="pp-match-card__team">
                    <TeamBadge name={m.participantHome} size={32} />
                    <span className="pp-match-card__team-name">{m.participantHome}</span>
                  </div>
                  <span className="pp-match-card__vs">VS</span>
                  <div className="pp-match-card__team">
                    <TeamBadge name={m.participantAway} size={32} />
                    <span className="pp-match-card__team-name">{m.participantAway}</span>
                  </div>
                </div>
                <div className="pp-match-card__closed-text">
                  {m.status === 'Cancelled' ? 'Cancelado' : 'Pronóstico cerrado'}
                  {m.myPrediction && (
                    <span style={{ marginLeft: '0.5rem' }}>
                      ({m.myPrediction.predictedHomeScore} - {m.myPrediction.predictedAwayScore})
                    </span>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
