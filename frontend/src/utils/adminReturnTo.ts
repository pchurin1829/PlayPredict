export function validAdminReturnTo(value: string | null): string | null {
  if (!value || !/^\/admin\/official-leagues\/\d+\/matches$/.test(value)) return null
  if (value.includes('://') || value.startsWith('//')) return null
  return value
}

export function officialLeagueIdFromReturnTo(returnTo: string | null): number | null {
  if (!returnTo) return null
  const match = /^\/admin\/official-leagues\/(\d+)\/matches$/.exec(returnTo)
  return match ? Number(match[1]) : null
}

export function appendReturnTo(path: string, returnTo: string | null): string {
  return returnTo ? `${path}${path.includes('?') ? '&' : '?'}returnTo=${encodeURIComponent(returnTo)}` : path
}
