import { useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { User } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from '../components/StatusMessage'

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
      <div className="admin-header">
        <h1>Mi perfil</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Perfil actualizado correctamente." />}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-row">
          <div className="form-field">
            <label htmlFor="firstName">Nombre</label>
            <input
              id="firstName"
              type="text"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
            />
            {fieldErrors.firstName && (
              <span className="form-field-error">{fieldErrors.firstName[0]}</span>
            )}
          </div>

          <div className="form-field">
            <label htmlFor="lastName">Apellido</label>
            <input
              id="lastName"
              type="text"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
            />
            {fieldErrors.lastName && (
              <span className="form-field-error">{fieldErrors.lastName[0]}</span>
            )}
          </div>
        </div>

        <div className="form-field">
          <label>Email</label>
          <input type="text" value={user.email} disabled />
        </div>

        <div className="form-field">
          <label>Roles</label>
          <input type="text" value={user.roles.join(', ')} disabled />
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </form>
    </div>
  )
}
