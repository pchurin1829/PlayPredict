import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { isoToLocalInput, localInputToIsoUtc } from '../api/dateUtils'
import type { Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function RoundFormPage() {
  const { editionId: editionIdParam, roundId } = useParams()
  const isEdit = Boolean(roundId)
  const navigate = useNavigate()

  const [editionId, setEditionId] = useState<string | undefined>(editionIdParam)
  const [name, setName] = useState('')
  const [order, setOrder] = useState(1)
  const [startDateUtc, setStartDateUtc] = useState('')
  const [endDateUtc, setEndDateUtc] = useState('')

  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (!isEdit) return
    api
      .get<Round>(`/rounds/${roundId}`)
      .then((r) => {
        setEditionId(String(r.editionId))
        setName(r.name)
        setOrder(r.order)
        setStartDateUtc(isoToLocalInput(r.startDateUtc))
        setEndDateUtc(isoToLocalInput(r.endDateUtc))
      })
      .catch((err) => setError(err.message ?? 'No se pudo cargar la fecha.'))
      .finally(() => setLoading(false))
  }, [roundId, isEdit])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    const payload = {
      name,
      order,
      startDateUtc: localInputToIsoUtc(startDateUtc),
      endDateUtc: localInputToIsoUtc(endDateUtc),
    }

    try {
      if (isEdit) {
        await api.put(`/rounds/${roundId}`, payload)
        setSaved(true)
      } else {
        const created = await api.post<Round>(`/editions/${editionIdParam}/rounds`, payload)
        setSaved(true)
        navigate(`/rounds/${created.id}/edit`, { replace: true })
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
    return <StatusMessage kind="loading" message="Cargando fecha..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to={`/editions/${editionId}/rounds`}>← Fechas</Link>
      </div>
      <div className="admin-header">
        <h1>{isEdit ? 'Editar Fecha' : 'Nueva Fecha'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Fecha guardada correctamente." />}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-field">
          <label htmlFor="name">Nombre</label>
          <input id="name" type="text" value={name} onChange={(e) => setName(e.target.value)} />
          {fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}
        </div>

        <div className="form-field">
          <label htmlFor="order">Orden</label>
          <input
            id="order"
            type="number"
            min={1}
            value={order}
            onChange={(e) => setOrder(Number(e.target.value))}
          />
          {fieldErrors.order && <span className="form-field-error">{fieldErrors.order[0]}</span>}
        </div>

        <div className="form-row">
          <div className="form-field">
            <label htmlFor="startDateUtc">Fecha de inicio (opcional)</label>
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

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          {isEdit && (
            <Link to={`/rounds/${roundId}/matches`} className="btn btn-secondary">
              Ver Partidos
            </Link>
          )}
        </div>
      </form>
    </div>
  )
}
