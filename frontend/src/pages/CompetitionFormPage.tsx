import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Competition } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function CompetitionFormPage() {
  const { competitionId } = useParams()
  const isEdit = Boolean(competitionId)
  const navigate = useNavigate()

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [sport, setSport] = useState('')
  const [isActive, setIsActive] = useState(true)

  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (!isEdit) return
    api
      .get<Competition>(`/competitions/${competitionId}`)
      .then((c) => {
        setName(c.name)
        setDescription(c.description ?? '')
        setSport(c.sport)
        setIsActive(c.isActive)
      })
      .catch((err) => setError(err.message ?? 'No se pudo cargar la competencia.'))
      .finally(() => setLoading(false))
  }, [competitionId, isEdit])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    const payload = { name, description: description || null, sport, isActive }

    try {
      if (isEdit) {
        await api.put(`/competitions/${competitionId}`, payload)
      } else {
        const created = await api.post<Competition>('/competitions', payload)
        setSaved(true)
        navigate(`/competitions/${created.id}/edit`, { replace: true })
        return
      }
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

  if (loading) {
    return <StatusMessage kind="loading" message="Cargando competencia..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/competitions">← Competencias</Link>
      </div>
      <div className="admin-header">
        <h1>{isEdit ? 'Editar Competencia' : 'Nueva Competencia'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Competencia guardada correctamente." />}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-field">
          <label htmlFor="name">Nombre</label>
          <input id="name" type="text" value={name} onChange={(e) => setName(e.target.value)} />
          {fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}
        </div>

        <div className="form-field">
          <label htmlFor="sport">Deporte / categoría</label>
          <input id="sport" type="text" value={sport} onChange={(e) => setSport(e.target.value)} />
          {fieldErrors.sport && <span className="form-field-error">{fieldErrors.sport[0]}</span>}
        </div>

        <div className="form-field">
          <label htmlFor="description">Descripción (opcional)</label>
          <textarea
            id="description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        <div className="form-field form-checkbox">
          <input
            id="isActive"
            type="checkbox"
            checked={isActive}
            onChange={(e) => setIsActive(e.target.checked)}
          />
          <label htmlFor="isActive">Activa</label>
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          {isEdit && (
            <Link to={`/competitions/${competitionId}/editions`} className="btn btn-secondary">
              Ver Ediciones
            </Link>
          )}
        </div>
      </form>
    </div>
  )
}
