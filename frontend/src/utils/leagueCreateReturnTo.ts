const KNOWN_RETURN_LABELS: Record<string, string> = {
  '/leagues': 'Mis Ligas',
  '/competitions/explore': 'Competencias Oficiales',
}

const FALLBACK_RETURN_TO = '/competitions/explore'

/**
 * Sólo acepta rutas internas de PlayPredict: deben empezar con "/" (no "//"),
 * sin backslashes ni caracteres de control. Cualquier URL absoluta, esquema
 * (http:, https:, javascript:, etc.) o dominio externo queda rechazado, así
 * que un valor manipulado siempre cae al fallback interno.
 */
export function isSafeInternalPath(value: string | null | undefined): value is string {
  if (!value) return false
  const trimmed = value.trim()
  if (trimmed.length === 0) return false
  if (!trimmed.startsWith('/')) return false
  if (trimmed.startsWith('//')) return false
  if (trimmed.includes('\\')) return false
  // eslint-disable-next-line no-control-regex
  if (/[\x00-\x1f]/.test(trimmed)) return false
  return true
}

export function leagueCreatePath(officialLeagueId: number, returnTo: string): string {
  const safeReturnTo = isSafeInternalPath(returnTo) ? returnTo : FALLBACK_RETURN_TO
  const params = new URLSearchParams({
    officialLeagueId: String(officialLeagueId),
    returnTo: safeReturnTo,
  })
  return `/leagues/new?${params.toString()}`
}

export function resolveLeagueCreateReturnTo(value: string | null): { path: string; label: string | null } {
  const path = isSafeInternalPath(value) ? value : FALLBACK_RETURN_TO
  return { path, label: KNOWN_RETURN_LABELS[path] ?? null }
}
