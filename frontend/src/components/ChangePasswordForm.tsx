import { useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { AuthResponse } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from './StatusMessage'

export default function ChangePasswordForm({ forced = false }: { forced?: boolean }) {
  const { updateUser } = useAuth()
  const [open, setOpen] = useState(forced)
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmNewPassword, setConfirmNewPassword] = useState('')
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  function reset() {
    setCurrentPassword(''); setNewPassword(''); setConfirmNewPassword(''); setError(null); setFieldErrors({})
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    const errors: Record<string, string[]> = {}
    if (!currentPassword) errors.currentPassword = ['Ingresá tu contraseña actual.']
    if (newPassword.length < 10) errors.newPassword = ['La nueva contraseña debe tener al menos 10 caracteres.']
    if (newPassword !== confirmNewPassword) errors.confirmNewPassword = ['La confirmación no coincide con la nueva contraseña.']
    setFieldErrors(errors); setError(null); setSaved(false)
    if (Object.keys(errors).length) return
    setSaving(true)
    try {
      const response = await api.put<AuthResponse>('/users/me/password', {
        currentPassword, newPassword, confirmNewPassword,
      })
      updateUser(response.user, response.token)
      reset(); setOpen(forced); setSaved(true)
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo cambiar la contraseña.')
      if (reason instanceof ApiError) setFieldErrors(reason.fieldErrors)
    } finally { setSaving(false) }
  }

  return <section aria-label="Cambiar contraseña">
    {saved && <StatusMessage kind="success" message="Contraseña actualizada correctamente." />}
    {forced && !saved && <StatusMessage kind="error" message="Tu contraseña es temporal. Cambiala para continuar usando PlayPredict." />}
    {!open && !forced ? <div className="pp-form__actions"><button type="button" className="pp-btn pp-btn--secondary" onClick={() => { reset(); setSaved(false); setOpen(true) }}>Cambiar contraseña</button></div> :
      <form onSubmit={submit} noValidate>
        {!forced && <h2>Cambiar contraseña</h2>}
        <p>La nueva contraseña debe tener al menos 10 caracteres.</p>
        {error && <StatusMessage kind="error" message={error} />}
        <div className="pp-form__field">
          <label className="pp-form__label" htmlFor="currentPasswordChange">Contraseña actual</label>
          <input className="pp-form__input" id="currentPasswordChange" type="password" autoComplete="current-password" value={currentPassword} onChange={e => setCurrentPassword(e.target.value)} disabled={saving} />
          {fieldErrors.currentPassword && <span className="pp-form__error">{fieldErrors.currentPassword[0]}</span>}
        </div>
        <div className="pp-form__field">
          <label className="pp-form__label" htmlFor="newPasswordChange">Nueva contraseña</label>
          <input className="pp-form__input" id="newPasswordChange" type="password" autoComplete="new-password" value={newPassword} onChange={e => setNewPassword(e.target.value)} disabled={saving} />
          {fieldErrors.newPassword && <span className="pp-form__error">{fieldErrors.newPassword[0]}</span>}
        </div>
        <div className="pp-form__field">
          <label className="pp-form__label" htmlFor="confirmNewPasswordChange">Confirmar nueva contraseña</label>
          <input className="pp-form__input" id="confirmNewPasswordChange" type="password" autoComplete="new-password" value={confirmNewPassword} onChange={e => setConfirmNewPassword(e.target.value)} disabled={saving} />
          {fieldErrors.confirmNewPassword && <span className="pp-form__error">{fieldErrors.confirmNewPassword[0]}</span>}
        </div>
        <div className="pp-form__actions">
          <button type="submit" className="pp-btn pp-btn--primary" disabled={saving}>{saving ? 'Guardando...' : 'Guardar nueva contraseña'}</button>
          {!forced && <button type="button" className="pp-btn pp-btn--secondary" disabled={saving} onClick={() => { reset(); setOpen(false) }}>Cancelar</button>}
        </div>
      </form>}
  </section>
}
