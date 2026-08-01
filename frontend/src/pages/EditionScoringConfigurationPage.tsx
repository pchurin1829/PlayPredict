import { useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Edition, EditionScoringConfiguration } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function EditionScoringConfigurationPage() {
  const { editionId } = useParams()

  const [edition, setEdition] = useState<Edition | null>(null)
  const [exactScorePoints, setExactScorePoints] = useState(0)
  const [correctOutcomePoints, setCorrectOutcomePoints] = useState(0)
  const [incorrectPoints, setIncorrectPoints] = useState(0)
  const [useExperienceDefaults, setUseExperienceDefaults] = useState(false)
  const [effective, setEffective] = useState<{ exact: number; correct: number; incorrect: number } | null>(null)

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    Promise.all([
      api.get<Edition>(`/editions/${editionId}`),
      api.get<EditionScoringConfiguration>(`/editions/${editionId}/scoring-configuration`),
    ])
      .then(([ed, cfg]) => {
        setEdition(ed)
        setExactScorePoints(cfg.exactScorePoints)
        setCorrectOutcomePoints(cfg.correctOutcomePoints)
        setIncorrectPoints(cfg.incorrectPoints)
        setUseExperienceDefaults(cfg.useExperienceDefaults)
        setEffective({
          exact: cfg.effectiveExactScorePoints,
          correct: cfg.effectiveCorrectOutcomePoints,
          incorrect: cfg.effectiveIncorrectPoints,
        })
      })
      .catch((err) => setError(err.message ?? 'No se pudo cargar la configuración de puntuación.'))
      .finally(() => setLoading(false))
  }, [editionId])

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

    try {
      const updated = await api.put<EditionScoringConfiguration>(`/editions/${editionId}/scoring-configuration`, {
        exactScorePoints,
        correctOutcomePoints,
        incorrectPoints,
        useExperienceDefaults,
      })
      setEffective({
        exact: updated.effectiveExactScorePoints,
        correct: updated.effectiveCorrectOutcomePoints,
        incorrect: updated.effectiveIncorrectPoints,
      })
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
    return <StatusMessage kind="loading" message="Cargando configuración de puntuación..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        {edition && (
          <Link to={`/competitions/${edition.competitionId}/editions`}>← Ediciones</Link>
        )}
      </div>
      <div className="admin-header">
        <h1>Configurar puntuación {edition ? `— ${edition.name}` : ''}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Configuración guardada correctamente." />}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-field form-checkbox">
          <input
            id="useExperienceDefaults"
            type="checkbox"
            checked={useExperienceDefaults}
            onChange={(e) => setUseExperienceDefaults(e.target.checked)}
          />
          <label htmlFor="useExperienceDefaults">Usar configuración de la Experience</label>
        </div>

        {useExperienceDefaults && effective && (
          <div className="empty-state">
            Se aplicarán los valores de la Experience: {effective.exact} / {effective.correct} /{' '}
            {effective.incorrect} (exacto / correcto / incorrecto).
          </div>
        )}

        <div className="form-field">
          <label htmlFor="exactScorePoints">Puntos por marcador exacto (configuración propia)</label>
          <input
            id="exactScorePoints"
            type="number"
            min={0}
            step={1}
            disabled={useExperienceDefaults}
            value={exactScorePoints}
            onChange={(e) => setExactScorePoints(clampNonNegative(e.target.value))}
          />
          {fieldErrors.exactScorePoints && (
            <span className="form-field-error">{fieldErrors.exactScorePoints[0]}</span>
          )}
        </div>

        <div className="form-field">
          <label htmlFor="correctOutcomePoints">Puntos por resultado correcto (configuración propia)</label>
          <input
            id="correctOutcomePoints"
            type="number"
            min={0}
            step={1}
            disabled={useExperienceDefaults}
            value={correctOutcomePoints}
            onChange={(e) => setCorrectOutcomePoints(clampNonNegative(e.target.value))}
          />
          {fieldErrors.correctOutcomePoints && (
            <span className="form-field-error">{fieldErrors.correctOutcomePoints[0]}</span>
          )}
        </div>

        <div className="form-field">
          <label htmlFor="incorrectPoints">Puntos por resultado incorrecto (configuración propia)</label>
          <input
            id="incorrectPoints"
            type="number"
            min={0}
            step={1}
            disabled={useExperienceDefaults}
            value={incorrectPoints}
            onChange={(e) => setIncorrectPoints(clampNonNegative(e.target.value))}
          />
          {fieldErrors.incorrectPoints && (
            <span className="form-field-error">{fieldErrors.incorrectPoints[0]}</span>
          )}
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          {edition && (
            <Link to={`/competitions/${edition.competitionId}/editions`} className="btn btn-secondary">
              Volver a Ediciones
            </Link>
          )}
        </div>
      </form>
    </div>
  )
}
