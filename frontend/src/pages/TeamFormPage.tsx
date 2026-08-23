import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Team } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function TeamFormPage() {
  const { teamId } = useParams(); const edit = Boolean(teamId); const navigate = useNavigate()
  const [name, setName] = useState(''); const [shortName, setShortName] = useState(''); const [logoUrl, setLogoUrl] = useState(''); const [sport, setSport] = useState('Fútbol'); const [active, setActive] = useState(true)
  const [error, setError] = useState<string | null>(null); const [saving, setSaving] = useState(false)
  useEffect(() => { if (edit) api.get<Team>(`/teams/${teamId}`).then(t => { setName(t.name); setShortName(t.shortName); setLogoUrl(t.logoUrl ?? ''); setSport(t.sport); setActive(t.active) }).catch(e => setError(e.message)) }, [edit, teamId])
  async function submit(e: FormEvent) { e.preventDefault(); setSaving(true); setError(null); try { const body = { name, shortName, logoUrl: logoUrl || null, sport, active }; if (edit) await api.put(`/teams/${teamId}`, body); else await api.post('/teams', body); navigate('/admin/teams') } catch (e) { setError(e instanceof ApiError ? e.message : 'No se pudo guardar el equipo.') } finally { setSaving(false) } }
  return <div><div className="breadcrumb"><Link to="/admin/teams">← Volver a Equipos</Link></div><div className="admin-header"><h1>{edit ? 'Editar Equipo' : 'Nuevo Equipo'}</h1></div>{error && <StatusMessage kind="error" message={error} />}<form className="form-card" onSubmit={submit}>
    <div className="form-row"><div className="form-field"><label htmlFor="teamName">Nombre</label><input id="teamName" value={name} onChange={e => setName(e.target.value)} required /></div><div className="form-field"><label htmlFor="shortName">Nombre corto</label><input id="shortName" value={shortName} onChange={e => setShortName(e.target.value)} required /></div></div>
    <div className="form-row"><div className="form-field"><label htmlFor="sport">Deporte</label><input id="sport" value={sport} onChange={e => setSport(e.target.value)} required /></div><div className="form-field"><label htmlFor="logoUrl">URL de logo (opcional)</label><input id="logoUrl" value={logoUrl} onChange={e => setLogoUrl(e.target.value)} /></div></div>
    <label className="checkbox-label"><input type="checkbox" checked={active} onChange={e => setActive(e.target.checked)} /> Equipo activo</label>
    <div className="form-actions"><button className="btn btn-primary" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</button><Link className="btn btn-secondary" to="/admin/teams">Cancelar</Link></div>
  </form></div>
}
