import { useState, type FormEvent } from 'react'
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

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal-card" onClick={(e) => e.stopPropagation()}>
        <h2>Resultado Oficial</h2>
        <p>
          {match.participantHome} vs {match.participantAway}
        </p>

        {error && <p className="form-field-error">{error}</p>}

        <form onSubmit={handleSubmit}>
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="homeGoals">Goles local</label>
              <input
                id="homeGoals"
                type="number"
                min={0}
                value={homeGoals}
                onChange={(e) => setHomeGoals(Number(e.target.value))}
              />
              {fieldErrors.homeGoals && (
                <span className="form-field-error">{fieldErrors.homeGoals[0]}</span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="awayGoals">Goles visitante</label>
              <input
                id="awayGoals"
                type="number"
                min={0}
                value={awayGoals}
                onChange={(e) => setAwayGoals(Number(e.target.value))}
              />
              {fieldErrors.awayGoals && (
                <span className="form-field-error">{fieldErrors.awayGoals[0]}</span>
              )}
            </div>
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
