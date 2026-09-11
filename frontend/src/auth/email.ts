export const normalizeEmail = (email: string) => email.trim().toLowerCase()

export function validateEmailPair(email: string, confirmation: string, emailField = 'email') {
  const errors: Record<string, string[]> = {}
  const valid = (value: string) => value.length <= 200 && /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/.test(value)
  const normalized = normalizeEmail(email)
  const confirmed = normalizeEmail(confirmation)
  if (!valid(normalized)) errors[emailField] = ['Ingresá un email válido (máximo 200 caracteres).']
  if (!valid(confirmed)) errors.confirmEmail = ['Confirmá el email con un formato válido.']
  else if (normalized !== confirmed) errors.confirmEmail = ['Los emails no coinciden.']
  return errors
}
