import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Competition, Edition, LeagueScopeType, LeagueSummary, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function LeagueCreatePage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const preselectedCompetitionId = searchParams.get('competitionId')
    ? Number(searchParams.get('competitionId'))
    : null

  const [competitions, setCompetitions] = useState<Competition[] | null>(null)
  const [preselectedCompetition, setPreselectedCompetition] = useState<Competition | null>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [competitionId, setCompetitionId] = useState<number | ''>(preselectedCompetitionId ?? '')
  const [scopeType, setScopeType] = useState<LeagueScopeType>('FullCompetition')

  const [editions, setEditions] = useState<Edition[]>([])
  const [editionId, setEditionId] = useState<number | ''>('')
  const [rounds, setRounds] = useState<Round[]>([])
  const [roundFromId, setRoundFromId] = useState<number | ''>('')
  const [roundToId, setRoundToId] = useState<number | ''>('')

  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  const cancelTo = preselectedCompetitionId ? `/competitions/${preselectedCompetitionId}` : '/leagues'

  // La Competencia llega preseleccionada (desde el detalle de una Competencia): no se vuelve
  // a pedir, solo se muestra como texto. Si no llega, se mantiene el selector como respaldo.
  useEffect(() => {
    if (preselectedCompetitionId) {
      api
        .get<Competition>(`/competitions/${preselectedCompetitionId}`)
        .then(setPreselectedCompetition)
        .catch((err) => setError(err.message ?? 'No se pudo cargar la Competencia.'))
      return
    }

    api
      .get<Competition[]>('/competitions')
      .then((data) => setCompetitions(data.filter((c) => c.isActive)))
      .catch((err) => setError(err.message ?? 'No se pudieron cargar las Competencias.'))
  }, [preselectedCompetitionId])

  useEffect(() => {
    setEditions([])
    setEditionId('')
    setRounds([])
    setRoundFromId('')
    setRoundToId('')

    if (competitionId === '' || scopeType !== 'RoundRange') return

    api
      .get<Edition[]>(`/competitions/${competitionId}/editions`)
      .then(setEditions)
      .catch((err) => setError(err.message ?? 'No se pudieron cargar las Ediciones.'))
  }, [competitionId, scopeType])

  useEffect(() => {
    setRounds([])
    setRoundFromId('')
    setRoundToId('')

    if (editionId === '') return

    api
      .get<Round[]>(`/editions/${editionId}/rounds`)
      .then((data) => setRounds([...data].sort((a, b) => a.order - b.order)))
      .catch((err) => setError(err.message ?? 'No se pudieron cargar las Fechas.'))
  }, [editionId])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})

    const payload = {
      name,
      description: description || null,
      competitionId: competitionId === '' ? null : competitionId,
      scopeType,
      roundFromId: scopeType === 'RoundRange' && roundFromId !== '' ? roundFromId : null,
      roundToId: scopeType === 'RoundRange' && roundToId !== '' ? roundToId : null,
    }

    try {
      const created = await api.post<LeagueSummary>('/leagues', payload)
      navigate(`/leagues/${created.id}`, { replace: true })
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.fieldErrors)
      } else {
        setError('Ocurrió un error inesperado al crear la Liga.')
      }
      setSaving(false)
    }
  }

  const ready = preselectedCompetitionId ? Boolean(preselectedCompetition) : Boolean(competitions)

  return (
    <div>
      <div className="breadcrumb">
        <Link to={cancelTo}>← Volver</Link>
      </div>
      <div className="admin-header">
        <h1>Crear Liga</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!ready && !error && <StatusMessage kind="loading" message="Cargando..." />}

      {ready && (
        <form className="form-card" onSubmit={handleSubmit}>
          <div className="form-field">
            <label>Competencia</label>
            {preselectedCompetition ? (
              <span>{preselectedCompetition.name}</span>
            ) : (
              <select
                id="competitionId"
                value={competitionId}
                onChange={(e) => setCompetitionId(e.target.value ? Number(e.target.value) : '')}
              >
                <option value="">Seleccionar...</option>
                {competitions?.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            )}
            {fieldErrors.competitionId && <span className="form-field-error">{fieldErrors.competitionId[0]}</span>}
          </div>

          <div className="form-field">
            <label htmlFor="name">Nombre</label>
            <input id="name" type="text" value={name} onChange={(e) => setName(e.target.value)} />
            {fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}
          </div>

          <div className="form-field">
            <label htmlFor="description">Descripción (opcional)</label>
            <textarea id="description" value={description} onChange={(e) => setDescription(e.target.value)} />
            {fieldErrors.description && <span className="form-field-error">{fieldErrors.description[0]}</span>}
          </div>

          <div className="form-field">
            <label htmlFor="scopeType">Alcance</label>
            <select
              id="scopeType"
              value={scopeType}
              onChange={(e) => setScopeType(e.target.value as LeagueScopeType)}
            >
              <option value="FullCompetition">Toda la Competencia</option>
              <option value="RoundRange">Rango de Fechas</option>
            </select>
          </div>

          {scopeType === 'RoundRange' && (
            <>
              <div className="form-field">
                <label htmlFor="editionId">Edición</label>
                <select
                  id="editionId"
                  value={editionId}
                  onChange={(e) => setEditionId(e.target.value ? Number(e.target.value) : '')}
                  disabled={competitionId === ''}
                >
                  <option value="">Seleccionar...</option>
                  {editions.map((ed) => (
                    <option key={ed.id} value={ed.id}>
                      {ed.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-row">
                <div className="form-field">
                  <label htmlFor="roundFromId">Fecha inicial</label>
                  <select
                    id="roundFromId"
                    value={roundFromId}
                    onChange={(e) => setRoundFromId(e.target.value ? Number(e.target.value) : '')}
                    disabled={editionId === ''}
                  >
                    <option value="">Seleccionar...</option>
                    {rounds.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name}
                      </option>
                    ))}
                  </select>
                  {fieldErrors.roundFromId && <span className="form-field-error">{fieldErrors.roundFromId[0]}</span>}
                </div>

                <div className="form-field">
                  <label htmlFor="roundToId">Fecha final</label>
                  <select
                    id="roundToId"
                    value={roundToId}
                    onChange={(e) => setRoundToId(e.target.value ? Number(e.target.value) : '')}
                    disabled={editionId === ''}
                  >
                    <option value="">Seleccionar...</option>
                    {rounds.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            </>
          )}

          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? 'Creando...' : 'Crear Liga'}
            </button>
            <Link to={cancelTo} className="btn btn-secondary">
              Cancelar
            </Link>
          </div>
        </form>
      )}
    </div>
  )
}
