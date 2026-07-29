import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { isoToLocalInput, localInputToIsoUtc } from '../api/dateUtils'
import { MATCH_STATUSES, type Match, type MatchStatus } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function MatchFormPage() {
  const { roundId: roundIdParam, matchId } = useParams()
  const isEdit = Boolean(matchId)
  const navigate = useNavigate()

  const [roundId, setRoundId] = useState<string | undefined>(roundIdParam)
  const [participantHome, setParticipantHome] = useState('')
  const [participantAway, setParticipantAway] = useState('')
  const [startsAtUtc, setStartsAtUtc] = useState('')
  const [status, setStatus] = useState<MatchStatus>('Scheduled')
  const [currentStatus, setCurrentStatus] = useState<string | null>(null)

  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (!isEdit) return
    api
      .get<Match>(`/matches/${matchId}`)
      .then((m) => {
        setRoundId(String(m.roundId))
        setParticipantHome(m.participantHome)
        setParticipantAway(m.participantAway)
        setStartsAtUtc(isoToLocalInput(m.startsAtUtc))
        setCurrentStatus(m.status)
        if (m.status !== 'Finished') {
          setStatus(m.status)
        }
      })
      .catch((err) => setError(err.message ?? 'No se pudo cargar el partido.'))
      .finally(() => setLoading(false))
  }, [matchId, isEdit])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    try {
      if (isEdit) {
        await api.put(`/matches/${matchId}`, {
          participantHome,
          participantAway,
          startsAtUtc: localInputToIsoUtc(startsAtUtc),
          status,
        })
        setSaved(true)
      } else {
        const created = await api.post<Match>(`/rounds/${roundIdParam}/matches`, {
          participantHome,
          participantAway,
          startsAtUtc: localInputToIsoUtc(startsAtUtc),
          status,
        })
        setSaved(true)
        navigate(`/matches/${created.id}/edit`, { replace: true })
        return
      }
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

  if (loading) {
    return <StatusMessage kind="loading" message="Cargando partido..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to={`/rounds/${roundId}/matches`}>← Partidos</Link>
      </div>
      <div className="admin-header">
        <h1>{isEdit ? 'Editar Partido' : 'Nuevo Partido'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Partido guardado correctamente." />}
      {currentStatus === 'Finished' && (
        <StatusMessage
          kind="loading"
          message="Este partido ya tiene Resultado Oficial cargado. El estado se administra desde la lista de Partidos."
        />
      )}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-row">
          <div className="form-field">
            <label htmlFor="participantHome">Participante local</label>
            <input
              id="participantHome"
              type="text"
              value={participantHome}
              onChange={(e) => setParticipantHome(e.target.value)}
            />
            {fieldErrors.participantHome && (
              <span className="form-field-error">{fieldErrors.participantHome[0]}</span>
            )}
          </div>

          <div className="form-field">
            <label htmlFor="participantAway">Participante visitante</label>
            <input
              id="participantAway"
              type="text"
              value={participantAway}
              onChange={(e) => setParticipantAway(e.target.value)}
            />
            {fieldErrors.participantAway && (
              <span className="form-field-error">{fieldErrors.participantAway[0]}</span>
            )}
          </div>
        </div>

        <div className="form-field">
          <label htmlFor="startsAtUtc">Fecha y hora de inicio</label>
          <input
            id="startsAtUtc"
            type="datetime-local"
            value={startsAtUtc}
            onChange={(e) => setStartsAtUtc(e.target.value)}
          />
        </div>

        <div className="form-field">
          <label htmlFor="status">Estado</label>
          <select
            id="status"
            value={status}
            onChange={(e) => setStatus(e.target.value as MatchStatus)}
            disabled={currentStatus === 'Finished'}
          >
            {MATCH_STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
          {fieldErrors.status && <span className="form-field-error">{fieldErrors.status[0]}</span>}
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
