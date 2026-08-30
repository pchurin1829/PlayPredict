import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { LEAGUE_SCOPE_LABELS, type LeagueScopeType, type LeagueSummary, type Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { resolveLeagueCreateReturnTo } from '../utils/leagueCreateReturnTo'
import './PlayerPages.css'

export default function LeagueCreatePage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const officialLeagueId = Number(searchParams.get('officialLeagueId'))
  const returnContext = resolveLeagueCreateReturnTo(searchParams.get('returnTo'))
  const [officialLeague, setOfficialLeague] = useState<LeagueSummary | null>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [scopeType, setScopeType] = useState<LeagueScopeType>('FullCompetition')
  const [availableRounds, setAvailableRounds] = useState<Round[]>([])
  const [roundFromId, setRoundFromId] = useState<number | ''>('')
  const [roundToId, setRoundToId] = useState<number | ''>('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  useEffect(() => {
    if (!Number.isInteger(officialLeagueId) || officialLeagueId <= 0) {
      setError('Elegí primero una Competencia Oficial PlayPredict.')
      return
    }
    api.get<LeagueSummary[]>('/leagues/officials')
      .then((leagues) => {
        const source = leagues.find((league) => league.id === officialLeagueId)
        if (!source) throw new Error('La Competencia Oficial seleccionada no está disponible.')
        setOfficialLeague(source)
      })
      .catch((reason) => setError(reason.message ?? 'No se pudo cargar la Competencia Oficial.'))
  }, [officialLeagueId])

  useEffect(() => {
    if (!officialLeague) return
    api.get<Round[]>(`/editions/${officialLeague.editionId}/rounds`)
      .then((rounds) => {
        const ordered = [...rounds].sort((a, b) => a.order - b.order)
        if (officialLeague.scopeType !== 'RoundRange') {
          setAvailableRounds(ordered)
          return
        }
        const from = ordered.find((round) => round.id === officialLeague.roundFromId)
        const to = ordered.find((round) => round.id === officialLeague.roundToId)
        setAvailableRounds(from && to ? ordered.filter((round) => round.order >= from.order && round.order <= to.order) : [])
      })
      .catch((reason) => setError(reason.message ?? 'No se pudieron cargar las Fechas habilitadas.'))
  }, [officialLeague])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      const created = await api.post<LeagueSummary>('/leagues', {
        name,
        description: description || null,
        officialLeagueId,
        scopeType,
        roundFromId: scopeType === 'RoundRange' && roundFromId !== '' ? roundFromId : null,
        roundToId: scopeType === 'RoundRange' && roundToId !== '' ? roundToId : null,
      })
      navigate(`/leagues/${created.id}`, { replace: true })
    } catch (reason) {
      if (reason instanceof ApiError) {
        setError(reason.message)
        setFieldErrors(reason.fieldErrors)
      } else setError('Ocurrió un error inesperado al crear la Liga de Amigos.')
      setSaving(false)
    }
  }

  const rangeLabel = officialLeague?.scopeType === 'RoundRange'
    ? `${officialLeague.roundFromName ?? 'Fecha inicial'} → ${officialLeague.roundToName ?? 'Fecha final'}`
    : null
  const selectedFrom = availableRounds.find((round) => round.id === roundFromId)
  const selectedTo = availableRounds.find((round) => round.id === roundToId)
  const roundsForFrom = selectedTo
    ? availableRounds.filter((round) => round.order <= selectedTo.order)
    : availableRounds
  const roundsForTo = selectedFrom
    ? availableRounds.filter((round) => round.order >= selectedFrom.order)
    : availableRounds

  return <div>
    <Link to={returnContext.path} className="pp-back">← Volver{returnContext.label ? ` a ${returnContext.label}` : ''}</Link>
    <div className="pp-header">
      <h1>Crear Liga de Amigos</h1>
      <p className="pp-header__subtitle">Tu Liga heredará el fixture, alcance y reglas actuales de la Competencia Oficial elegida.</p>
    </div>
    {error && <StatusMessage kind="error" message={error} />}
    {!officialLeague && !error && <StatusMessage kind="loading" message="Cargando Competencia Oficial..." />}
    {officialLeague && <>
      <div className="pp-info-card">
        <h2 className="pp-info-card__title">Fuente oficial: {officialLeague.name}</h2>
        <div className="pp-info-card__meta">
          <span className="pp-info-card__meta-item">Referencia deportiva: {officialLeague.competitionName} · {officialLeague.editionName}</span>
          <span className="pp-info-card__meta-item">Alcance: {LEAGUE_SCOPE_LABELS[officialLeague.scopeType]}{rangeLabel ? ` (${rangeLabel})` : ''}</span>
        </div>
      </div>
      <form className="pp-form" onSubmit={handleSubmit}>
        <div className="pp-form__field">
          <label className="pp-form__label" htmlFor="name">Nombre</label>
          <input id="name" className="pp-form__input" type="text" placeholder="Ej: Liga de los viernes" value={name} onChange={(event) => setName(event.target.value)} />
          {fieldErrors.name && <span className="pp-form__error">{fieldErrors.name[0]}</span>}
        </div>
        <div className="pp-form__field">
          <label className="pp-form__label" htmlFor="description">Descripción (opcional)</label>
          <textarea id="description" className="pp-form__textarea" placeholder="Contale a tus amigos de qué trata esta Liga..." value={description} onChange={(event) => setDescription(event.target.value)} />
          {fieldErrors.description && <span className="pp-form__error">{fieldErrors.description[0]}</span>}
        </div>
        <fieldset className="pp-form__field">
          <legend className="pp-form__label">Alcance</legend>
          <label className="form-checkbox">
            <input type="radio" name="scopeType" checked={scopeType === 'FullCompetition'} onChange={() => setScopeType('FullCompetition')} />
            <span>Todas las fechas de {officialLeague.name}</span>
          </label>
          <label className="form-checkbox">
            <input type="radio" name="scopeType" checked={scopeType === 'RoundRange'} onChange={() => setScopeType('RoundRange')} />
            <span>Solo algunas fechas</span>
          </label>
        </fieldset>
        {scopeType === 'RoundRange' && <div className="pp-form__row">
          <div className="pp-form__field">
            <label className="pp-form__label" htmlFor="roundFromId">Desde</label>
            <select id="roundFromId" className="pp-form__select" value={roundFromId} onChange={(event) => setRoundFromId(event.target.value ? Number(event.target.value) : '')} required>
              <option value="">Seleccionar...</option>
              {roundsForFrom.map((round) => <option key={round.id} value={round.id}>{round.name}</option>)}
            </select>
            {fieldErrors.roundFromId && <span className="pp-form__error">{fieldErrors.roundFromId[0]}</span>}
          </div>
          <div className="pp-form__field">
            <label className="pp-form__label" htmlFor="roundToId">Hasta</label>
            <select id="roundToId" className="pp-form__select" value={roundToId} onChange={(event) => setRoundToId(event.target.value ? Number(event.target.value) : '')} required>
              <option value="">Seleccionar...</option>
              {roundsForTo.map((round) => <option key={round.id} value={round.id}>{round.name}</option>)}
            </select>
          </div>
        </div>}
        {fieldErrors.officialLeagueId && <span className="pp-form__error">{fieldErrors.officialLeagueId[0]}</span>}
        <div className="pp-form__actions pp-form__actions--league-create">
          <button type="submit" className="pp-btn pp-btn--primary" disabled={saving}>{saving ? 'Creando...' : 'Crear Liga'}</button>
          <Link to={returnContext.path} className="pp-btn pp-btn--secondary">Cancelar</Link>
        </div>
      </form>
    </>}
  </div>
}
