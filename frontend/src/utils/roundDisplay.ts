import type { Match, Round } from '../api/types'

const DATE_FORMAT = new Intl.DateTimeFormat('es-AR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

function day(value: string): string {
  return DATE_FORMAT.format(new Date(value))
}

export function roundCalendarLabel(round: Round, matches: Match[] = []): string | null {
  const matchDays = [...new Set(matches.map(match => day(match.startsAtUtc)))]
  if (matchDays.length === 1) return matchDays[0]
  if (matchDays.length > 1) return `${matchDays[0]} al ${matchDays[matchDays.length - 1]}`

  if (round.startDateUtc && round.endDateUtc) {
    const from = day(round.startDateUtc)
    const to = day(round.endDateUtc)
    return from === to ? from : `${from} al ${to}`
  }
  if (round.startDateUtc) return day(round.startDateUtc)
  if (round.endDateUtc) return day(round.endDateUtc)

  return null
}

export function roundDisplayName(round: Round, matches: Match[] = []): string {
  const calendar = roundCalendarLabel(round, matches)
  return calendar ? `${round.name} — ${calendar}` : round.name
}
