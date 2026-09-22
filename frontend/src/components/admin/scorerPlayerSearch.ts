import type { TeamPlayer } from '../../api/types'

/** Accent- and case-insensitive normalization (same approach as PreferredPlayerPicker). */
export function normalizeScorerQuery(value: string): string {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase()
}

function scorerHaystack(player: TeamPlayer): string {
  return normalizeScorerQuery(
    `${player.displayName} ${player.firstName} ${player.lastName}`,
  )
}

/**
 * Filters an already eligibility-scoped player list by free text.
 * Empty query returns the list unchanged. Never reorders.
 */
export function filterScorerPlayers(
  players: TeamPlayer[],
  query: string,
): TeamPlayer[] {
  const needle = normalizeScorerQuery(query.trim())
  if (!needle) return players
  return players.filter((player) => scorerHaystack(player).includes(needle))
}
