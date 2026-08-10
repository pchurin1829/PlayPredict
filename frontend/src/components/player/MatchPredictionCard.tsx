import { useState } from 'react'
import { api, ApiError } from '../../api/client'
import type { MatchWithPrediction } from '../../api/types'
import TeamBadge from './TeamBadge'
import './MatchPredictionCard.css'

interface MatchPredictionCardProps {
  match: MatchWithPrediction
  leagueId: number
  onPredictionUpdated: (match: MatchWithPrediction) => void
}

function sanitizeDigits(value: string): string {
  return value.replace(/\D/g, '')
}

export default function MatchPredictionCard({
  match,
  leagueId,
  onPredictionUpdated,
}: MatchPredictionCardProps) {
  const [homeInput, setHomeInput] = useState(
    match.myPrediction ? String(match.myPrediction.predictedHomeScore) : '',
  )
  const [awayInput, setAwayInput] = useState(
    match.myPrediction ? String(match.myPrediction.predictedAwayScore) : '',
  )
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [savedMessage, setSavedMessage] = useState<string | null>(null)

  const startsAt = new Date(match.startsAtUtc)
  const editable = match.canPredict
  const isFinished = match.status === 'Finished'
  const isCancelled = match.status === 'Cancelled'

  async function savePrediction() {
    const homeScore = Number(homeInput)
    const awayScore = Number(awayInput)

    if (
      homeInput.trim() === '' ||
      awayInput.trim() === '' ||
      !Number.isInteger(homeScore) ||
      !Number.isInteger(awayScore) ||
      homeScore < 0 ||
      awayScore < 0
    ) {
      setError('Ingresá un resultado válido')
      return
    }

    setSaving(true)
    setError(null)
    setSavedMessage(null)

    try {
      const updated = match.myPrediction
        ? await api.put<MatchWithPrediction['myPrediction']>(
            `/predictions/${match.myPrediction.id}`,
            { predictedHomeScore: homeScore, predictedAwayScore: awayScore },
          )
        : await api.post<MatchWithPrediction['myPrediction']>('/predictions', {
            leagueId,
            matchId: match.id,
            predictedHomeScore: homeScore,
            predictedAwayScore: awayScore,
          })

      onPredictionUpdated({ ...match, myPrediction: updated })
      setSavedMessage('¡Guardado!')
      setTimeout(() => setSavedMessage(null), 3000)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Error al guardar')
    } finally {
      setSaving(false)
    }
  }

  const statusClass = isFinished
    ? 'mpcard--finished'
    : isCancelled
      ? 'mpcard--cancelled'
      : match.myPrediction
        ? 'mpcard--saved'
        : 'mpcard--pending'

  return (
    <div className={`mpcard ${statusClass}`}>
      <div className="mpcard__teams">
        <div className="mpcard__team">
          <TeamBadge name={match.participantHome} size={36} />
          <span className="mpcard__team-name">{match.participantHome}</span>
        </div>
        <span className="mpcard__vs">VS</span>
        <div className="mpcard__team">
          <TeamBadge name={match.participantAway} size={36} />
          <span className="mpcard__team-name">{match.participantAway}</span>
        </div>
      </div>

      <div className="mpcard__info">
        <span className="mpcard__date">
          {startsAt.toLocaleDateString([], { day: 'numeric', month: 'short' })}
        </span>
        <span className="mpcard__time">
          {startsAt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </span>
      </div>

      <div className="mpcard__prediction">
        {editable ? (
          <>
            <span className="mpcard__prediction-label">TU PRONÓSTICO</span>
            <div className="mpcard__inputs">
              <input
                type="text"
                inputMode="numeric"
                pattern="[0-9]*"
                placeholder="-"
                aria-label={`Goles ${match.participantHome}`}
                value={homeInput}
                onChange={(e) => {
                  setHomeInput(sanitizeDigits(e.target.value))
                  setSavedMessage(null)
                  setError(null)
                }}
                className="mpcard__input"
              />
              <span className="mpcard__separator">-</span>
              <input
                type="text"
                inputMode="numeric"
                pattern="[0-9]*"
                placeholder="-"
                aria-label={`Goles ${match.participantAway}`}
                value={awayInput}
                onChange={(e) => {
                  setAwayInput(sanitizeDigits(e.target.value))
                  setSavedMessage(null)
                  setError(null)
                }}
                className="mpcard__input"
              />
            </div>
            <button
              type="button"
              className="mpcard__save-btn"
              disabled={saving}
              onClick={savePrediction}
            >
              {saving ? 'Guardando...' : match.myPrediction ? 'Actualizar' : '¡Pronosticá!'}
            </button>
          </>
        ) : isFinished && match.myPrediction ? (
          <div className="mpcard__result">
            <span className="mpcard__result-label">Tu pronóstico</span>
            <span className="mpcard__result-score">
              {match.myPrediction.predictedHomeScore} - {match.myPrediction.predictedAwayScore}
            </span>
            <span className="mpcard__result-points">
              {match.myPrediction.points} pts
            </span>
          </div>
        ) : isCancelled ? (
          <span className="mpcard__cancelled-text">Cancelado</span>
        ) : (
          <span className="mpcard__closed-text">
            Pronóstico cerrado
            {match.myPrediction &&
              ` (${match.myPrediction.predictedHomeScore}-${match.myPrediction.predictedAwayScore})`}
          </span>
        )}
      </div>

      {error && <span className="mpcard__error">{error}</span>}
      {savedMessage && <span className="mpcard__saved">{savedMessage}</span>}

      {isFinished && (
        <div className="mpcard__official">
          Resultado: {match.homeGoals} - {match.awayGoals}
        </div>
      )}
    </div>
  )
}
