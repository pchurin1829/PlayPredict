import { useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { User } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

export default function ProfilePage() {
  const { user } = useAuth()

  const [firstName, setFirstName] = useState(user?.firstName ?? '')
  const [lastName, setLastName] = useState(user?.lastName ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  if (!user) {
    return null
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    try {
      await api.put<User>('/users/me', { firstName, lastName })
      setSaved(true)
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.fieldErrors)
      } else {
        setError('Ocurrió un error inesperado al guardar.')
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <div>
      <div className="pp-header">
        <h1>Mi Perfil</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Perfil actualizado correctamente." />}

      <div className="pp-profile__card">
        <div className="pp-profile__avatar-section">
          <div className="pp-profile__avatar">
            {user.firstName[0]}{user.lastName[0]}
          </div>
          <div>
            <div className="pp-profile__name">{user.firstName} {user.lastName}</div>
            <div className="pp-profile__email">{user.email}</div>
          </div>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="pp-form__row">
            <div className="pp-form__field">
              <label className="pp-form__label" htmlFor="firstName">Nombre</label>
              <input
                id="firstName"
                className="pp-form__input"
                type="text"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
              />
              {fieldErrors.firstName && (
                <span className="pp-form__error">{fieldErrors.firstName[0]}</span>
              )}
            </div>

            <div className="pp-form__field">
              <label className="pp-form__label" htmlFor="lastName">Apellido</label>
              <input
                id="lastName"
                className="pp-form__input"
                type="text"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
              />
              {fieldErrors.lastName && (
                <span className="pp-form__error">{fieldErrors.lastName[0]}</span>
              )}
            </div>
          </div>

          <div className="pp-form__field">
            <label className="pp-form__label">Email</label>
            <input className="pp-form__input" type="text" value={user.email} disabled style={{ opacity: 0.6 }} />
          </div>

          <div className="pp-form__actions">
            <button type="submit" className="pp-btn pp-btn--primary" disabled={saving}>
              {saving ? 'Guardando...' : 'Guardar'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
