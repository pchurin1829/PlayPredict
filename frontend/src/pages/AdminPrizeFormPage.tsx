import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import {
  PRIZE_AWARD_CRITERIA,
  PRIZE_CRITERIA_LABELS,
  PRIZE_SCOPE_LABELS,
  PRIZE_SCOPE_TYPES,
  PRIZE_TYPES,
  PRIZE_TYPE_LABELS,
  type Competition,
  type Edition,
  type Prize,
  type PrizeAwardCriteria,
  type PrizeScopeType,
  type PrizeType,
  type Round,
} from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function AdminPrizeFormPage() {
  const { prizeId } = useParams()
  const isEdit = Boolean(prizeId)
  const navigate = useNavigate()

  const [competitions, setCompetitions] = useState<Competition[]>([])
  const [editions, setEditions] = useState<Edition[]>([])
  const [rounds, setRounds] = useState<Round[]>([])

  const [competitionId, setCompetitionId] = useState('')
  const [editionId, setEditionId] = useState('')
  const [roundId, setRoundId] = useState('')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [prizeType, setPrizeType] = useState<PrizeType>('Money')
  const [referenceValue, setReferenceValue] = useState('')
  const [sponsorName, setSponsorName] = useState('')
  const [imageUrl, setImageUrl] = useState('')
  const [scopeType, setScopeType] = useState<PrizeScopeType>('Edition')
  const [awardCriteria, setAwardCriteria] = useState<PrizeAwardCriteria>('Position')
  const [positionFrom, setPositionFrom] = useState('')
  const [positionTo, setPositionTo] = useState('')
  const [statusLabel, setStatusLabel] = useState<string | null>(null)
  const [editionLocked, setEditionLocked] = useState(false)

  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    api
      .get<Competition[]>('/competitions')
      .then(setCompetitions)
      .catch(() => setCompetitions([]))
  }, [])

  useEffect(() => {
    if (!isEdit) return
    api
      .get<Prize>(`/admin/prizes/${prizeId}`)
      .then(async (p) => {
        setName(p.name)
        setDescription(p.description ?? '')
        setPrizeType(p.prizeType)
        setReferenceValue(p.referenceValue ?? '')
        setSponsorName(p.sponsorName ?? '')
        setImageUrl(p.imageUrl ?? '')
        setScopeType(p.scopeType)
        setAwardCriteria(p.awardCriteria)
        setPositionFrom(p.positionFrom != null ? String(p.positionFrom) : '')
        setPositionTo(p.positionTo != null ? String(p.positionTo) : '')
        setStatusLabel(p.statusLabel)
        setEditionId(String(p.editionId))
        setRoundId(p.roundId != null ? String(p.roundId) : '')
        setEditionLocked(true)

        const ed = await api.get<Edition>(`/editions/${p.editionId}`)
        setCompetitionId(String(ed.competitionId))
      })
      .catch((err) => setError(err.message ?? 'No se pudo cargar el premio.'))
      .finally(() => setLoading(false))
  }, [prizeId, isEdit])

  useEffect(() => {
    if (!competitionId) {
      setEditions([])
      return
    }
    api
      .get<Edition[]>(`/competitions/${competitionId}/editions`)
      .then(setEditions)
      .catch(() => setEditions([]))
  }, [competitionId])

  useEffect(() => {
    if (!editionId) {
      setRounds([])
      return
    }
    api
      .get<Round[]>(`/editions/${editionId}/rounds`)
      .then(setRounds)
      .catch(() => setRounds([]))
  }, [editionId])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    const payload = {
      roundId: scopeType === 'Round' && roundId ? Number(roundId) : null,
      name,
      description: description || null,
      prizeType,
      referenceValue: referenceValue || null,
      sponsorName: sponsorName || null,
      imageUrl: imageUrl || null,
      scopeType,
      awardCriteria,
      positionFrom: awardCriteria === 'Position' && positionFrom ? Number(positionFrom) : null,
      positionTo: awardCriteria === 'Position' && positionTo ? Number(positionTo) : null,
    }

    try {
      if (isEdit) {
        await api.put(`/admin/prizes/${prizeId}`, payload)
        setSaved(true)
      } else {
        const created = await api.post<Prize>('/admin/prizes', {
          editionId: Number(editionId),
          ...payload,
        })
        setSaved(true)
        navigate(`/admin/prizes/${created.id}/edit`, { replace: true })
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
    return <StatusMessage kind="loading" message="Cargando premio..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/admin/prizes">← Premios</Link>
      </div>
      <div className="admin-header">
        <h1>{isEdit ? 'Editar Premio' : 'Nuevo Premio'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Premio guardado correctamente." />}

      <form className="form-card" onSubmit={handleSubmit}>
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
            <label htmlFor="prizeType">Tipo</label>
            <select id="prizeType" value={prizeType} onChange={(e) => setPrizeType(e.target.value as PrizeType)}>
              {PRIZE_TYPES.map((t) => (
                <option key={t} value={t}>
                  {PRIZE_TYPE_LABELS[t]}
                </option>
              ))}
            </select>
          </div>

          <div className="form-field">
            <label htmlFor="referenceValue">Valor de referencia</label>
            <input
              id="referenceValue"
              type="text"
              value={referenceValue}
              onChange={(e) => setReferenceValue(e.target.value)}
            />
            {fieldErrors.referenceValue && (
              <span className="form-field-error">{fieldErrors.referenceValue[0]}</span>
            )}
          </div>
        </div>

        <div className="form-row">
          <div className="form-field">
            <label htmlFor="sponsorName">Sponsor</label>
            <input
              id="sponsorName"
              type="text"
              value={sponsorName}
              onChange={(e) => setSponsorName(e.target.value)}
            />
            {fieldErrors.sponsorName && <span className="form-field-error">{fieldErrors.sponsorName[0]}</span>}
          </div>

          <div className="form-field">
            <label htmlFor="imageUrl">Imagen (URL)</label>
            <input id="imageUrl" type="text" value={imageUrl} onChange={(e) => setImageUrl(e.target.value)} />
            {fieldErrors.imageUrl && <span className="form-field-error">{fieldErrors.imageUrl[0]}</span>}
          </div>
        </div>

        <div className="form-row">
          <div className="form-field">
            <label htmlFor="competitionId">Competencia</label>
            <select
              id="competitionId"
              value={competitionId}
              disabled={editionLocked}
              onChange={(e) => {
                setCompetitionId(e.target.value)
                setEditionId('')
              }}
            >
              <option value="">Seleccionar...</option>
              {competitions.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>

          <div className="form-field">
            <label htmlFor="editionId">Edición</label>
            <select
              id="editionId"
              value={editionId}
              disabled={editionLocked || !competitionId}
              onChange={(e) => setEditionId(e.target.value)}
            >
              <option value="">Seleccionar...</option>
              {editions.map((ed) => (
                <option key={ed.id} value={ed.id}>
                  {ed.name}
                </option>
              ))}
            </select>
            {fieldErrors.editionId && <span className="form-field-error">{fieldErrors.editionId[0]}</span>}
          </div>
        </div>

        <div className="form-row">
          <div className="form-field">
            <label htmlFor="scopeType">Ámbito</label>
            <select id="scopeType" value={scopeType} onChange={(e) => setScopeType(e.target.value as PrizeScopeType)}>
              {PRIZE_SCOPE_TYPES.map((s) => (
                <option key={s} value={s}>
                  {PRIZE_SCOPE_LABELS[s]}
                </option>
              ))}
            </select>
          </div>

          {scopeType === 'Round' && (
            <div className="form-field">
              <label htmlFor="roundId">Fecha</label>
              <select id="roundId" value={roundId} onChange={(e) => setRoundId(e.target.value)}>
                <option value="">Seleccionar...</option>
                {rounds.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name}
                  </option>
                ))}
              </select>
              {fieldErrors.roundId && <span className="form-field-error">{fieldErrors.roundId[0]}</span>}
            </div>
          )}
        </div>

        <div className="form-field">
          <label htmlFor="awardCriteria">Criterio</label>
          <select
            id="awardCriteria"
            value={awardCriteria}
            onChange={(e) => setAwardCriteria(e.target.value as PrizeAwardCriteria)}
          >
            {PRIZE_AWARD_CRITERIA.map((c) => (
              <option key={c} value={c}>
                {PRIZE_CRITERIA_LABELS[c]}
              </option>
            ))}
          </select>
          {fieldErrors.awardCriteria && <span className="form-field-error">{fieldErrors.awardCriteria[0]}</span>}
        </div>

        {awardCriteria === 'Position' && (
          <div className="form-row">
            <div className="form-field">
              <label htmlFor="positionFrom">Posición desde</label>
              <input
                id="positionFrom"
                type="number"
                min={1}
                value={positionFrom}
                onChange={(e) => setPositionFrom(e.target.value)}
              />
              {fieldErrors.positionFrom && (
                <span className="form-field-error">{fieldErrors.positionFrom[0]}</span>
              )}
            </div>

            <div className="form-field">
              <label htmlFor="positionTo">Posición hasta</label>
              <input
                id="positionTo"
                type="number"
                min={1}
                value={positionTo}
                onChange={(e) => setPositionTo(e.target.value)}
              />
              {fieldErrors.positionTo && <span className="form-field-error">{fieldErrors.positionTo[0]}</span>}
            </div>
          </div>
        )}

        {statusLabel && (
          <div className="form-field">
            <label>Estado</label>
            <span>{statusLabel}</span>
          </div>
        )}

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving || (!isEdit && !editionId)}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          <Link to="/admin/prizes" className="btn btn-secondary">
            Volver a Premios
          </Link>
        </div>
      </form>
    </div>
  )
}
