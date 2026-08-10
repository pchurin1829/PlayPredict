import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { LeagueSummary } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

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
      <Link to="/leagues" className="pp-back">← Mis Ligas</Link>

      <div className="pp-header">
        <h1>Unirse a una Liga</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}

      <div className="pp-form" style={{ maxWidth: '420px' }}>
        <form onSubmit={handleSubmit}>
          <div className="pp-form__field">
            <label className="pp-form__label" htmlFor="inviteCode">Código de invitación</label>
            <input
              id="inviteCode"
              className="pp-form__input"
              type="text"
              placeholder="Ej: 9JT3UMS4"
              value={inviteCode}
              onChange={(e) => setInviteCode(e.target.value)}
              style={{ textAlign: 'center', fontSize: '1.1rem', letterSpacing: '0.1em', fontWeight: 700 }}
            />
            {fieldErrors.inviteCode && <span className="pp-form__error">{fieldErrors.inviteCode[0]}</span>}
          </div>

          <div className="pp-form__actions">
            <button type="submit" className="pp-btn pp-btn--primary" disabled={saving || !inviteCode.trim()} style={{ flex: 1 }}>
              {saving ? 'Uniéndome...' : '✋ Unirme'}
            </button>
            <Link to="/leagues" className="pp-btn pp-btn--secondary">
              Cancelar
            </Link>
          </div>
        </form>
      </div>
    </div>
  )
}
