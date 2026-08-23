import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Experience } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function AdminExperienceFormPage() {
  const { experienceId } = useParams()
  const isEdit = Boolean(experienceId)
  const navigate = useNavigate()

  const [activeTab, setActiveTab] = useState<'general' | 'configuracion'>('general')

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [primaryColor, setPrimaryColor] = useState('')
  const [secondaryColor, setSecondaryColor] = useState('')
  const [isPublic, setIsPublic] = useState(false)
  const [defaultExactScorePoints, setDefaultExactScorePoints] = useState(6)
  const [defaultCorrectOutcomePoints, setDefaultCorrectOutcomePoints] = useState(3)
  const [defaultIncorrectPoints, setDefaultIncorrectPoints] = useState(0)
  const [statusLabel, setStatusLabel] = useState<string | null>(null)

  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (!isEdit) return
    api
      .get<Experience>(`/admin/experiences/${experienceId}`)
      .then((e) => {
        setName(e.name)
        setDescription(e.description ?? '')
        setPrimaryColor(e.primaryColor ?? '')
        setSecondaryColor(e.secondaryColor ?? '')
        setIsPublic(e.isPublic)
        setDefaultExactScorePoints(e.defaultExactScorePoints)
        setDefaultCorrectOutcomePoints(e.defaultCorrectOutcomePoints)
        setDefaultIncorrectPoints(e.defaultIncorrectPoints)
        setStatusLabel(e.statusLabel)
      })
      .catch((err) => setError(err.message ?? 'No se pudo cargar la experiencia.'))
      .finally(() => setLoading(false))
  }, [experienceId, isEdit])

  function clampNonNegative(value: string): number {
    const n = Math.trunc(Number(value))
    return Number.isFinite(n) && n >= 0 ? n : 0
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    const payload = {
      name,
      description: description || null,
      primaryColor: primaryColor || null,
      secondaryColor: secondaryColor || null,
      logoUrl: null,
      isPublic,
      defaultExactScorePoints,
      defaultCorrectOutcomePoints,
      defaultIncorrectPoints,
    }

    try {
      if (isEdit) {
        await api.put(`/admin/experiences/${experienceId}`, payload)
        setSaved(true)
      } else {
        const created = await api.post<Experience>('/admin/experiences', payload)
        setSaved(true)
        navigate(`/admin/experiences/${created.id}/edit`, { replace: true })
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
    return <StatusMessage kind="loading" message="Cargando experiencia..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/admin/experiences">← Volver a Experiencias</Link>
      </div>
      <div className="admin-header">
        <h1>{isEdit ? 'Editar Experiencia' : 'Nueva Experiencia'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Experiencia guardada correctamente." />}

      <div className="tabs">
        <button
          type="button"
          className={`tab-button ${activeTab === 'general' ? 'tab-button--active' : ''}`}
          onClick={() => setActiveTab('general')}
        >
          Datos generales
        </button>
        <button
          type="button"
          className={`tab-button ${activeTab === 'configuracion' ? 'tab-button--active' : ''}`}
          onClick={() => setActiveTab('configuracion')}
        >
          Configuración
        </button>
      </div>

      <form className="form-card" onSubmit={handleSubmit}>
        {activeTab === 'general' && (
          <>
            <div className="form-field">
              <label htmlFor="name">Nombre</label>
              <input id="name" type="text" value={name} onChange={(e) => setName(e.target.value)} />
              {fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}
            </div>

            <div className="form-field">
              <label htmlFor="description">Descripción</label>
              <textarea id="description" value={description} onChange={(e) => setDescription(e.target.value)} />
              {fieldErrors.description && <span className="form-field-error">{fieldErrors.description[0]}</span>}
            </div>

            <div className="form-row">
              <div className="form-field">
                <label htmlFor="primaryColor">Color primario</label>
                <input
                  id="primaryColor"
                  type="text"
                  placeholder="#4a4ad1"
                  value={primaryColor}
                  onChange={(e) => setPrimaryColor(e.target.value)}
                />
                {fieldErrors.primaryColor && (
                  <span className="form-field-error">{fieldErrors.primaryColor[0]}</span>
                )}
              </div>

              <div className="form-field">
                <label htmlFor="secondaryColor">Color secundario</label>
                <input
                  id="secondaryColor"
                  type="text"
                  placeholder="#ffffff"
                  value={secondaryColor}
                  onChange={(e) => setSecondaryColor(e.target.value)}
                />
                {fieldErrors.secondaryColor && (
                  <span className="form-field-error">{fieldErrors.secondaryColor[0]}</span>
                )}
              </div>
            </div>

            <div className="form-field form-checkbox">
              <input
                id="isPublic"
                type="checkbox"
                checked={isPublic}
                onChange={(e) => setIsPublic(e.target.checked)}
              />
              <label htmlFor="isPublic">Pública</label>
            </div>

            {statusLabel && (
              <div className="form-field">
                <label>Estado</label>
                <span>{statusLabel}</span>
              </div>
            )}
          </>
        )}

        {activeTab === 'configuracion' && (
          <>
            <div className="form-field">
              <label htmlFor="defaultExactScorePoints">Puntos por marcador exacto (por defecto)</label>
              <input
                id="defaultExactScorePoints"
                type="number"
                min={0}
                step={1}
                value={defaultExactScorePoints}
                onChange={(e) => setDefaultExactScorePoints(clampNonNegative(e.target.value))}
              />
              {fieldErrors.defaultExactScorePoints && (
                <span className="form-field-error">{fieldErrors.defaultExactScorePoints[0]}</span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="defaultCorrectOutcomePoints">Puntos por resultado correcto (por defecto)</label>
              <input
                id="defaultCorrectOutcomePoints"
                type="number"
                min={0}
                step={1}
                value={defaultCorrectOutcomePoints}
                onChange={(e) => setDefaultCorrectOutcomePoints(clampNonNegative(e.target.value))}
              />
              {fieldErrors.defaultCorrectOutcomePoints && (
                <span className="form-field-error">{fieldErrors.defaultCorrectOutcomePoints[0]}</span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="defaultIncorrectPoints">Puntos por resultado incorrecto (por defecto)</label>
              <input
                id="defaultIncorrectPoints"
                type="number"
                min={0}
                step={1}
                value={defaultIncorrectPoints}
                onChange={(e) => setDefaultIncorrectPoints(clampNonNegative(e.target.value))}
              />
              {fieldErrors.defaultIncorrectPoints && (
                <span className="form-field-error">{fieldErrors.defaultIncorrectPoints[0]}</span>
              )}
            </div>
          </>
        )}

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          <Link to="/admin/experiences" className="btn btn-secondary">
            Cancelar
          </Link>
        </div>
      </form>
    </div>
  )
}
