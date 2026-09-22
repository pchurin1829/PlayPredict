export interface PointsBreakdownInput {
  points: number | null
  resultPoints: number | null
  preferredPlayerPoints: number | null
  preferredPlayerName: string | null
  evaluationLabel: string | null
}

export interface PointsBreakdownRow {
  kind: 'result' | 'bonus'
  label: string
  pointsText: string
}

/**
 * Builds the visible points breakdown strictly from backend-provided values.
 * Returns null when there is no evaluation yet (points === null): the UI must
 * not invent a breakdown. Never recalculates scoring.
 */
export function buildPointsBreakdown(
  input: PointsBreakdownInput,
): PointsBreakdownRow[] | null {
  if (input.points === null || input.points === undefined) return null

  const rows: PointsBreakdownRow[] = [
    {
      kind: 'result',
      label: input.evaluationLabel ?? 'Resultado',
      pointsText: `${input.resultPoints ?? input.points} pts`,
    },
  ]

  const bonus = input.preferredPlayerPoints ?? 0
  if (bonus > 0) {
    rows.push({
      kind: 'bonus',
      label: input.preferredPlayerName ?? 'Jugador Preferido',
      pointsText: `+${bonus} pts`,
    })
  }

  return rows
}
