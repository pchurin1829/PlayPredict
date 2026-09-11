import { useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { AuthResponse } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { normalizeEmail, validateEmailPair } from '../auth/email'
import StatusMessage from './StatusMessage'

export default function ChangeEmailForm() {
  const { updateUser } = useAuth()
  const [open, setOpen] = useState(false)
  const [newEmail, setNewEmail] = useState('')
  const [confirmEmail, setConfirmEmail] = useState('')
  const [currentPassword, setCurrentPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  function reset() {
    setNewEmail(''); setConfirmEmail(''); setCurrentPassword(''); setError(null); setFieldErrors({})
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    const errors = validateEmailPair(newEmail, confirmEmail, 'newEmail')
    if (!currentPassword) errors.currentPassword = ['Ingresá tu contraseña actual.']
    setFieldErrors(errors); setError(null); setSaved(false)
    if (Object.keys(errors).length) return
    setSaving(true)
    try {
      const response = await api.put<AuthResponse>('/users/me/email', {
        newEmail: normalizeEmail(newEmail), confirmEmail: normalizeEmail(confirmEmail), currentPassword,
      })
      updateUser(response.user, response.token)
      reset(); setOpen(false); setSaved(true)
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo cambiar el email.')
      if (reason instanceof ApiError) setFieldErrors(reason.fieldErrors)
    } finally { setSaving(false) }
  }

  return <section aria-label="Cambiar email">
    {saved && <StatusMessage kind="success" message="Email actualizado. Usá el nuevo email para ingresar a PlayPredict." />}
    {!open ? <div className="pp-form__actions"><button type="button" className="pp-btn pp-btn--secondary" onClick={() => { reset(); setSaved(false); setOpen(true) }}>Cambiar email</button></div> :
      <form onSubmit={submit} noValidate>
        <h2>Cambiar email</h2>
        <p>Este email será tu usuario para ingresar a PlayPredict.</p>
        {error && <StatusMessage kind="error" message={error} />}
        <div className="pp-form__field">
          <label className="pp-form__label" htmlFor="newEmail">Nuevo email</label>
          <input className="pp-form__input" id="newEmail" type="email" autoComplete="email" value={newEmail} onChange={e => setNewEmail(e.target.value)} disabled={saving} />
          {fieldErrors.newEmail && <span className="pp-form__error">{fieldErrors.newEmail[0]}</span>}
        </div>
        <div className="pp-form__field">
          <label className="pp-form__label" htmlFor="confirmEmail">Confirmar nuevo email</label>
          <input className="pp-form__input" id="confirmEmail" type="email" autoComplete="email" value={confirmEmail} onChange={e => setConfirmEmail(e.target.value)} disabled={saving} />
          {fieldErrors.confirmEmail && <span className="pp-form__error">{fieldErrors.confirmEmail[0]}</span>}
        </div>
        <div className="pp-form__field">
          <label className="pp-form__label" htmlFor="currentPassword">Contraseña actual</label>
          <input className="pp-form__input" id="currentPassword" type="password" autoComplete="current-password" value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} disabled={saving} />
          {fieldErrors.currentPassword && <span className="pp-form__error">{fieldErrors.currentPassword[0]}</span>}
        </div>
        <div className="pp-form__actions">
          <button type="submit" className="pp-btn pp-btn--primary" disabled={saving}>{saving ? 'Guardando...' : 'Guardar nuevo email'}</button>
          <button type="button" className="pp-btn pp-btn--secondary" disabled={saving} onClick={() => { reset(); setOpen(false) }}>Cancelar</button>
        </div>
      </form>}
  </section>
}
