import { useEffect, useRef, useState, type FocusEvent, type FormEvent, type KeyboardEvent, type MouseEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { Match, TeamPlayer } from '../api/types'
import ConfirmModal from './ConfirmModal'

interface MatchResultModalProps {
  match: Match
  onClose: () => void
  onSaved: (updated: Match) => void
}

interface ScorerRow {
  teamPlayerId: number
  goals: number
  confirmed: boolean
}

interface ScorerRequirements {
  showScorerSection: boolean
  requiresScorerDetail: boolean
}

export default function MatchResultModal({ match, onClose, onSaved }: MatchResultModalProps) {
  const [homeGoals, setHomeGoals] = useState(match.homeGoals ?? 0)
  const [awayGoals, setAwayGoals] = useState(match.awayGoals ?? 0)
  const [saving, setSaving] = useState(false)
  const [blockingError, setBlockingError] = useState<string | null>(null)
  const [players, setPlayers] = useState<TeamPlayer[]>([])
  const [scorerRequirements, setScorerRequirements] = useState<ScorerRequirements | null>(null)
  const [scorers, setScorers] = useState<ScorerRow[]>(match.scorers
    .filter(s => (s.teamId === match.homeTeamId && (match.homeGoals ?? 0) > 0) || (s.teamId === match.awayTeamId && (match.awayGoals ?? 0) > 0))
    .map(s => ({ teamPlayerId: s.teamPlayerId, goals: s.goals, confirmed: true })))
  const homeGoalsRef = useRef<HTMLInputElement>(null)
  const awayGoalsRef = useRef<HTMLInputElement>(null)
  const scorerSelectRefs = useRef<Array<HTMLSelectElement | null>>([])
  const scorerGoalsRefs = useRef<Array<HTMLInputElement | null>>([])
  const addScorerRef = useRef<HTMLButtonElement>(null)
  const submitRef = useRef<HTMLButtonElement>(null)

  const eligiblePlayers = players.filter(player =>
    (player.teamId === match.homeTeamId && homeGoals > 0)
    || (player.teamId === match.awayTeamId && awayGoals > 0),
  )
  const assignedGoals = scorers.reduce((total, scorer) => total + scorer.goals, 0)
  const canAddScorer = eligiblePlayers.length > 0 && assignedGoals < homeGoals + awayGoals
  const showScorerSection = scorerRequirements?.showScorerSection ?? players.length > 0
  const requiresScorerDetail = scorerRequirements?.requiresScorerDetail === true

  useEffect(() => {
    setSaving(false)
    setBlockingError(null)
    const frame = window.requestAnimationFrame(() => {
      homeGoalsRef.current?.focus()
      homeGoalsRef.current?.select()
    })
    return () => window.cancelAnimationFrame(frame)
  }, [match.id])

  useEffect(() => {
    Promise.all([
      api.get<TeamPlayer[]>(`/teams/${match.homeTeamId}/players`),
      api.get<TeamPlayer[]>(`/teams/${match.awayTeamId}/players`),
    ]).then(([home, away]) => {
      setPlayers([...home, ...away].filter(player => player.active))
    }).catch(() => {
      setPlayers([])
    })

    api.get<ScorerRequirements>(`/matches/${match.id}/result-requirements`)
      .then(setScorerRequirements)
      .catch(() => setScorerRequirements(null))
  }, [match.awayTeamId, match.homeTeamId, match.id])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (scorers.some(row => !row.confirmed)) {
      setBlockingError('Confirmá cada asignación de goleador antes de confirmar el resultado.')
      return
    }
    if (requiresScorerDetail && homeGoals + awayGoals > 0) {
      const homePlayerIds = new Set(players.filter(player => player.teamId === match.homeTeamId).map(player => player.id))
      const awayPlayerIds = new Set(players.filter(player => player.teamId === match.awayTeamId).map(player => player.id))
      const assignedHomeGoals = scorers.filter(row => homePlayerIds.has(row.teamPlayerId)).reduce((total, row) => total + row.goals, 0)
      const assignedAwayGoals = scorers.filter(row => awayPlayerIds.has(row.teamPlayerId)).reduce((total, row) => total + row.goals, 0)
      if (assignedHomeGoals !== homeGoals || assignedAwayGoals !== awayGoals) {
        setBlockingError('Para calcular los puntos de Jugador Preferido debés completar los goleadores del partido.')
        return
      }
    }
    setSaving(true)
    setBlockingError(null)

    try {
      const updated = await api.put<Match>(`/matches/${match.id}/result`, {
        homeGoals,
        awayGoals,
        scorers: scorers.filter(s => s.teamPlayerId > 0 && s.goals > 0),
      })
      onSaved(updated)
    } catch (err) {
      if (err instanceof ApiError) {
        const messages = Object.values(err.fieldErrors).flat()
        setBlockingError(messages.length > 0 ? messages.join('\n') : err.message)
      } else {
        setBlockingError('Ocurrió un error inesperado al guardar el resultado.')
      }
    } finally {
      setSaving(false)
    }
  }

  function selectValue(event: FocusEvent<HTMLInputElement> | MouseEvent<HTMLInputElement>) {
    event.currentTarget.select()
  }

  function changeScore(side: 'home' | 'away', value: number) {
    const goals = Math.min(99, Math.max(0, value))
    if (side === 'home') setHomeGoals(goals)
    else setAwayGoals(goals)
    if (goals === 0) {
      const teamId = side === 'home' ? match.homeTeamId : match.awayTeamId
      const teamPlayerIds = new Set(players.filter(player => player.teamId === teamId).map(player => player.id))
      setScorers(current => current.filter(scorer => !teamPlayerIds.has(scorer.teamPlayerId)))
    }
  }

  function handleFormKeyDown(event: KeyboardEvent<HTMLFormElement>) {
    if (event.key !== 'Enter') return
    const target = event.target as HTMLElement
    const side = target.dataset.resultScore
    if (side !== 'home' && side !== 'away') return
    event.preventDefault()
    if (side === 'home') {
      awayGoalsRef.current?.focus()
      awayGoalsRef.current?.select()
      return
    }
    const firstPendingRow = scorers.findIndex(row => !row.confirmed)
    if (firstPendingRow >= 0) scorerSelectRefs.current[firstPendingRow]?.focus()
    else if (showScorerSection && canAddScorer) addScorerRef.current?.focus()
    else submitRef.current?.focus()
  }

  function addScorer() {
    const nextIndex = scorers.length
    setScorers(current => [...current, { teamPlayerId: 0, goals: 1, confirmed: false }])
    setTimeout(() => scorerSelectRefs.current[nextIndex]?.focus())
  }

  function confirmScorer(index: number) {
    const row = scorers[index]
    if (!row || row.teamPlayerId <= 0 || row.goals <= 0) {
      setBlockingError('Seleccioná un jugador y una cantidad de goles válida para confirmar la asignación.')
      return
    }
    setScorers(current => current.map((item, itemIndex) => itemIndex === index ? { ...item, confirmed: true } : item))
    setTimeout(() => {
      if (assignedGoals < homeGoals + awayGoals) addScorerRef.current?.focus()
      else submitRef.current?.focus()
    })
  }

  function team(name: string, logoUrl: string | null, reverse = false) {
    const mark = logoUrl ? <img src={logoUrl} alt="" /> : <span aria-hidden="true">{name.slice(0, 2).toUpperCase()}</span>
    return <div className={`result-team ${reverse ? 'result-team--reverse' : ''}`}>{mark}<strong>{name}</strong></div>
  }

  return (
    <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose() }}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()} onMouseDown={(e) => e.stopPropagation()} onMouseUp={(e) => e.stopPropagation()}>
        <h2>Resultado Oficial</h2>
        {match.status === 'Finished' && (
          <p className="admin-help">Al confirmar se recalcularán los puntos existentes con el nuevo resultado, sin acumular evaluaciones.</p>
        )}

        <form onSubmit={handleSubmit} onKeyDownCapture={handleFormKeyDown}>
          <div className="result-scoreboard">
            {team(match.participantHome, match.homeTeamLogoUrl)}
            <div className="result-scoreboard__score">
              <input
                ref={homeGoalsRef}
                id="homeGoals"
                aria-label={`Goles de ${match.participantHome}`}
                type="number"
                min={0}
                max={99}
                inputMode="numeric"
                value={homeGoals}
                onFocus={selectValue}
                onClick={selectValue}
                onChange={(e) => changeScore('home', Number(e.target.value))}
                data-result-score="home"
              />
              <strong>−</strong>
              <input ref={awayGoalsRef} id="awayGoals" aria-label={`Goles de ${match.participantAway}`} type="number" min={0} max={99} inputMode="numeric" value={awayGoals} onFocus={selectValue} onClick={selectValue} onChange={(e) => changeScore('away', Number(e.target.value))} data-result-score="away" />
            </div>
            {team(match.participantAway, match.awayTeamLogoUrl, true)}
          </div>
          {showScorerSection && <section className="result-scorers"><h3>Goleadores ({requiresScorerDetail ? 'obligatorio' : 'opcional'})</h3><p className="admin-help">Incluye jugadores activos de cualquier posición.</p>
            {scorers.map((row,index) => <div className={`result-scorer-row ${row.confirmed ? 'result-scorer-row--confirmed' : ''}`} key={index}>
              <select ref={element => { scorerSelectRefs.current[index] = element }} aria-label={`Goleador ${index + 1}`} value={row.teamPlayerId} onChange={event => { const teamPlayerId=Number(event.target.value); setScorers(current => current.map((item,itemIndex) => itemIndex===index ? {...item,teamPlayerId,confirmed:false} : item)); if(teamPlayerId > 0) setTimeout(()=>scorerGoalsRefs.current[index]?.focus()) }} onKeyDown={event=>{if(event.key==='Enter'){event.preventDefault();scorerGoalsRefs.current[index]?.focus();scorerGoalsRefs.current[index]?.select()}}}>
                <option value={0}>Seleccionar jugador</option>
                {homeGoals > 0 && <optgroup label={match.participantHome}>{eligiblePlayers.filter(player=>player.teamId===match.homeTeamId).map(player=><option value={player.id} key={player.id}>{player.displayName}</option>)}</optgroup>}
                {awayGoals > 0 && <optgroup label={match.participantAway}>{eligiblePlayers.filter(player=>player.teamId===match.awayTeamId).map(player=><option value={player.id} key={player.id}>{player.displayName}</option>)}</optgroup>}
              </select>
              <input ref={element => { scorerGoalsRefs.current[index] = element }} aria-label="Cantidad de goles" type="number" min={1} max={99} inputMode="numeric" value={row.goals} onFocus={selectValue} onChange={event=>setScorers(current=>current.map((item,itemIndex)=>itemIndex===index?{...item,goals:Math.max(1,Number(event.target.value)),confirmed:false}:item))} onKeyDown={event=>{if(event.key==='Enter'){event.preventDefault();confirmScorer(index)}}}/>
              <div className="result-scorer-row__actions">
                {!row.confirmed && <button type="button" className="btn btn-primary" onClick={()=>confirmScorer(index)}>Confirmar</button>}
                {row.confirmed && <span className="result-scorer-row__confirmed">Asignado</span>}
                <button type="button" className="btn btn-tertiary" onClick={()=>setScorers(current=>current.filter((_,itemIndex)=>itemIndex!==index))}>Quitar</button>
              </div>
            </div>)}
            <button ref={addScorerRef} type="button" className="btn btn-secondary" disabled={!canAddScorer} onClick={addScorer}>+ Agregar goleador</button>
            {!canAddScorer && homeGoals + awayGoals > 0 && <span className="form-field-hint">Ya se asignaron todos los goles cargados o no hay jugadores disponibles.</span>}
          </section>}

          <div className="form-actions">
            <button ref={submitRef} type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Guardando...' : 'Confirmar resultado'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={onClose}>
              Cancelar
            </button>
          </div>
        </form>
      </div>
      <ConfirmModal open={Boolean(blockingError)} title="No se puede confirmar el resultado" message={blockingError ?? ''} confirmLabel="OK" showCancel={false} onConfirm={() => setBlockingError(null)} onCancel={() => setBlockingError(null)} />
    </div>
  )
}
