import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { PLAYER_POSITIONS, type AdminOfficialLeague, type Competition, type Edition, type LeagueScopeType, type Round, type PlayerPosition } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { useCompanySettings } from '../company/CompanySettingsContext'

export default function AdminOfficialLeagueFormPage() {
  const { company } = useCompanySettings()
  const companyName = company.shortName || 'PlayPredict'
  const { leagueId } = useParams()
  const isEdit = Boolean(leagueId)
  const navigate = useNavigate()
  const [competitions, setCompetitions] = useState<Competition[]>([])
  const [editions, setEditions] = useState<Edition[]>([])
  const [rounds, setRounds] = useState<Round[]>([])
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [competitionId, setCompetitionId] = useState<number | ''>('')
  const [editionId, setEditionId] = useState<number | ''>('')
  const [scopeType, setScopeType] = useState<LeagueScopeType>('FullCompetition')
  const [roundFromId, setRoundFromId] = useState<number | ''>('')
  const [roundToId, setRoundToId] = useState<number | ''>('')
  const [isActive, setIsActive] = useState(true)
  const [useGeneralScoring,setUseGeneralScoring]=useState(true),[exact,setExact]=useState(6),[correct,setCorrect]=useState(3),[incorrect,setIncorrect]=useState(0)
  const [preferred,setPreferred]=useState(true),[perGoal,setPerGoal]=useState(2),[positions,setPositions]=useState<PlayerPosition[]>(['Mediocampista','Delantero'])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  useEffect(() => {
    Promise.all([
      api.get<Competition[]>('/competitions'),
      isEdit ? api.get<AdminOfficialLeague>(`/admin/official-leagues/${leagueId}`) : Promise.resolve(null),
    ]).then(([allCompetitions, league]) => {
      setCompetitions(allCompetitions)
      if (league) {
        setName(league.name); setDescription(league.description ?? '')
        setCompetitionId(league.competitionId); setEditionId(league.editionId)
        setScopeType(league.scopeType); setRoundFromId(league.roundFromId ?? '')
        setRoundToId(league.roundToId ?? ''); setIsActive(league.isActive)
        setUseGeneralScoring(league.useGeneralScoring);setExact(league.exactScorePoints);setCorrect(league.correctOutcomePoints);setIncorrect(league.incorrectPoints);setPreferred(league.preferredPlayerEnabled);setPerGoal(league.preferredPlayerPointsPerGoal);setPositions(league.preferredPlayerPositions)
      }
    }).catch((reason) => setError(reason.message ?? `No se pudo cargar la competencia ${companyName}.`))
      .finally(() => setLoading(false))
  }, [isEdit, leagueId])

  useEffect(() => {
    if (competitionId === '') { setEditions([]); return }
    api.get<Edition[]>(`/competitions/${competitionId}/editions`).then(setEditions)
      .catch((reason) => setError(reason.message ?? 'No se pudieron cargar las Ediciones.'))
  }, [competitionId])

  useEffect(() => {
    if (editionId === '') { setRounds([]); return }
    api.get<Round[]>(`/editions/${editionId}/rounds`).then((data) => setRounds([...data].sort((a, b) => a.order - b.order)))
      .catch((reason) => setError(reason.message ?? 'No se pudieron cargar las Fechas.'))
  }, [editionId])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault(); setSaving(true); setError(null); setFieldErrors({})
    if (scopeType === 'RoundRange') {
      const from = rounds.find((round) => round.id === roundFromId)
      const to = rounds.find((round) => round.id === roundToId)
      if (!from || !to) {
        setFieldErrors({ roundFromId: ['Debés seleccionar la Fecha inicial y final.'] }); setSaving(false); return
      }
      if (from.order > to.order) {
        setFieldErrors({ roundFromId: ['La Fecha inicial no puede ser posterior a la final.'] }); setSaving(false); return
      }
    }
    const payload = {
      name, description: description || null, competitionId, editionId, scopeType,
      roundFromId: scopeType === 'RoundRange' ? roundFromId || null : null,
      roundToId: scopeType === 'RoundRange' ? roundToId || null : null,
      isActive,
      useGeneralScoring, exactScorePoints:exact, correctOutcomePoints:correct, incorrectPoints:incorrect,
      preferredPlayerEnabled:preferred, preferredPlayerPointsPerGoal:perGoal, preferredPlayerPositions:positions,
    }
    try {
      if (isEdit) await api.put(`/admin/official-leagues/${leagueId}`, payload)
      else await api.post('/admin/official-leagues', payload)
      navigate('/admin/official-leagues', { replace: true })
    } catch (reason) {
      if (reason instanceof ApiError) { setError(reason.message); setFieldErrors(reason.fieldErrors) }
      else setError(`No se pudo guardar la competencia ${companyName}.`)
      setSaving(false)
    }
  }

  if (loading) return <StatusMessage kind="loading" message="Cargando..." />

  return (
    <div>
      <div className="breadcrumb"><Link to="/admin/official-leagues">← Volver a Competencias {companyName}</Link></div>
      <div className="admin-header"><h1>{isEdit ? `Editar Competencia ${companyName}` : `Nueva Competencia ${companyName}`}</h1></div>
      <p className="admin-help">Esta competencia tendrá identidad propia, pero utilizará el fixture y los resultados de la competencia de referencia seleccionada.</p>
      {error && <StatusMessage kind="error" message={error} />}
      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-field"><label htmlFor="officialName">Nombre</label><input id="officialName" type="text" placeholder={`Ej: COPA ${companyName}`} value={name} onChange={(e) => setName(e.target.value)} /><span className="form-field-hint">Ejemplo: COPA {companyName}, basada en Copa Libertadores 2026, Fecha 1 a Fecha 5.</span>{fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}</div>
        <div className="form-field"><label htmlFor="officialDescription">Descripción (opcional)</label><textarea id="officialDescription" value={description} onChange={(e) => setDescription(e.target.value)} /></div>
        <div className="form-field"><label htmlFor="officialCompetition">Competencia de referencia</label><select id="officialCompetition" value={competitionId} required onChange={(e) => { setCompetitionId(e.target.value ? Number(e.target.value) : ''); setEditionId(''); setScopeType('FullCompetition'); setRoundFromId(''); setRoundToId('') }}><option value="">Seleccionar competencia de referencia...</option>{competitions.map((competition) => <option key={competition.id} value={competition.id}>{competition.name}</option>)}</select>{fieldErrors.competitionId && <span className="form-field-error">{fieldErrors.competitionId[0]}</span>}</div>
        <div className="form-field"><label htmlFor="officialEdition">Edición</label><select id="officialEdition" value={editionId} required disabled={competitionId === ''} onChange={(e) => { setEditionId(e.target.value ? Number(e.target.value) : ''); setRoundFromId(''); setRoundToId('') }}><option value="">Seleccionar...</option>{editions.map((edition) => <option key={edition.id} value={edition.id}>{edition.name}</option>)}</select>{fieldErrors.editionId && <span className="form-field-error">{fieldErrors.editionId[0]}</span>}</div>
        <div className="form-field"><label htmlFor="officialScope">Alcance</label><select id="officialScope" value={scopeType} onChange={(e) => { const next = e.target.value as LeagueScopeType; setScopeType(next); if (next === 'FullCompetition') { setRoundFromId(''); setRoundToId('') } }}><option value="FullCompetition">Toda la Edición</option><option value="RoundRange">Rango de Fechas</option></select></div>
        {scopeType === 'RoundRange' && <div className="form-row"><div className="form-field"><label htmlFor="officialFrom">Desde fecha</label><select id="officialFrom" value={roundFromId} required onChange={(e) => setRoundFromId(e.target.value ? Number(e.target.value) : '')}><option value="">Seleccionar...</option>{rounds.map((round) => <option key={round.id} value={round.id}>{round.name}</option>)}</select>{fieldErrors.roundFromId && <span className="form-field-error">{fieldErrors.roundFromId[0]}</span>}</div><div className="form-field"><label htmlFor="officialTo">Hasta fecha</label><select id="officialTo" value={roundToId} required onChange={(e) => setRoundToId(e.target.value ? Number(e.target.value) : '')}><option value="">Seleccionar...</option>{rounds.map((round) => <option key={round.id} value={round.id}>{round.name}</option>)}</select>{fieldErrors.roundToId && <span className="form-field-error">{fieldErrors.roundToId[0]}</span>}</div></div>}
        <div className="form-field form-checkbox"><input id="officialActive" type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} /><label htmlFor="officialActive">Competencia activa</label></div>
        <section className="scoring-special-section"><h2>Configuración de juego</h2><label className="form-field form-checkbox"><input type="checkbox" checked={useGeneralScoring} onChange={e=>setUseGeneralScoring(e.target.checked)}/><span>Usar configuración general</span></label>{useGeneralScoring&&<p className="form-help">Se aplicarán siempre los valores generales vigentes de la empresa.</p>}<fieldset disabled={useGeneralScoring}><legend>Configuración propia</legend><div className="form-row"><label className="form-field">Marcador exacto<input type="number" min="0" value={exact} onChange={e=>setExact(Math.max(0,Number(e.target.value)))}/></label><label className="form-field">Resultado correcto<input type="number" min="0" value={correct} onChange={e=>setCorrect(Math.max(0,Number(e.target.value)))}/></label><label className="form-field">Incorrecto<input type="number" min="0" value={incorrect} onChange={e=>setIncorrect(Math.max(0,Number(e.target.value)))}/></label></div><label className="form-field form-checkbox"><input type="checkbox" checked={preferred} onChange={e=>setPreferred(e.target.checked)}/><span>Jugador Preferido habilitado</span></label><label className="form-field">Puntos por gol<input type="number" min="0" disabled={!preferred} value={perGoal} onChange={e=>setPerGoal(Math.max(0,Number(e.target.value)))}/></label><div className="scoring-position-options">{PLAYER_POSITIONS.map(position=><label className="form-checkbox" key={position}><input type="checkbox" disabled={!preferred} checked={positions.includes(position)} onChange={e=>setPositions(current=>e.target.checked?[...current,position]:current.filter(x=>x!==position))}/><span>{position}</span></label>)}</div></fieldset></section>
        <div className="form-actions"><button className="btn btn-primary" type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</button><Link className="btn btn-secondary" to="/admin/official-leagues">Cancelar</Link></div>
      </form>
    </div>
  )
}
