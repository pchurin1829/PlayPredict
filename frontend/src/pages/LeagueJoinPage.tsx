import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { LeagueSummary } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function LeagueJoinPage() {
  const navigate = useNavigate()
  const [inviteCode, setInviteCode] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})

    try {
      const league = await api.post<LeagueSummary>('/leagues/join', { inviteCode })
      navigate(`/leagues/${league.id}`, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.fieldErrors)
      } else {
        setError('Ocurrió un error inesperado al unirte a la Liga.')
      }
      setSaving(false)
    }
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/leagues">← Mis Ligas</Link>
      </div>
      <div className="admin-header">
        <h1>Unirse a una Liga</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-field">
          <label htmlFor="inviteCode">Código de invitación</label>
          <input
            id="inviteCode"
            type="text"
            placeholder="Ej: 9JT3UMS4"
            value={inviteCode}
            onChange={(e) => setInviteCode(e.target.value)}
          />
          {fieldErrors.inviteCode && <span className="form-field-error">{fieldErrors.inviteCode[0]}</span>}
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving || !inviteCode.trim()}>
            {saving ? 'Uniéndome...' : 'Unirme'}
          </button>
          <Link to="/leagues" className="btn btn-secondary">
            Cancelar
          </Link>
        </div>
      </form>
    </div>
  )
}
