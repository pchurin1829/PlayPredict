import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Competition, Edition, LeagueScopeType, LeagueSummary, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

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

  const cancelTo = '/competitions/explore'

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

    if (scopeType === 'RoundRange' && roundFromId !== '' && roundToId !== '') {
      const fromRound = rounds.find((r) => r.id === roundFromId)
      const toRound = rounds.find((r) => r.id === roundToId)
      if (fromRound && toRound && fromRound.order > toRound.order) {
        setError('La Fecha inicial no puede ser posterior a la Fecha final.')
        setFieldErrors({ roundFromId: ['La Fecha inicial no puede ser posterior a la Fecha final.'] })
        setSaving(false)
        return
      }
    }

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
      <Link to={cancelTo} className="pp-back">← Volver</Link>

      <div className="pp-header">
        <h1>Crear Liga</h1>
        <p className="pp-header__subtitle">Estás creando una Liga privada para jugar con amigos utilizando los partidos de esta competencia. No estás creando una nueva Competencia deportiva.</p>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!ready && !error && <StatusMessage kind="loading" message="Cargando..." />}

      {ready && (
        <form className="pp-form" onSubmit={handleSubmit}>
          {/* Competition */}
          <div className="pp-form__field">
            <label className="pp-form__label">Competencia</label>
            {preselectedCompetition ? (
              <span style={{ fontWeight: 600, color: 'var(--color-primary)' }}>⚽ {preselectedCompetition.name}</span>
            ) : (
              <select
                className="pp-form__select"
                value={competitionId}
                onChange={(e) => setCompetitionId(e.target.value ? Number(e.target.value) : '')}
              >
                <option value="">Seleccionar...</option>
                {competitions?.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            )}
            {fieldErrors.competitionId && <span className="pp-form__error">{fieldErrors.competitionId[0]}</span>}
          </div>

          {/* Name */}
          <div className="pp-form__field">
            <label className="pp-form__label" htmlFor="name">Nombre de la Liga</label>
            <input
              id="name"
              className="pp-form__input"
              type="text"
              placeholder="Ej: Liga de los viernes"
              value={name}
              onChange={(e) => setName(e.target.value)}
            />
            {fieldErrors.name && <span className="pp-form__error">{fieldErrors.name[0]}</span>}
          </div>

          {/* Description */}
          <div className="pp-form__field">
            <label className="pp-form__label" htmlFor="description">Descripción (opcional)</label>
            <textarea
              id="description"
              className="pp-form__textarea"
              placeholder="Contale a tus amigos de qué trata esta Liga..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
            {fieldErrors.description && <span className="pp-form__error">{fieldErrors.description[0]}</span>}
          </div>

          {/* Scope selector — visual cards */}
          <div className="pp-form__field">
            <label className="pp-form__label">Alcance de la Liga</label>
          </div>
          <div className="pp-scope-selector">
            <button
              type="button"
              className={`pp-scope-option ${scopeType === 'FullCompetition' ? 'pp-scope-option--selected' : ''}`}
              onClick={() => setScopeType('FullCompetition')}
            >
              <span className="pp-scope-option__icon">🏆</span>
              <p className="pp-scope-option__title">Toda la Competencia</p>
              <p className="pp-scope-option__desc">Los jugadores pronostican todos los partidos de la edición</p>
            </button>
            <button
              type="button"
              className={`pp-scope-option ${scopeType === 'RoundRange' ? 'pp-scope-option--selected' : ''}`}
              onClick={() => setScopeType('RoundRange')}
            >
              <span className="pp-scope-option__icon">📅</span>
              <p className="pp-scope-option__title">Rango de Fechas</p>
              <p className="pp-scope-option__desc">Elegí una o más fechas específicas de la edición</p>
            </button>
          </div>

          {/* Round range fields */}
          {scopeType === 'RoundRange' && (
            <>
              <div className="pp-form__field">
                <label className="pp-form__label" htmlFor="editionId">Torneo / Edición</label>
                <select
                  id="editionId"
                  className="pp-form__select"
                  value={editionId}
                  onChange={(e) => setEditionId(e.target.value ? Number(e.target.value) : '')}
                  disabled={competitionId === ''}
                >
                  <option value="">Seleccionar...</option>
                  {editions.map((ed) => (
                    <option key={ed.id} value={ed.id}>{ed.name}</option>
                  ))}
                </select>
              </div>

              <div className="pp-form__row">
                <div className="pp-form__field">
                  <label className="pp-form__label" htmlFor="roundFromId">Fecha inicial</label>
                  <select
                    id="roundFromId"
                    className="pp-form__select"
                    value={roundFromId}
                    onChange={(e) => setRoundFromId(e.target.value ? Number(e.target.value) : '')}
                    disabled={editionId === ''}
                  >
                    <option value="">Seleccionar...</option>
                    {rounds.map((r) => (
                      <option key={r.id} value={r.id}>{r.name}</option>
                    ))}
                  </select>
                  {fieldErrors.roundFromId && <span className="pp-form__error">{fieldErrors.roundFromId[0]}</span>}
                </div>

                <div className="pp-form__field">
                  <label className="pp-form__label" htmlFor="roundToId">Fecha final</label>
                  <select
                    id="roundToId"
                    className="pp-form__select"
                    value={roundToId}
                    onChange={(e) => setRoundToId(e.target.value ? Number(e.target.value) : '')}
                    disabled={editionId === ''}
                  >
                    <option value="">Seleccionar...</option>
                    {rounds.map((r) => (
                      <option key={r.id} value={r.id}>{r.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              <p className="pp-form__hint">
                Podés elegir la misma fecha como inicial y final para crear una Liga de una sola fecha.
              </p>
            </>
          )}

          <div className="pp-form__actions">
            <button type="submit" className="pp-btn pp-btn--primary" disabled={saving}>
              {saving ? 'Creando...' : 'Crear Liga'}
            </button>
            <Link to={cancelTo} className="pp-btn pp-btn--secondary">
              Cancelar
            </Link>
          </div>
        </form>
      )}
    </div>
  )
}
