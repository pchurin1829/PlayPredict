export function validAdminReturnTo(value: string | null): string | null {
  if (!value || !value.startsWith('/admin/official-leagues/') || !value.endsWith('/matches')) return null
  if (value.includes('://') || value.startsWith('//')) return null
  return value
}

export function appendReturnTo(path: string, returnTo: string | null): string {
  return returnTo ? `${path}${path.includes('?') ? '&' : '?'}returnTo=${encodeURIComponent(returnTo)}` : path
}
