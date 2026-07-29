// Convierte una fecha ISO UTC a un valor apto para <input type="datetime-local"> en hora local del navegador.
export function isoToLocalInput(iso: string | null): string {
  if (!iso) return ''
  const date = new Date(iso)
  const offsetMs = date.getTimezoneOffset() * 60000
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}

// Convierte el valor de <input type="datetime-local"> (hora local) a ISO UTC para enviar al backend.
export function localInputToIsoUtc(localValue: string): string | null {
  if (!localValue) return null
  return new Date(localValue).toISOString()
}
