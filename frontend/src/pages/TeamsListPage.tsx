import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Team } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import ConfirmModal from '../components/ConfirmModal'

function TeamIdentity({ team }: { team: Team }) {
  return <span className="team-list-identity">
    {team.logoUrl
      ? <img src={team.logoUrl} alt="" />
      : <span className="team-list-identity__placeholder" aria-hidden="true">{team.shortName.slice(0, 2).toUpperCase()}</span>}
    <strong>{team.name}</strong>
  </span>
}

export default function TeamsListPage() {
  const [teams, setTeams] = useState<Team[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Team | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  useEffect(() => { api.get<Team[]>('/teams').then(setTeams).catch((e) => setError(e.message)) }, [])

  async function deleteTeam() {
    if (!deleteTarget) return
    setDeleting(true); setError(null); setMessage(null); setDeleteError(null)
    try {
      await api.del(`/teams/${deleteTarget.id}`)
      setTeams((current) => current?.filter((team) => team.id !== deleteTarget.id) ?? current)
      setMessage('Equipo eliminado correctamente.')
      setDeleteTarget(null)
    } catch (reason) {
      const failure = reason instanceof ApiError ? reason.message : 'No se pudo eliminar el equipo.'
      setError(failure); setDeleteError(failure)
    } finally {
      setDeleting(false)
    }
  }
  return <div>
    <div className="admin-header"><div><h1>Equipos</h1><p className="admin-help">Catálogo de equipos disponibles para el fixture.</p></div><div className="match-row-actions"><Link className="btn btn-secondary" to="/admin/teams/import">Importar Equipos y Planteles</Link><Link className="btn btn-primary" to="/admin/teams/new">+ Nuevo Equipo</Link></div></div>
    {error && <StatusMessage kind="error" message={error} />}
    {message && <StatusMessage kind="success" message={message} />}
    {!teams && !error && <StatusMessage kind="loading" message="Cargando equipos..." />}
    {teams && <div className="table-wrap"><table className="admin-table"><thead><tr><th>Equipo</th><th>Nombre corto</th><th>Deporte</th><th>Estado</th><th /></tr></thead><tbody>{teams.map((team) => <tr key={team.id}><td><TeamIdentity team={team} /></td><td>{team.shortName}</td><td>{team.sport}</td><td>{team.active ? 'Activo' : 'Inactivo'}</td><td><div className="match-row-actions"><Link className="btn btn-secondary" to={`/admin/teams/${team.id}/edit`}>Editar</Link><Link className="btn btn-secondary" to={`/admin/teams/${team.id}/players`}>Plantel</Link><button type="button" className="btn btn-danger" onClick={() => setDeleteTarget(team)}>Eliminar</button></div></td></tr>)}</tbody></table></div>}
    <ConfirmModal open={Boolean(deleteTarget)} title="Eliminar equipo" message={`¿Eliminar ${deleteTarget?.name ?? 'este equipo'}?\nSólo se eliminará si no tiene partidos, fixture, plantel ni otras dependencias.${deleteError ? `\n\n${deleteError}` : ''}`} confirmLabel={deleting ? 'Eliminando...' : deleteError ? 'Reintentar' : 'Eliminar'} onConfirm={deleteTeam} onCancel={() => { setDeleteTarget(null); setDeleteError(null) }} />
  </div>
}
