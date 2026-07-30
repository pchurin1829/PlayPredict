import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { isoToLocalInput, localInputToIsoUtc } from '../api/dateUtils'
import { MATCH_STATUSES, MATCH_STATUS_LABELS, type Match, type MatchStatus } from '../api/types'
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

    try {
      if (isEdit) {
        await api.put(`/matches/${matchId}`, {
          participantHome,
          participantAway,
          startsAtUtc: localInputToIsoUtc(startsAtUtc),
          // Un partido Finalizado no debe enviar status: su resultado oficial
          // solo se modifica desde "Cargar resultado".
          ...(currentStatus === 'Finished' ? {} : { status }),
        })
      } else {
        await api.post<Match>(`/rounds/${roundIdParam}/matches`, {
          participantHome,
          participantAway,
          startsAtUtc: localInputToIsoUtc(startsAtUtc),
          status,
        })
      }
      navigate(`/rounds/${roundId}/matches`, {
        replace: true,
        state: { savedMessage: 'Partido guardado correctamente.' },
      })
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
          {currentStatus === 'Finished' ? (
            <input id="status" type="text" value={MATCH_STATUS_LABELS.Finished} disabled />
          ) : (
            <select
              id="status"
              value={status}
              onChange={(e) => setStatus(e.target.value as MatchStatus)}
            >
              {MATCH_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {MATCH_STATUS_LABELS[s]}
                </option>
              ))}
            </select>
          )}
          {fieldErrors.status && <span className="form-field-error">{fieldErrors.status[0]}</span>}
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          <Link to={`/rounds/${roundId}/matches`} className="btn btn-secondary">
            Volver a Partidos
          </Link>
        </div>
      </form>
    </div>
  )
}
