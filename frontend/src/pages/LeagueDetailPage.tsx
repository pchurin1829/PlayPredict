import { useEffect, useState, useCallback } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { LEAGUE_SCOPE_LABELS, type LeagueDetail, type LeagueParticipantInfo, type RankingEntry, type MatchWithPrediction } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import ComingSoonBadge from '../components/player/ComingSoonBadge'
import TeamBadge from '../components/player/TeamBadge'
import { useAuth } from '../auth/AuthContext'
import './PlayerPages.css'

type Tab = 'resumen' | 'pronosticos' | 'resultados' | 'ranking' | 'premios' | 'participantes'

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

export default function LeagueDetailPage() {
  const { leagueId } = useParams()
  const { user } = useAuth()

  const [league, setLeague] = useState<LeagueDetail | null>(null)
  const [participants, setParticipants] = useState<LeagueParticipantInfo[] | null>(null)
  const [ranking, setRanking] = useState<RankingEntry[] | null>(null)
  const [matches, setMatches] = useState<MatchWithPrediction[] | null>(null)
  const [rows, setRows] = useState<Record<number, RowState>>({})
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<Tab>('resumen')
  const [roundFilter, setRoundFilter] = useState<number | null>(null)
  const [copiedCode, setCopiedCode] = useState(false)

  useEffect(() => {
    let cancelled = false
    setError(null)

    Promise.all([
      api.get<LeagueDetail>(`/leagues/${leagueId}`),
      api.get<LeagueParticipantInfo[]>(`/leagues/${leagueId}/participants`),
    ])
      .then(([l, p]) => {
        if (cancelled) return
        setLeague(l)
        setParticipants(p)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudo cargar la Liga.')
      })

    return () => { cancelled = true }
  }, [leagueId])

  useEffect(() => {
    if (activeTab !== 'ranking' || !leagueId) return
    let cancelled = false
    setRanking(null)
    api.get<RankingEntry[]>(`/rankings/leagues/${leagueId}`)
      .then((data) => { if (!cancelled) setRanking(data) })
      .catch(() => { if (!cancelled) setRanking([]) })
    return () => { cancelled = true }
  }, [activeTab, leagueId])

  useEffect(() => {
    if ((activeTab !== 'pronosticos' && activeTab !== 'resultados') || !leagueId) return
    let cancelled = false
    setMatches(null)
    api.get<MatchWithPrediction[]>(`/leagues/${leagueId}/matches`)
      .then((ms) => {
        if (cancelled) return
        setMatches(ms)
        const initialRows: Record<number, RowState> = {}
        ms.forEach((m) => { initialRows[m.id] = buildInitialRow(m) })
        setRows(initialRows)
      })
      .catch(() => { if (!cancelled) setMatches([]) })
    return () => { cancelled = true }
  }, [activeTab, leagueId])

  function updateRow(matchId: number, patch: Partial<RowState>) {
    setRows((prev) => ({ ...prev, [matchId]: { ...prev[matchId], ...patch } }))
  }

  async function savePrediction(match: MatchWithPrediction) {
    const row = rows[match.id]
    if (!row) return
    const homeScore = Number(row.homeInput)
    const awayScore = Number(row.awayInput)
    if (row.homeInput.trim() === '' || row.awayInput.trim() === '' || !Number.isInteger(homeScore) || !Number.isInteger(awayScore) || homeScore < 0 || awayScore < 0) {
      updateRow(match.id, { error: 'Ingresá un resultado válido (0 o mayor).', savedMessage: null })
      return
    }
    updateRow(match.id, { saving: true, error: null, savedMessage: null })
    try {
      const isUpdate = !!match.myPrediction
      const updated = isUpdate
        ? await api.put<MatchWithPrediction['myPrediction']>(`/predictions/${match.myPrediction!.id}`, { predictedHomeScore: homeScore, predictedAwayScore: awayScore })
        : await api.post<MatchWithPrediction['myPrediction']>('/predictions', { leagueId: Number(leagueId), matchId: match.id, predictedHomeScore: homeScore, predictedAwayScore: awayScore })
      setMatches((prev) => prev ? prev.map((m) => m.id === match.id ? { ...m, myPrediction: updated } : m) : prev)
      updateRow(match.id, { saving: false, savedMessage: isUpdate ? 'Pronóstico actualizado correctamente.' : 'Pronóstico guardado correctamente.' })
      setTimeout(() => updateRow(match.id, { savedMessage: null }), 4000)
    } catch (err) {
      updateRow(match.id, { saving: false, error: err instanceof ApiError ? err.message : 'Ocurrió un error inesperado al guardar.' })
    }
  }

  const handleCopyCode = useCallback(() => {
    if (!league?.inviteCode) return
    navigator.clipboard.writeText(league.inviteCode).then(() => {
      setCopiedCode(true)
      setTimeout(() => setCopiedCode(false), 2000)
    })
  }, [league?.inviteCode])

  // Group matches by round
  function getRoundName(roundId: number): string {
    if (!league?.rounds) return `Fecha ${roundId}`
    const r = league.rounds.find((rr) => rr.id === roundId)
    return r ? r.name : `Fecha ${roundId}`
  }

  function getRoundOrder(roundId: number): number {
    if (!league?.rounds) return roundId
    const r = league.rounds.find((rr) => rr.id === roundId)
    return r ? r.order : roundId
  }

  function groupByRound(ms: MatchWithPrediction[]): Map<string, MatchWithPrediction[]> {
    const map = new Map<string, MatchWithPrediction[]>()
    const sorted = [...ms].sort((a, b) => getRoundOrder(a.roundId) - getRoundOrder(b.roundId) || new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime())
    for (const m of sorted) {
      const key = getRoundName(m.roundId)
      if (!map.has(key)) map.set(key, [])
      map.get(key)!.push(m)
    }
    return map
  }

  if (error) {
    return (
      <div>
        <Link to="/leagues" className="pp-back">← Mis Ligas</Link>
        <StatusMessage kind="error" message={error} />
      </div>
    )
  }

  if (!league) {
    return <StatusMessage kind="loading" message="Cargando Liga..." />
  }

  const tabs: { key: Tab; label: string; soon?: boolean }[] = [
    { key: 'resumen', label: '📋 Resumen' },
    { key: 'pronosticos', label: '⚽ Pronósticos' },
    { key: 'resultados', label: '✅ Resultados' },
    { key: 'ranking', label: '🏆 Ranking' },
    { key: 'premios', label: '🎁 Premios', soon: true },
    { key: 'participantes', label: '👥 Participantes' },
  ]

  // Match grouping for Pronósticos and Resultados tabs
  const pendingMatches = matches ? matches.filter((m) => m.canPredict) : []
  const finishedMatches = matches ? matches.filter((m) => m.status === 'Finished') : []
  const closedMatches = matches ? matches.filter((m) => !m.canPredict && m.status !== 'Finished') : []

  const roundOptions = league?.rounds ? [...league.rounds].sort((a, b) => a.order - b.order) : []

  function filterByRound(ms: MatchWithPrediction[]): MatchWithPrediction[] {
    if (roundFilter === null) return ms
    return ms.filter((m) => m.roundId === roundFilter)
  }

  // ── RENDER ──────────────────────────────────────────────────

  return (
    <div>
      <Link to="/leagues" className="pp-back">← Mis Ligas</Link>

      <div className="pp-workspace__header">
        <h1 className="pp-workspace__title">{league.name}</h1>
        {league.description && <p className="pp-workspace__subtitle">{league.description}</p>}
        <div className="pp-workspace__meta">
          <span className="pp-workspace__meta-item">⚽ {league.competitionName}</span>
          <span className="pp-workspace__meta-item">
            📋 {LEAGUE_SCOPE_LABELS[league.scopeType]}
            {league.scopeType === 'RoundRange' && league.roundFromName && league.roundToName && (
              <> ({league.roundFromName} → {league.roundToName})</>
            )}
          </span>
          <span className="pp-workspace__meta-item">👥 {league.participantsCount} participantes</span>
          <span className="pp-workspace__meta-item">{league.isActive ? '🟢 Activa' : '🔴 Inactiva'}</span>
        </div>
      </div>

      <div className="pp-tabs">
        {tabs.map((tab) =>
          tab.soon ? (
            <button key={tab.key} type="button" className="pp-tab pp-tab--soon" disabled>
              {tab.label} <ComingSoonBadge />
            </button>
          ) : (
            <button key={tab.key} type="button" className={`pp-tab ${activeTab === tab.key ? 'pp-tab--active' : ''}`} onClick={() => setActiveTab(tab.key)}>
              {tab.label}
            </button>
          ),
        )}
      </div>

      {/* ── RESUMEN ────────────────────────────────────────── */}
      {activeTab === 'resumen' && (
        <div>
          <div className="pp-info-card">
            <h2 className="pp-info-card__title">Tu Liga</h2>
            <div className="pp-info-card__meta">
              <span className="pp-info-card__meta-item">⚽ Competencia: {league.competitionName}</span>
              <span className="pp-info-card__meta-item">
                📋 Alcance: {LEAGUE_SCOPE_LABELS[league.scopeType]}
                {league.scopeType === 'RoundRange' && league.roundFromName && league.roundToName && (
                  <> ({league.roundFromName} → {league.roundToName})</>
                )}
              </span>
              <span className="pp-info-card__meta-item">{league.isCreator ? '👑 Creador' : '🙋 Participante'}</span>
            </div>
            {league.isCreator && league.inviteCode && (
              <div className="pp-workspace__invite-section" style={{ marginTop: '1rem' }}>
                <div className="pp-workspace__invite-title">Invitá jugadores a tu Liga</div>
                <div className="pp-workspace__invite-code-row">
                  <span>Código: </span>
                  <strong>{league.inviteCode}</strong>
                  <button type="button" className="pp-btn pp-btn--secondary pp-btn--sm" onClick={handleCopyCode}>
                    {copiedCode ? '✓ Copiado' : '📋 Copiar código'}
                  </button>
                </div>
                <p className="pp-workspace__invite-hint">Compartí este código para que tus amigos se unan.</p>
              </div>
            )}
            <div className="pp-info-card__cta">
              <button type="button" className="pp-btn pp-btn--primary" onClick={() => setActiveTab('pronosticos')}>
                ⚽ Pronosticar ahora
              </button>
            </div>
          </div>

          {participants && (
            <div className="pp-info-card">
              <h3 style={{ margin: '0 0 0.75rem', fontSize: '1rem', fontWeight: 700 }}>
                👥 Participantes ({participants.length})
              </h3>
              <div className="pp-participants">
                {participants.map((p) => (
                  <div key={p.userId} className="pp-participant">
                    <div className="pp-participant__avatar">{p.firstName[0]}{p.lastName[0]}</div>
                    <div>
                      <div className="pp-participant__name">{p.firstName} {p.lastName}</div>
                      {p.isCreator && <div className="pp-participant__badge">Creador</div>}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* ── PRONÓSTICOS ────────────────────────────────────── */}
      {activeTab === 'pronosticos' && (
        <div>
          {!matches && <StatusMessage kind="loading" message="Cargando partidos..." />}

          {matches && matches.length === 0 && (
            <div className="pp-empty">
              <span className="pp-empty__icon">⚽</span>
              <p className="pp-empty__text">Esta Liga todavía no tiene partidos.</p>
            </div>
          )}

          {matches && matches.length > 0 && (
            <>
              {/* Round filter */}
              {roundOptions.length > 3 && (
                <div className="pp-round-filter">
                  <select className="pp-round-filter__select" value={roundFilter ?? ''} onChange={(e) => setRoundFilter(e.target.value ? Number(e.target.value) : null)}>
                    <option value="">Todas las Fechas</option>
                    {roundOptions.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
                  </select>
                </div>
              )}
              {roundOptions.length > 1 && roundOptions.length <= 3 && (
                <div className="pp-round-chips">
                  <button type="button" className={`pp-round-chip ${roundFilter === null ? 'pp-round-chip--active' : ''}`} onClick={() => setRoundFilter(null)}>Todas</button>
                  {roundOptions.map((r) => <button key={r.id} type="button" className={`pp-round-chip ${roundFilter === r.id ? 'pp-round-chip--active' : ''}`} onClick={() => setRoundFilter(r.id)}>{r.name}</button>)}
                </div>
              )}

              {(() => {
                const filtered = filterByRound(pendingMatches)
                if (filtered.length === 0) {
                  return (
                    <div className="pp-empty">
                      <span className="pp-empty__icon">✅</span>
                      <p className="pp-empty__text">
                        {pendingMatches.length === 0
                          ? 'No hay partidos para pronosticar en este momento.'
                          : 'No hay partidos para pronosticar en esta Fecha.'}
                      </p>
                    </div>
                  )
                }
                const grouped = groupByRound(filtered)
                const elements: React.ReactElement[] = []
                grouped.forEach((ms, roundName) => {
                  elements.push(
                    <div key={`p-${roundName}`}>
                      <h3 className="pp-round-heading">{roundName}</h3>
                      <div className="pp-matches">
                        {ms.map((m) => {
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
                                  <input type="text" inputMode="numeric" pattern="[0-9]*" placeholder="-" aria-label={`Goles ${m.participantHome}`} value={row.homeInput} onChange={(e) => updateRow(m.id, { homeInput: sanitizeDigits(e.target.value), savedMessage: null, error: null })} className="pp-match-card__input" />
                                  <span className="pp-match-card__separator">-</span>
                                  <input type="text" inputMode="numeric" pattern="[0-9]*" placeholder="-" aria-label={`Goles ${m.participantAway}`} value={row.awayInput} onChange={(e) => updateRow(m.id, { awayInput: sanitizeDigits(e.target.value), savedMessage: null, error: null })} className="pp-match-card__input" />
                                </div>
                                <button type="button" className="pp-btn pp-btn--primary" style={{ fontSize: '0.85rem', padding: '0.4rem 1.25rem' }} disabled={row.saving} onClick={() => savePrediction(m)}>
                                  {row.saving ? 'Guardando...' : hasPrediction ? 'Guardar cambios' : '¡Pronosticá!'}
                                </button>
                                {hasPrediction && <span className="pp-match-card__hint">Podés modificar tu pronóstico hasta el cierre del partido.</span>}
                              </div>
                              {row.error && <div className="pp-match-card__error">{row.error}</div>}
                              {row.savedMessage && <div className="pp-match-card__saved">{row.savedMessage}</div>}
                            </div>
                          )
                        })}
                      </div>
                    </div>
                  )
                })
                // Closed matches after pending
                const filteredClosed = filterByRound(closedMatches)
                if (filteredClosed.length > 0) {
                  const closedGrouped = groupByRound(filteredClosed)
                  closedGrouped.forEach((ms, roundName) => {
                    elements.push(
                      <div key={`c-${roundName}`}>
                        <h3 className="pp-round-heading">{roundName} — 🔒 Cerrados</h3>
                        <div className="pp-matches">
                          {ms.map((m) => (
                            <div key={m.id} className="pp-match-card pp-match-card--closed">
                              <div className="pp-match-card__teams">
                                <div className="pp-match-card__team"><TeamBadge name={m.participantHome} size={32} /><span className="pp-match-card__team-name">{m.participantHome}</span></div>
                                <span className="pp-match-card__vs">VS</span>
                                <div className="pp-match-card__team"><TeamBadge name={m.participantAway} size={32} /><span className="pp-match-card__team-name">{m.participantAway}</span></div>
                              </div>
                              <div className="pp-match-card__closed-text">
                                Pronóstico cerrado
                                {m.myPrediction && <span style={{ marginLeft: '0.5rem' }}>({m.myPrediction.predictedHomeScore} - {m.myPrediction.predictedAwayScore})</span>}
                              </div>
                            </div>
                          ))}
                        </div>
                      </div>
                    )
                  })
                }
                return elements
              })()}
            </>
          )}
        </div>
      )}

      {/* ── RESULTADOS ─────────────────────────────────────── */}
      {activeTab === 'resultados' && (
        <div>
          {!matches && <StatusMessage kind="loading" message="Cargando resultados..." />}

          {matches && finishedMatches.length === 0 && (
            <div className="pp-empty">
              <span className="pp-empty__icon">📊</span>
              <p className="pp-empty__text">Todavía no hay partidos finalizados en esta Liga.</p>
            </div>
          )}

          {matches && finishedMatches.length > 0 && (
            <>
              {roundOptions.length > 3 && (
                <div className="pp-round-filter">
                  <select className="pp-round-filter__select" value={roundFilter ?? ''} onChange={(e) => setRoundFilter(e.target.value ? Number(e.target.value) : null)}>
                    <option value="">Todas las Fechas</option>
                    {roundOptions.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
                  </select>
                </div>
              )}
              {roundOptions.length > 1 && roundOptions.length <= 3 && (
                <div className="pp-round-chips">
                  <button type="button" className={`pp-round-chip ${roundFilter === null ? 'pp-round-chip--active' : ''}`} onClick={() => setRoundFilter(null)}>Todas</button>
                  {roundOptions.map((r) => <button key={r.id} type="button" className={`pp-round-chip ${roundFilter === r.id ? 'pp-round-chip--active' : ''}`} onClick={() => setRoundFilter(r.id)}>{r.name}</button>)}
                </div>
              )}

              {(() => {
                const filtered = filterByRound(finishedMatches)
                const grouped = groupByRound(filtered)
                const elements: React.ReactElement[] = []
                grouped.forEach((ms, roundName) => {
                  elements.push(
                    <div key={`r-${roundName}`}>
                      <h3 className="pp-round-heading">{roundName}</h3>
                      <div className="pp-matches">
                        {ms.map((m) => (
                          <div key={m.id} className="pp-match-card pp-match-card--finished">
                            <div className="pp-match-card__teams">
                              <div className="pp-match-card__team"><TeamBadge name={m.participantHome} size={36} /><span className="pp-match-card__team-name">{m.participantHome}</span></div>
                              <span className="pp-match-card__vs">VS</span>
                              <div className="pp-match-card__team"><TeamBadge name={m.participantAway} size={36} /><span className="pp-match-card__team-name">{m.participantAway}</span></div>
                            </div>
                            <div className="pp-match-card__result-section">
                              <div className="pp-match-card__result-row">
                                <span className="pp-match-card__result-label">Resultado</span>
                                <span className="pp-match-card__result-score">{m.homeGoals} - {m.awayGoals}</span>
                              </div>
                              {m.myPrediction ? (
                                <>
                                  <div className="pp-match-card__result-row">
                                    <span className="pp-match-card__result-label">Mi pronóstico</span>
                                    <span className="pp-match-card__result-value">{m.myPrediction.predictedHomeScore} - {m.myPrediction.predictedAwayScore}</span>
                                  </div>
                                  <div className="pp-match-card__result-row">
                                    <span className="pp-match-card__result-label">Puntos</span>
                                    <span className={`pp-match-card__result-points ${(m.myPrediction.points ?? 0) > 0 ? 'pp-match-card__result-points--positive' : ''}`}>{m.myPrediction.points ?? 0} pts</span>
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
                  )
                })
                return elements
              })()}
            </>
          )}
        </div>
      )}

      {/* ── RANKING ────────────────────────────────────────── */}
      {activeTab === 'ranking' && (
        <div>
          {!ranking && <StatusMessage kind="loading" message="Cargando ranking..." />}
          {ranking && ranking.length === 0 && (
            <div className="pp-empty">
              <span className="pp-empty__icon">📊</span>
              <p className="pp-empty__text">Todavía no hay pronósticos evaluados en esta Liga.<br />Cuando se carguen resultados oficiales, vas a ver las posiciones acá.</p>
            </div>
          )}
          {ranking && ranking.length > 0 && (
            <div className="pp-ranking">
              <div className="pp-ranking__header"><h2>Ranking de la Liga</h2></div>
              <table>
                <thead><tr><th>#</th><th>Jugador</th><th>Puntos</th><th>Exactos</th><th>Correctos</th><th>Evaluados</th></tr></thead>
                <tbody>
                  {ranking.map((r) => {
                    const isMe = user && r.userId === user.id
                    return (
                      <tr key={r.userId} className={isMe ? 'pp-ranking__me' : ''}>
                        <td><span className={`pp-ranking__pos ${r.position <= 3 ? `pp-ranking__pos--${r.position}` : ''}`}>{r.position}°</span></td>
                        <td>{r.firstName} {r.lastName}{isMe && <span className="pp-ranking__me-badge">(Vos)</span>}</td>
                        <td className="pp-ranking__points">{r.points}</td>
                        <td>{r.exactCount}</td>
                        <td>{r.correctCount}</td>
                        <td>{r.evaluatedCount}</td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* ── PARTICIPANTES ──────────────────────────────────── */}
      {activeTab === 'participantes' && (
        <div>
          {!participants && <StatusMessage kind="loading" message="Cargando participantes..." />}
          {participants && participants.length === 0 && (
            <div className="pp-empty"><span className="pp-empty__icon">👥</span><p className="pp-empty__text">No hay participantes todavía.</p></div>
          )}
          {participants && participants.length > 0 && (
            <div className="pp-participants">
              {participants.map((p) => (
                <div key={p.userId} className="pp-participant">
                  <div className="pp-participant__avatar">{p.firstName[0]}{p.lastName[0]}</div>
                  <div>
                    <div className="pp-participant__name">{p.firstName} {p.lastName}</div>
                    {p.isCreator && <div className="pp-participant__badge">Creador</div>}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
