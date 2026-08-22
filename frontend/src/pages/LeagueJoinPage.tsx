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
  const [success, setSuccess] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSuccess(null)

    try {
      const league = await api.post<LeagueSummary>('/leagues/join', { inviteCode })
      if (league.isParticipant && league.participantsCount > 1) {
        setSuccess(`¡Te uniste a "${league.name}"! Redirigiendo...`)
      } else {
        setSuccess(`Ya participás en "${league.name}". Redirigiendo...`)
      }
      setTimeout(() => navigate(`/leagues/${league.id}`, { replace: true }), 1200)
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 404) {
          setError('El código de invitación no existe. Verificá e intentá nuevamente.')
        } else {
          setError(err.message)
          setFieldErrors(err.fieldErrors)
        }
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
        <h1>Unirme a Liga de Amigos con código</h1>
        <p className="pp-header__subtitle">
          Usá el código de invitación que te compartió el creador de una Liga de Amigos.
        </p>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {success && <StatusMessage kind="success" message={success} />}

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
              onChange={(e) => setInviteCode(e.target.value.toUpperCase())}
              style={{ textAlign: 'center', fontSize: '1.1rem', letterSpacing: '0.1em', fontWeight: 700 }}
            />
            {fieldErrors.inviteCode && <span className="pp-form__error">{fieldErrors.inviteCode[0]}</span>}
          </div>

          <div className="pp-form__actions">
            <button type="submit" className="pp-btn pp-btn--primary" disabled={saving || !inviteCode.trim()} style={{ flex: 1 }}>
              {saving ? 'Uniéndome...' : '✋ Unirme a Liga de Amigos'}
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
