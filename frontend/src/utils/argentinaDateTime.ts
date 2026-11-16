const DATE_FORMATTER = new Intl.DateTimeFormat('es-AR', {
  timeZone: 'America/Argentina/Buenos_Aires',
  day: 'numeric',
  month: 'short',
})

const TIME_FORMATTER = new Intl.DateTimeFormat('es-AR', {
  timeZone: 'America/Argentina/Buenos_Aires',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
})

export function formatArgentinaDate(iso: string): string {
  return DATE_FORMATTER.format(new Date(iso))
}

export function formatArgentinaTime(iso: string): string {
  return TIME_FORMATTER.format(new Date(iso))
}
