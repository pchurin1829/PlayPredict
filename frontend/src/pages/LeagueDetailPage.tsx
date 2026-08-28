import { useEffect, useRef, useState, useCallback, type KeyboardEvent } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { LEAGUE_SCOPE_LABELS, type LeagueDetail, type LeagueParticipantInfo, type RankingEntry, type MatchWithPrediction } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import ComingSoonBadge from '../components/player/ComingSoonBadge'
import ConfirmModal from '../components/ConfirmModal'
import TeamBadge from '../components/player/TeamBadge'
import QuickPreferredPlayerPicker from '../components/player/QuickPreferredPlayerPicker'
import { useAuth } from '../auth/AuthContext'
import './PlayerPages.css'

type Tab = 'resumen' | 'pronosticos' | 'resultados' | 'ranking' | 'premios' | 'participantes'

interface RowState {
  homeInput: string
  awayInput: string
  savedHome: string
  savedAway: string
  preferredPlayerId: string
  savedPreferredPlayerId: string
  hasPrediction: boolean
  saving: boolean
  error: string | null
  savedMessage: string | null
}

function sanitizeDigits(value: string): string {
  return value.replace(/\D/g, '')
}

function buildInitialRow(match: MatchWithPrediction): RowState {
  const home = match.myPrediction ? String(match.myPrediction.predictedHomeScore) : ''
  const away = match.myPrediction ? String(match.myPrediction.predictedAwayScore) : ''
  const preferredPlayerId = match.myPrediction?.preferredPlayerId
    ? String(match.myPrediction.preferredPlayerId)
    : !match.myPrediction && match.quickPreferredPlayers.length === 1 ? String(match.quickPreferredPlayers[0].id) : ''
  return {
    homeInput: home,
    awayInput: away,
    savedHome: home,
    savedAway: away,
    preferredPlayerId,
    savedPreferredPlayerId: preferredPlayerId,
    hasPrediction: !!match.myPrediction,
    saving: false,
    error: null,
    savedMessage: null,
  }
}

function isDirty(row: RowState): boolean {
  return row.hasPrediction && (
    row.homeInput !== row.savedHome
    || row.awayInput !== row.savedAway
    || row.preferredPlayerId !== row.savedPreferredPlayerId
  )
}

function hasBothScores(row: RowState): boolean {
  return row.homeInput !== '' && row.awayInput !== ''
}

function hasNoScores(row: RowState): boolean {
  return row.homeInput === '' && row.awayInput === ''
}

function predictionAction(row: RowState): { label: string; kind: 'none' | 'save' | 'delete'; disabled: boolean } {
  if (row.saving) return { label: 'Guardando...', kind: 'none', disabled: true }
  if (!row.hasPrediction && hasNoScores(row)) return { label: '¡Pronosticá!', kind: 'none', disabled: true }
  if (!row.hasPrediction && !hasBothScores(row)) return { label: 'Completá ambos resultados', kind: 'none', disabled: true }
  if (!row.hasPrediction) return { label: 'Guardar pronóstico', kind: 'save', disabled: false }
  if (!isDirty(row)) return { label: 'Pronosticado', kind: 'none', disabled: true }
  if (hasNoScores(row)) return { label: 'Eliminar pronóstico', kind: 'delete', disabled: false }
  if (!hasBothScores(row)) return { label: 'Completá ambos resultados', kind: 'none', disabled: true }
  return { label: 'Guardar cambios', kind: 'save', disabled: false }
}

interface CalendarDateParts {
  day: string
  month: string
  year: string
  key: string
}

const argentinaDateFormatter = new Intl.DateTimeFormat('es-AR', {
  timeZone: 'America/Argentina/Buenos_Aires',
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

function calendarDateParts(value: string): CalendarDateParts {
  const parts = argentinaDateFormatter.formatToParts(new Date(value))
  const day = parts.find((part) => part.type === 'day')?.value ?? ''
  const month = parts.find((part) => part.type === 'month')?.value ?? ''
  const year = parts.find((part) => part.type === 'year')?.value ?? ''
  return { day, month, year, key: `${year}-${month}-${day}` }
}

function formatRoundDateRange(roundMatches: MatchWithPrediction[]): string {
  const dates = roundMatches
    .map((match) => calendarDateParts(match.startsAtUtc))
    .sort((a, b) => a.key.localeCompare(b.key))

  if (dates.length === 0) return ''
  const min = dates[0]
  const max = dates[dates.length - 1]
  if (min.key === max.key) return `${min.day}/${min.month}/${min.year}`
  if (min.year === max.year && min.month === max.month) return `${min.day}–${max.day}/${max.month}/${max.year}`
  if (min.year === max.year) return `${min.day}/${min.month}–${max.day}/${max.month}/${max.year}`
  return `${min.day}/${min.month}/${min.year}–${max.day}/${max.month}/${max.year}`
}

export default function LeagueDetailPage() {
  const { leagueId } = useParams()
  const [searchParams] = useSearchParams()
  const { user } = useAuth()
  const requestedTab = searchParams.get('tab')
  const requestedRoundId = Number(searchParams.get('round')) || null

  const [league, setLeague] = useState<LeagueDetail | null>(null)
  const [participants, setParticipants] = useState<LeagueParticipantInfo[] | null>(null)
  const [ranking, setRanking] = useState<RankingEntry[] | null>(null)
  const [rankingRoundId, setRankingRoundId] = useState<number | null>(null)
  const [matches, setMatches] = useState<MatchWithPrediction[] | null>(null)
  const [rows, setRows] = useState<Record<number, RowState>>({})
  const [error, setError] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState<Tab>(requestedTab === 'pronosticos' ? 'pronosticos' : 'resumen')
  const [roundFilter, setRoundFilter] = useState<number | null>(null)
  const [expandedPredictionRounds, setExpandedPredictionRounds] = useState<Set<number>>(new Set())
  const [copiedCode, setCopiedCode] = useState(false)
  const [deleteTarget, setDeleteTarget] = useState<MatchWithPrediction | null>(null)
  const [actionFocusMatchId, setActionFocusMatchId] = useState<number | null>(null)
  const cardRefs = useRef<Record<number, HTMLDivElement | null>>({})

  useEffect(() => {
    if (actionFocusMatchId == null) return
    const action = cardRefs.current[actionFocusMatchId]?.querySelector<HTMLButtonElement>('[data-prediction-action]')
    if (action && !action.disabled) action.focus()
    setActionFocusMatchId(null)
  }, [actionFocusMatchId, rows])

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
    const rankingUrl = rankingRoundId === null
      ? `/rankings/leagues/${leagueId}`
      : `/rankings/leagues/${leagueId}/rounds/${rankingRoundId}`
    api.get<RankingEntry[]>(rankingUrl)
      .then((data) => { if (!cancelled) setRanking(data) })
      .catch(() => { if (!cancelled) setRanking([]) })
    return () => { cancelled = true }
  }, [activeTab, leagueId, rankingRoundId])

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
        if (activeTab === 'pronosticos') {
          const roundsNeedingAction = new Set(
            ms.filter((m) => m.canPredict && !m.myPrediction).map((m) => m.roundId),
          )
          const openRounds = new Set(ms.filter((m) => m.canPredict).map((m) => m.roundId))
          const requestedRoundExists = requestedRoundId != null && ms.some((match) => match.roundId === requestedRoundId)
          setExpandedPredictionRounds(
            requestedRoundExists
              ? new Set([requestedRoundId])
              : roundsNeedingAction.size > 0 ? roundsNeedingAction : openRounds,
          )
          if (requestedRoundExists) setRoundFilter(requestedRoundId)
        }
      })
      .catch(() => { if (!cancelled) setMatches([]) })
    return () => { cancelled = true }
  }, [activeTab, leagueId, requestedRoundId])

  function updateRow(matchId: number, patch: Partial<RowState>) {
    setRows((prev) => ({ ...prev, [matchId]: { ...prev[matchId], ...patch } }))
  }

  function handlePredictionEnter(event: KeyboardEvent<HTMLInputElement>, matchId: number) {
    if (event.key !== 'Enter') return

    event.preventDefault()
    if (rows[matchId]?.hasPrediction) {
      setActionFocusMatchId(matchId)
      return
    }

    const card = cardRefs.current[matchId]
    if (!card) return
    const inputs = Array.from(card.querySelectorAll<HTMLInputElement>('[data-prediction-score]'))
    const currentIndex = inputs.indexOf(event.currentTarget)
    const nextInput = inputs[currentIndex + 1]

    if (nextInput) {
      nextInput.focus()
      nextInput.select()
      return
    }

    const preferredPlayerInput = card.querySelector<HTMLInputElement>('[data-preferred-player-input]')
    if (preferredPlayerInput) {
      preferredPlayerInput.focus()
      return
    }

    card.querySelector<HTMLButtonElement>('[data-prediction-action]')?.focus()
  }

  function advanceToNextMatch(currentMatchId: number) {
    if (!matches) return
    const pending = matches.filter((m) => m.canPredict)
    const currentIndex = pending.findIndex((m) => m.id === currentMatchId)
    const nextMatch = pending[currentIndex + 1]
    const firstInput = nextMatch
      ? cardRefs.current[nextMatch.id]?.querySelector<HTMLInputElement>('[data-prediction-score]')
      : null
    if (firstInput) {
      setTimeout(() => {
        firstInput.focus()
        firstInput.select()
      }, 300)
    }
  }

  async function savePrediction(match: MatchWithPrediction) {
    const row = rows[match.id]
    if (!row) return
    const action = predictionAction(row)
    if (action.kind !== 'save' || action.disabled) return
    const homeScore = Number(row.homeInput)
    const awayScore = Number(row.awayInput)
    if (row.homeInput.trim() === '' || row.awayInput.trim() === '' || !Number.isInteger(homeScore) || !Number.isInteger(awayScore) || homeScore < 0 || awayScore < 0) {
      updateRow(match.id, { error: 'Ingresá un resultado válido (0 o mayor).', savedMessage: null })
      return
    }
    updateRow(match.id, { saving: true, error: null, savedMessage: null })
    try {
      const isUpdate = row.hasPrediction
      const updated = isUpdate
        ? await api.put<MatchWithPrediction['myPrediction']>(`/predictions/${match.myPrediction!.id}`, { leagueId: Number(leagueId), predictedHomeScore: homeScore, predictedAwayScore: awayScore, preferredPlayerId: row.preferredPlayerId ? Number(row.preferredPlayerId) : null, updatePreferredPlayer: match.preferredPlayerEnabled })
        : await api.post<MatchWithPrediction['myPrediction']>('/predictions', { leagueId: Number(leagueId), matchId: match.id, predictedHomeScore: homeScore, predictedAwayScore: awayScore, preferredPlayerId: row.preferredPlayerId ? Number(row.preferredPlayerId) : null, updatePreferredPlayer: match.preferredPlayerEnabled })
      setMatches((prev) => prev ? prev.map((m) => m.id === match.id ? { ...m, myPrediction: updated } : m) : prev)
      updateRow(match.id, {
        saving: false,
        savedHome: String(homeScore),
        savedAway: String(awayScore),
        savedPreferredPlayerId: row.preferredPlayerId,
        hasPrediction: true,
        savedMessage: isUpdate ? 'Pronóstico actualizado correctamente.' : 'Pronóstico guardado correctamente.',
      })
      setTimeout(() => updateRow(match.id, { savedMessage: null }), 4000)
      if (!isUpdate) advanceToNextMatch(match.id)
    } catch (err) {
      updateRow(match.id, { saving: false, error: err instanceof ApiError ? err.message : 'Ocurrió un error inesperado al guardar.' })
    }
  }

  async function deletePrediction() {
    const match = deleteTarget
    if (!match?.myPrediction) return
    setDeleteTarget(null)
    updateRow(match.id, { saving: true, error: null, savedMessage: null })
    try {
      await api.del<void>(`/predictions/${match.myPrediction.id}?leagueId=${leagueId}`)
      setMatches((prev) => prev ? prev.map((m) => m.id === match.id ? { ...m, myPrediction: null } : m) : prev)
      updateRow(match.id, {
        homeInput: '', awayInput: '', savedHome: '', savedAway: '', preferredPlayerId: '', savedPreferredPlayerId: '', hasPrediction: false,
        saving: false, savedMessage: 'Pronóstico eliminado correctamente.',
      })
      setTimeout(() => updateRow(match.id, { savedMessage: null }), 4000)
    } catch (err) {
      updateRow(match.id, { saving: false, error: err instanceof ApiError ? err.message : 'Ocurrió un error inesperado al eliminar.' })
    }
  }

  const handleCopyCode = useCallback(() => {
    const inviteCode = league?.inviteCode
    if (!inviteCode) return
    navigator.clipboard.writeText(inviteCode).then(() => {
      setCopiedCode(true)
      setTimeout(() => setCopiedCode(false), 2000)
    }).catch(() => {
      const fallback = document.createElement('textarea')
      fallback.value = inviteCode
      fallback.style.position = 'fixed'
      fallback.style.opacity = '0'
      document.body.appendChild(fallback)
      fallback.select()
      document.execCommand('copy')
      document.body.removeChild(fallback)
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
  const finishedMatches = matches ? matches.filter((m) => m.status === 'Finished') : []

  const roundOptions = league?.rounds ? [...league.rounds].sort((a, b) => a.order - b.order) : []

  function filterByRound(ms: MatchWithPrediction[]): MatchWithPrediction[] {
    if (roundFilter === null) return ms
    return ms.filter((m) => m.roundId === roundFilter)
  }

  function togglePredictionRound(roundId: number) {
    setExpandedPredictionRounds((previous) => {
      const next = new Set(previous)
      if (next.has(roundId)) next.delete(roundId)
      else next.add(roundId)
      return next
    })
  }

  // ── RENDER ──────────────────────────────────────────────────

  return (
    <div className={`pp-league-workspace pp-league-workspace--${league.leagueType === 'Official' ? 'official' : 'private'}`}>
      <Link to="/leagues" className="pp-back">← Mis Ligas</Link>

      <div className="pp-workspace__header">
        <h1 className="pp-workspace__title">{league.name}</h1>
        {league.description && <p className="pp-workspace__subtitle">{league.description}</p>}
        <div className="pp-workspace__meta">
          <span className="pp-workspace__meta-item">⚽ {league.competitionName} · {league.editionName}</span>
          <span className="pp-workspace__meta-item">
            📋 {LEAGUE_SCOPE_LABELS[league.scopeType]}
            {league.scopeType === 'RoundRange' && league.roundFromName && league.roundToName && (
              <> ({league.roundFromName} → {league.roundToName})</>
            )}
          </span>
          <span className="pp-workspace__meta-item">👥 {league.participantsCount} participantes</span>
          <span className="pp-workspace__meta-item">{league.isActive ? '🟢 Activa' : '🟡 Suspendida'}</span>
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
              {league.leagueType === 'Private' && league.sourceLeagueName && (
                <span className="pp-info-card__meta-item">🏆 Derivada de {league.sourceLeagueName}</span>
              )}
              <span className="pp-info-card__meta-item">⚽ Referencia deportiva: {league.competitionName} · {league.editionName}</span>
              <span className="pp-info-card__meta-item">
                📋 Alcance: {league.leagueType === 'Private' && league.usesFullSourceScope && league.sourceLeagueName
                  ? `Toda ${league.sourceLeagueName}`
                  : league.scopeType === 'RoundRange' && league.roundFromName && league.roundToName
                    ? `${league.roundFromName} → ${league.roundToName}`
                    : LEAGUE_SCOPE_LABELS[league.scopeType]}
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
          {!league.isActive && (
            <StatusMessage kind="info" message="Esta Liga está suspendida. Podés consultar los pronósticos, pero no cargar ni modificar nuevos resultados hasta que se reactive." />
          )}
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
                const visibleRounds = roundOptions.filter((round) =>
                  roundFilter === null || round.id === roundFilter
                )

                return visibleRounds.map((round) => {
                  const roundMatches = matches
                    .filter((match) => match.roundId === round.id)
                    .sort((a, b) => new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime())
                  if (roundMatches.length === 0) return null

                  const isExpanded = expandedPredictionRounds.has(round.id)
                  const hasOpenMatches = roundMatches.some((match) => match.canPredict)
                  const needsAction = roundMatches.some((match) => match.canPredict && !rows[match.id]?.hasPrediction)
                  const roundState = !hasOpenMatches
                    ? 'Cerrada'
                    : needsAction
                      ? 'ABIERTA'
                      : 'Pronosticada'

                  return (
                    <section key={round.id} className="pp-prediction-round">
                      <button
                        type="button"
                        className={`pp-prediction-round__toggle ${isExpanded ? 'pp-prediction-round__toggle--expanded' : ''}`}
                        aria-expanded={isExpanded}
                        aria-controls={`prediction-round-${round.id}`}
                        onClick={() => togglePredictionRound(round.id)}
                      >
                        <span className="pp-prediction-round__title">{round.name}</span>
                        <span className={`pp-prediction-round__status pp-prediction-round__status--${roundState.toLowerCase()}`}>
                          {roundState}
                        </span>
                        <span className="pp-prediction-round__action">{isExpanded ? 'Ocultar' : 'Ver'}</span>
                      </button>

                      {isExpanded && (
                        <div id={`prediction-round-${round.id}`} className="pp-matches pp-prediction-round__content">
                          {roundMatches.map((m) => {
                            if (!m.canPredict) {
                              return (
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
                              )
                            }

                          const row = rows[m.id]
                          if (!row) return null
                          const startsAt = new Date(m.startsAtUtc)
                          const action = predictionAction(row)
                          const saved = row.hasPrediction && !isDirty(row)
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
                                  <input type="text" inputMode="numeric" pattern="[0-9]*" placeholder="-" aria-label={`Goles ${m.participantHome}`} value={row.homeInput} onFocus={(e) => e.currentTarget.select()} onChange={(e) => updateRow(m.id, { homeInput: sanitizeDigits(e.target.value), savedMessage: null, error: null })} onKeyDown={(e) => handlePredictionEnter(e, m.id)} data-prediction-score className="pp-match-card__input" />
                                  <span className="pp-match-card__separator">-</span>
                                  <input type="text" inputMode="numeric" pattern="[0-9]*" placeholder="-" aria-label={`Goles ${m.participantAway}`} value={row.awayInput} onFocus={(e) => e.currentTarget.select()} onChange={(e) => updateRow(m.id, { awayInput: sanitizeDigits(e.target.value), savedMessage: null, error: null })} onKeyDown={(e) => handlePredictionEnter(e, m.id)} data-prediction-score className="pp-match-card__input" />
                                </div>
                                {m.preferredPlayerEnabled && (
                                  <label className="pp-match-card__preferred">
                                    <span className="pp-match-card__preferred-label">Jugador Preferido <small>(opcional)</small></span>
                                    {m.homePlayers.length > 0 || m.awayPlayers.length > 0 ? (
                                      <QuickPreferredPlayerPicker homeTeam={m.participantHome} awayTeam={m.participantAway} homePlayers={m.homePlayers} awayPlayers={m.awayPlayers} quickPlayers={m.quickPreferredPlayers} value={row.preferredPlayerId} ariaLabel={`Jugador Preferido para ${m.participantHome} vs ${m.participantAway}`} onChange={value => updateRow(m.id, { preferredPlayerId: value, savedMessage: null, error: null })} onSelectionComplete={() => setActionFocusMatchId(m.id)} />
                                    ) : (
                                      <span className="pp-match-card__preferred-empty">No hay jugadores disponibles para las posiciones habilitadas.</span>
                                    )}
                                  </label>
                                )}
                                <button
                                  type="button"
                                  className={saved ? 'pp-btn pp-btn--saved' : action.kind === 'delete' ? 'pp-btn pp-btn--danger' : 'pp-btn pp-btn--primary'}
                                  style={{ fontSize: '0.85rem', padding: '0.4rem 1.25rem' }}
                                  disabled={action.disabled}
                                  onClick={() => action.kind === 'delete' ? setDeleteTarget(m) : savePrediction(m)}
                                  data-prediction-action
                                >
                                  {action.label}
                                </button>
                                {row.hasPrediction && !saved && action.kind !== 'delete' && <span className="pp-match-card__hint">Podés modificar tu pronóstico hasta el cierre del partido.</span>}
                              </div>
                              {row.error && <div className="pp-match-card__error">{row.error}</div>}
                              {row.savedMessage && <div className="pp-match-card__saved">{row.savedMessage}</div>}
                            </div>
                          )
                          })}
                        </div>
                      )}
                    </section>
                  )
                })
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
                  const roundMatches = matches.filter((match) => match.roundId === ms[0].roundId)
                  const dateRange = formatRoundDateRange(roundMatches)
                  elements.push(
                    <div key={`r-${roundName}`}>
                      <h3 className="pp-round-heading">{roundName}{dateRange ? ` · ${dateRange}` : ''}</h3>
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
                                    <span className={`pp-match-card__result-points ${(m.myPrediction.points ?? 0) > 0 ? 'pp-match-card__result-points--positive' : ''}`}>{m.predictionEligible ? `${m.myPrediction.points ?? 0} pts` : 'No elegible en esta Liga'}</span>
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
          <div className="pp-edition-select">
            <span className="pp-edition-select__label">Alcance del ranking:</span>
            <select className="pp-edition-select__select" value={rankingRoundId ?? ''} onChange={(event) => setRankingRoundId(event.target.value ? Number(event.target.value) : null)}>
              <option value="">General de la Liga</option>
              {roundOptions.map((round) => <option key={round.id} value={round.id}>{round.name}</option>)}
            </select>
          </div>
          {!ranking && <StatusMessage kind="loading" message="Cargando ranking..." />}
          {ranking && ranking.length === 0 && (
            <div className="pp-empty">
              <span className="pp-empty__icon">📊</span>
              <p className="pp-empty__text">Todavía no hay pronósticos evaluados en esta Liga.<br />Cuando se carguen resultados oficiales, vas a ver las posiciones acá.</p>
            </div>
          )}
          {ranking && ranking.length > 0 && (
            <div className="pp-ranking">
              <div className="pp-ranking__header"><h2>{rankingRoundId === null ? 'Ranking de la Liga' : `Ranking — ${getRoundName(rankingRoundId)}`}</h2></div>
              <table>
                <thead><tr><th>#</th><th>Jugador</th><th>Puntos</th><th>Exactos</th><th>Correctos</th><th>Evaluados</th></tr></thead>
                <tbody>
                  {ranking.map((r) => {
                    const isMe = user && r.userId === user.id
                    return (
                      <tr key={r.userId} className={isMe ? 'pp-ranking__me' : ''}>
                        <td><span className={`pp-ranking__pos ${r.position <= 3 ? `pp-ranking__pos--${r.position}` : ''}`}>{r.position}°</span></td>
                        <td>{r.firstName} {r.lastName}{!r.isActiveParticipant && <span className="pp-ranking__inactive-badge">Retirado</span>}{isMe && <span className="pp-ranking__me-badge">(Vos)</span>}</td>
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

      <ConfirmModal
        open={deleteTarget !== null}
        title="Eliminar pronóstico"
        message="¿Querés eliminar este pronóstico? Se borrará para este partido en todas las Ligas que lo utilizan."
        confirmLabel="Eliminar"
        cancelLabel="Cancelar"
        onConfirm={deletePrediction}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}
