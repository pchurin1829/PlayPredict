import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AdminOfficialLeague, Competition, Edition, LeagueScopeType, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function AdminOfficialLeagueFormPage() {
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
      }
    }).catch((reason) => setError(reason.message ?? 'No se pudo cargar la Liga Oficial.'))
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
    }
    try {
      if (isEdit) await api.put(`/admin/official-leagues/${leagueId}`, payload)
      else await api.post('/admin/official-leagues', payload)
      navigate('/admin/official-leagues', { replace: true })
    } catch (reason) {
      if (reason instanceof ApiError) { setError(reason.message); setFieldErrors(reason.fieldErrors) }
      else setError('No se pudo guardar la Liga Oficial.')
      setSaving(false)
    }
  }

  if (loading) return <StatusMessage kind="loading" message="Cargando..." />

  return (
    <div>
      <div className="breadcrumb"><Link to="/admin/official-leagues">← Volver a Ligas Oficiales</Link></div>
      <div className="admin-header"><h1>{isEdit ? 'Editar Liga Oficial' : 'Nueva Liga Oficial'}</h1></div>
      <p className="admin-help"><strong>Nombre de Liga Oficial</strong> es la marca pública. La Competencia deportiva y su Edición son la fuente de fechas, partidos y resultados.</p>
      {error && <StatusMessage kind="error" message={error} />}
      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-field"><label htmlFor="officialName">Nombre público/comercial</label><input id="officialName" type="text" placeholder="Ej: COPA EL NENE" value={name} onChange={(e) => setName(e.target.value)} />{fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}</div>
        <div className="form-field"><label htmlFor="officialDescription">Descripción (opcional)</label><textarea id="officialDescription" value={description} onChange={(e) => setDescription(e.target.value)} /></div>
        <div className="form-field"><label htmlFor="officialCompetition">Competencia deportiva fuente</label><select id="officialCompetition" value={competitionId} required onChange={(e) => { setCompetitionId(e.target.value ? Number(e.target.value) : ''); setEditionId(''); setScopeType('FullCompetition'); setRoundFromId(''); setRoundToId('') }}><option value="">Seleccionar...</option>{competitions.map((competition) => <option key={competition.id} value={competition.id}>{competition.name}</option>)}</select>{fieldErrors.competitionId && <span className="form-field-error">{fieldErrors.competitionId[0]}</span>}</div>
        <div className="form-field"><label htmlFor="officialEdition">Edición</label><select id="officialEdition" value={editionId} required disabled={competitionId === ''} onChange={(e) => { setEditionId(e.target.value ? Number(e.target.value) : ''); setRoundFromId(''); setRoundToId('') }}><option value="">Seleccionar...</option>{editions.map((edition) => <option key={edition.id} value={edition.id}>{edition.name}</option>)}</select>{fieldErrors.editionId && <span className="form-field-error">{fieldErrors.editionId[0]}</span>}</div>
        <div className="form-field"><label htmlFor="officialScope">Alcance</label><select id="officialScope" value={scopeType} onChange={(e) => { const next = e.target.value as LeagueScopeType; setScopeType(next); if (next === 'FullCompetition') { setRoundFromId(''); setRoundToId('') } }}><option value="FullCompetition">Toda la Edición</option><option value="RoundRange">Rango de Fechas</option></select></div>
        {scopeType === 'RoundRange' && <div className="form-row"><div className="form-field"><label htmlFor="officialFrom">Desde fecha</label><select id="officialFrom" value={roundFromId} required onChange={(e) => setRoundFromId(e.target.value ? Number(e.target.value) : '')}><option value="">Seleccionar...</option>{rounds.map((round) => <option key={round.id} value={round.id}>{round.name}</option>)}</select>{fieldErrors.roundFromId && <span className="form-field-error">{fieldErrors.roundFromId[0]}</span>}</div><div className="form-field"><label htmlFor="officialTo">Hasta fecha</label><select id="officialTo" value={roundToId} required onChange={(e) => setRoundToId(e.target.value ? Number(e.target.value) : '')}><option value="">Seleccionar...</option>{rounds.map((round) => <option key={round.id} value={round.id}>{round.name}</option>)}</select>{fieldErrors.roundToId && <span className="form-field-error">{fieldErrors.roundToId[0]}</span>}</div></div>}
        <div className="form-field form-checkbox"><input id="officialActive" type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} /><label htmlFor="officialActive">Liga Oficial activa</label></div>
        <div className="form-actions"><button className="btn btn-primary" type="submit" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</button><Link className="btn btn-secondary" to="/admin/official-leagues">Cancelar</Link></div>
      </form>
    </div>
  )
}
