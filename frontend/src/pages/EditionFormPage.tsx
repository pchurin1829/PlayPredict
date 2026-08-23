import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { isoToLocalInput, localInputToIsoUtc } from '../api/dateUtils'
import { EDITION_STATUSES, EDITION_STATUS_LABELS, type Edition, type EditionStatus } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function EditionFormPage() {
  const { competitionId: competitionIdParam, editionId } = useParams()
  const isEdit = Boolean(editionId)
  const navigate = useNavigate()

  const [competitionId, setCompetitionId] = useState<string | undefined>(competitionIdParam)
  const [name, setName] = useState('')
  const [startDateUtc, setStartDateUtc] = useState('')
  const [endDateUtc, setEndDateUtc] = useState('')
  const [status, setStatus] = useState<EditionStatus>('Draft')

  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (!isEdit) return
    api.get<Edition>(`/editions/${editionId}`).then((ed) => {
        setCompetitionId(String(ed.competitionId))
        setName(ed.name)
        setStartDateUtc(isoToLocalInput(ed.startDateUtc))
        setEndDateUtc(isoToLocalInput(ed.endDateUtc))
        setStatus(ed.status)
      })
      .catch((err) => setError(err.message ?? 'No se pudo cargar la edición.'))
      .finally(() => setLoading(false))
  }, [editionId, isEdit])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    const payload = {
      name,
      startDateUtc: localInputToIsoUtc(startDateUtc),
      endDateUtc: localInputToIsoUtc(endDateUtc),
      status,
    }

    try {
      if (isEdit) {
        await api.put(`/editions/${editionId}`, payload)
        setSaved(true)
      } else {
        const created = await api.post<Edition>(`/competitions/${competitionIdParam}/editions`, payload)
        setSaved(true)
        navigate(`/editions/${created.id}/edit`, { replace: true })
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
    return <StatusMessage kind="loading" message="Cargando edición..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to={`/competitions/${competitionId}/editions`}>← Volver a Ediciones</Link>
      </div>
      <div className="admin-header">
        <h1>{isEdit ? 'Editar Edición' : 'Nueva Edición'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Edición guardada correctamente." />}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-field">
          <label htmlFor="name">Nombre</label>
          <input id="name" type="text" value={name} onChange={(e) => setName(e.target.value)} />
          {fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}
        </div>

        <div className="form-row">
          <div className="form-field">
            <label htmlFor="startDateUtc">Fecha de inicio</label>
            <input
              id="startDateUtc"
              type="datetime-local"
              value={startDateUtc}
              onChange={(e) => setStartDateUtc(e.target.value)}
            />
          </div>

          <div className="form-field">
            <label htmlFor="endDateUtc">Fecha de finalización (opcional)</label>
            <input
              id="endDateUtc"
              type="datetime-local"
              value={endDateUtc}
              onChange={(e) => setEndDateUtc(e.target.value)}
            />
            {fieldErrors.endDateUtc && (
              <span className="form-field-error">{fieldErrors.endDateUtc[0]}</span>
            )}
          </div>
        </div>

        <div className="form-field">
          <label htmlFor="status">Estado</label>
          <select
            id="status"
            value={status}
            onChange={(e) => setStatus(e.target.value as EditionStatus)}
          >
            {EDITION_STATUSES.map((s) => (
              <option key={s} value={s}>
                {EDITION_STATUS_LABELS[s]}
              </option>
            ))}
          </select>
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          <Link to={`/competitions/${competitionId}/editions`} className="btn btn-secondary">Cancelar</Link>
          {isEdit && (
            <Link to={`/editions/${editionId}/rounds`} className="btn btn-tertiary form-actions__contextual">
              Ver Fechas
            </Link>
          )}
        </div>
      </form>
    </div>
  )
}
