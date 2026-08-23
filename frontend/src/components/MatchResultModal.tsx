import { useState, type FocusEvent, type FormEvent, type MouseEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { Match } from '../api/types'

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

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})

    try {
      const updated = await api.put<Match>(`/matches/${match.id}/result`, {
        homeGoals,
        awayGoals,
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
