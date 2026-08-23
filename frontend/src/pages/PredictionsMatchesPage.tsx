import { useEffect, useRef, useState, type KeyboardEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { LeagueDetail, MatchWithPrediction } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import ConfirmModal from '../components/ConfirmModal'
import TeamBadge from '../components/player/TeamBadge'
import './PlayerPages.css'

interface RowState {
  homeInput: string
  awayInput: string
  savedHome: string
  savedAway: string
  hasPrediction: boolean
  saving: boolean
  error: string | null
  savedMessage: string | null
}

function sanitizeDigits(value: string): string {
  return value.replace(/\D/g, '')
}

function isDirty(row: RowState): boolean {
  if (!row.hasPrediction) return false
  return row.homeInput !== row.savedHome || row.awayInput !== row.savedAway
}

function isReady(row: RowState): boolean {
  return row.homeInput !== '' && row.awayInput !== ''
}

function predictionAction(row: RowState): { label: string; kind: 'none' | 'save' | 'delete'; disabled: boolean } {
  const empty = row.homeInput === '' && row.awayInput === ''
  if (row.saving) return { label: 'Guardando...', kind: 'none', disabled: true }
  if (!row.hasPrediction && empty) return { label: '¡Pronosticá!', kind: 'none', disabled: true }
  if (!row.hasPrediction && !isReady(row)) return { label: 'Completá ambos resultados', kind: 'none', disabled: true }
  if (!row.hasPrediction) return { label: 'Guardar pronóstico', kind: 'save', disabled: false }
  if (!isDirty(row)) return { label: 'Pronosticado', kind: 'none', disabled: true }
  if (empty) return { label: 'Eliminar pronóstico', kind: 'delete', disabled: false }
  if (!isReady(row)) return { label: 'Completá ambos resultados', kind: 'none', disabled: true }
  return { label: 'Guardar cambios', kind: 'save', disabled: false }
}

function isSaved(row: RowState): boolean {
  return row.hasPrediction && !isDirty(row)
}

function buildInitialRow(match: MatchWithPrediction): RowState {
  const homeInput = match.myPrediction ? String(match.myPrediction.predictedHomeScore) : ''
  const awayInput = match.myPrediction ? String(match.myPrediction.predictedAwayScore) : ''
  return {
    homeInput,
    awayInput,
    savedHome: match.myPrediction ? String(match.myPrediction.predictedHomeScore) : '',
    savedAway: match.myPrediction ? String(match.myPrediction.predictedAwayScore) : '',
    hasPrediction: !!match.myPrediction,
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
  const [deleteTarget, setDeleteTarget] = useState<MatchWithPrediction | null>(null)
  const cardRefs = useRef<Record<number, HTMLDivElement | null>>({})

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

  function handleInputChange(
    matchId: number,
    field: 'homeInput' | 'awayInput',
    value: string,
  ) {
    const clean = sanitizeDigits(value)
    setRows((prev) => {
      const row = prev[matchId]
      if (!row) return prev
      const updated = { ...row, [field]: clean, savedMessage: null, error: null }
      return { ...prev, [matchId]: updated }
    })
  }

  function handlePredictionEnter(event: KeyboardEvent<HTMLInputElement>, matchId: number) {
    if (event.key !== 'Enter') return
    event.preventDefault()

    const card = cardRefs.current[matchId]
    if (!card) return

    const inputs = Array.from(card.querySelectorAll<HTMLInputElement>('[data-prediction-score]'))
    const currentIndex = inputs.indexOf(event.currentTarget)

    if (currentIndex === inputs.length - 1) {
      const btn = card.querySelector<HTMLButtonElement>('[data-prediction-action]')
      if (btn) {
        btn.focus()
      }
      return
    }

    const nextInput = inputs[currentIndex + 1]
    if (nextInput) {
      nextInput.focus()
      nextInput.select()
    }
  }

  function advanceToNextMatch(currentMatchId: number) {
    if (!matches) return
    const pending = matches.filter((m) => m.canPredict)
    const currentIndex = pending.findIndex((m) => m.id === currentMatchId)
    const nextMatch = pending[currentIndex + 1]
    if (nextMatch) {
      const nextCard = cardRefs.current[nextMatch.id]
      if (nextCard) {
        const firstInput = nextCard.querySelector<HTMLInputElement>('[data-prediction-score]')
        if (firstInput) {
          setTimeout(() => {
            firstInput.focus()
            firstInput.select()
          }, 300)
        }
      }
    }
  }

  async function savePrediction(match: MatchWithPrediction) {
    const row = rows[match.id]
    if (!row) return
    const action = predictionAction(row)
    if (action.kind !== 'save' || action.disabled) return

    const homeScore = Number(row.homeInput)
    const awayScore = Number(row.awayInput)

    if (
      row.homeInput === '' ||
      row.awayInput === '' ||
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
      const isUpdate = row.hasPrediction
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
      updateRow(match.id, {
        saving: false,
        savedHome: String(homeScore),
        savedAway: String(awayScore),
        hasPrediction: true,
        savedMessage: isUpdate ? 'Pronóstico actualizado correctamente.' : 'Pronóstico guardado correctamente.',
      })
      setTimeout(() => updateRow(match.id, { savedMessage: null }), 4000)
      advanceToNextMatch(match.id)
    } catch (err) {
      updateRow(match.id, {
        saving: false,
        error: err instanceof ApiError ? err.message : 'Ocurrió un error inesperado al guardar.',
      })
    }
  }

  async function deletePrediction() {
    const match = deleteTarget
    if (!match?.myPrediction) return
    setDeleteTarget(null)
    updateRow(match.id, { saving: true, error: null, savedMessage: null })
    try {
      await api.del<void>(`/predictions/${match.myPrediction.id}`)
      setMatches((prev) => prev ? prev.map((m) => m.id === match.id ? { ...m, myPrediction: null } : m) : prev)
      updateRow(match.id, {
        homeInput: '', awayInput: '', savedHome: '', savedAway: '', hasPrediction: false,
        saving: false, savedMessage: 'Pronóstico eliminado correctamente.',
      })
      setTimeout(() => updateRow(match.id, { savedMessage: null }), 4000)
    } catch (err) {
      updateRow(match.id, { saving: false, error: err instanceof ApiError ? err.message : 'Ocurrió un error inesperado al eliminar.' })
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
              const saved = isSaved(row)
              const action = predictionAction(row)
              const btnClass = saved
                ? 'pp-btn pp-btn--saved'
                : action.kind === 'delete' ? 'pp-btn pp-btn--danger' : 'pp-btn pp-btn--primary'

              return (
                <div
                  key={m.id}
                  ref={(el) => { cardRefs.current[m.id] = el }}
                  className={`pp-match-card ${row.hasPrediction ? 'pp-match-card--saved' : 'pp-match-card--pending'}`}
                >
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
                      {saved ? '✅ PRONOSTICADO' : 'INGRESÁ TU PRONÓSTICO'}
                    </span>
                    <div className="pp-match-card__inputs">
                      <input
                        type="text"
                        inputMode="numeric"
                        pattern="[0-9]*"
                        placeholder="-"
                        aria-label={`Goles ${m.participantHome}`}
                        value={row.homeInput}
                        onFocus={(e) => e.currentTarget.select()}
                        onChange={(e) => handleInputChange(m.id, 'homeInput', e.target.value)}
                        onKeyDown={(e) => handlePredictionEnter(e, m.id)}
                        data-prediction-score
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
                        onFocus={(e) => e.currentTarget.select()}
                        onChange={(e) => handleInputChange(m.id, 'awayInput', e.target.value)}
                        onKeyDown={(e) => handlePredictionEnter(e, m.id)}
                        data-prediction-score
                        className="pp-match-card__input"
                      />
                    </div>
                    <button
                      type="button"
                      className={btnClass}
                      style={{ fontSize: '0.85rem', padding: '0.4rem 1.25rem' }}
                      disabled={action.disabled}
                      onClick={() => action.kind === 'delete' ? setDeleteTarget(m) : savePrediction(m)}
                      data-prediction-action
                    >
                      {action.label}
                    </button>
                    {row.hasPrediction && !saved && (
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

      <ConfirmModal
        open={deleteTarget !== null}
        title="Eliminar pronóstico"
        message="¿Querés eliminar este pronóstico? Los valores guardados se borrarán definitivamente."
        confirmLabel="Eliminar"
        cancelLabel="Cancelar"
        onConfirm={deletePrediction}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}
