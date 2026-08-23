import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Team } from '../api/types'
import StatusMessage from '../components/StatusMessage'

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
  useEffect(() => { api.get<Team[]>('/teams').then(setTeams).catch((e) => setError(e.message)) }, [])
  return <div>
    <div className="admin-header"><div><h1>Equipos</h1><p className="admin-help">Catálogo de equipos disponibles para el fixture.</p></div><Link className="btn btn-primary" to="/admin/teams/new">+ Nuevo Equipo</Link></div>
    {error && <StatusMessage kind="error" message={error} />}
    {!teams && !error && <StatusMessage kind="loading" message="Cargando equipos..." />}
    {teams && <div className="table-wrap"><table className="admin-table"><thead><tr><th>Equipo</th><th>Nombre corto</th><th>Deporte</th><th>Estado</th><th /></tr></thead><tbody>{teams.map((team) => <tr key={team.id}><td><TeamIdentity team={team} /></td><td>{team.shortName}</td><td>{team.sport}</td><td>{team.active ? 'Activo' : 'Inactivo'}</td><td><div className="match-row-actions"><Link className="btn btn-secondary" to={`/admin/teams/${team.id}/edit`}>Editar</Link><Link className="btn btn-secondary" to={`/admin/teams/${team.id}/players`}>Plantel</Link></div></td></tr>)}</tbody></table></div>}
  </div>
}
