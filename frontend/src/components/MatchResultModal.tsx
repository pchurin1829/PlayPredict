import { useEffect, useState, type FocusEvent, type FormEvent, type MouseEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { Match, TeamPlayer } from '../api/types'

interface MatchResultModalProps {
  match: Match
  onClose: () => void
  onSaved: (updated: Match) => void
}

export default function MatchResultModal({ match, onClose, onSaved }: MatchResultModalProps) {
  const [homeGoals, setHomeGoals] = useState(match.homeGoals ?? 0)
  const [awayGoals, setAwayGoals] = useState(match.awayGoals ?? 0)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [players, setPlayers] = useState<TeamPlayer[]>([])
  const [scorers, setScorers] = useState(match.scorers.map(s => ({ teamPlayerId: s.teamPlayerId, goals: s.goals })))

  useEffect(() => { Promise.all([api.get<TeamPlayer[]>(`/teams/${match.homeTeamId}/players`), api.get<TeamPlayer[]>(`/teams/${match.awayTeamId}/players`)]).then(([home, away]) => setPlayers([...home, ...away].filter(p => p.active))).catch(() => setPlayers([])) }, [match.homeTeamId, match.awayTeamId])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})

    try {
      const updated = await api.put<Match>(`/matches/${match.id}/result`, {
        homeGoals,
        awayGoals,
        scorers: scorers.filter(s => s.teamPlayerId > 0 && s.goals > 0),
      })
      onSaved(updated)
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.fieldErrors)
      } else {
        setError('Ocurrió un error inesperado al guardar el resultado.')
      }
    } finally {
      setSaving(false)
    }
  }

  function selectValue(event: FocusEvent<HTMLInputElement> | MouseEvent<HTMLInputElement>) {
    event.currentTarget.select()
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

        {error && <p className="form-field-error">{error}</p>}

        <form onSubmit={handleSubmit}>
          <div className="result-scoreboard">
            {team(match.participantHome, match.homeTeamLogoUrl)}
            <div className="result-scoreboard__score">
              <input
                id="homeGoals"
                aria-label={`Goles de ${match.participantHome}`}
                type="number"
                min={0}
                max={99}
                inputMode="numeric"
                value={homeGoals}
                onFocus={selectValue}
                onClick={selectValue}
                onChange={(e) => setHomeGoals(Math.min(99, Math.max(0, Number(e.target.value))))}
              />
              <strong>−</strong>
              <input id="awayGoals" aria-label={`Goles de ${match.participantAway}`} type="number" min={0} max={99} inputMode="numeric" value={awayGoals} onFocus={selectValue} onClick={selectValue} onChange={(e) => setAwayGoals(Math.min(99, Math.max(0, Number(e.target.value))))} />
            </div>
            {team(match.participantAway, match.awayTeamLogoUrl, true)}
          </div>
          <div className="result-field-errors">
              {fieldErrors.homeGoals && (
                <span className="form-field-error">{fieldErrors.homeGoals[0]}</span>
              )}
              {fieldErrors.awayGoals && (
                <span className="form-field-error">{fieldErrors.awayGoals[0]}</span>
              )}
          </div>

          {players.length > 0 && <section className="result-scorers"><h3>Goleadores <small>(opcional)</small></h3><p className="admin-help">Si no se informan, Jugador Preferido queda en 0 puntos hasta completar el detalle.</p>
            {scorers.map((row,index) => <div className="result-scorer-row" key={index}><select aria-label={`Goleador ${index + 1}`} value={row.teamPlayerId} onChange={e => setScorers(current => current.map((x,i) => i===index ? {...x,teamPlayerId:Number(e.target.value)} : x))}><option value={0}>Seleccionar jugador</option><optgroup label={match.participantHome}>{players.filter(p=>p.teamId===match.homeTeamId).map(p=><option value={p.id} key={p.id}>{p.displayName}</option>)}</optgroup><optgroup label={match.participantAway}>{players.filter(p=>p.teamId===match.awayTeamId).map(p=><option value={p.id} key={p.id}>{p.displayName}</option>)}</optgroup></select><input aria-label="Cantidad de goles" type="number" min={1} max={99} inputMode="numeric" value={row.goals} onFocus={selectValue} onChange={e=>setScorers(current=>current.map((x,i)=>i===index?{...x,goals:Math.max(1,Number(e.target.value))}:x))}/><button type="button" className="btn btn-tertiary" onClick={()=>setScorers(current=>current.filter((_,i)=>i!==index))}>Quitar</button></div>)}
            <button type="button" className="btn btn-secondary" onClick={()=>setScorers(current=>[...current,{teamPlayerId:0,goals:1}])}>+ Agregar goleador</button>
            {fieldErrors.scorers && <span className="form-field-error">{fieldErrors.scorers[0]}</span>}
          </section>}

          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Guardando...' : 'Confirmar resultado'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={onClose}>
              Cancelar
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
