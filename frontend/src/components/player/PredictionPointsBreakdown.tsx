import type { Prediction } from '../../api/types'
import { buildPointsBreakdown } from '../../utils/pointsBreakdown'

interface Props {
  prediction: Prediction
}

/**
 * Explicit points breakdown for evaluated predictions.
 * Renders nothing when there is no evaluation yet.
 * All values come from the backend evaluation; nothing is recalculated.
 */
export default function PredictionPointsBreakdown({ prediction }: Props) {
  const rows = buildPointsBreakdown({
    points: prediction.points,
    resultPoints: prediction.resultPoints,
    preferredPlayerPoints: prediction.preferredPlayerPoints,
    preferredPlayerName: prediction.preferredPlayerName,
    evaluationLabel: prediction.evaluationLabel,
  })

  if (!rows) return null

  return (
    <>
      {rows.map((row) => (
        <div className="pp-match-card__result-row" key={row.kind}>
          <span className="pp-match-card__result-label">
            {row.kind === 'result' ? 'Motivo' : 'Jugador Preferido'}
          </span>
          <span className="pp-match-card__result-value">
            {row.label} · {row.pointsText}
          </span>
        </div>
      ))}
    </>
  )
}
