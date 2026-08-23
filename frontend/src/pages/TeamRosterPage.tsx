import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Team, TeamPlayer } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function TeamRosterPage() {
  const { teamId } = useParams(); const [team, setTeam] = useState<Team | null>(null); const [players, setPlayers] = useState<TeamPlayer[] | null>(null); const [error, setError] = useState<string | null>(null)
  useEffect(() => { Promise.all([api.get<Team>(`/teams/${teamId}`), api.get<TeamPlayer[]>(`/teams/${teamId}/players`)]).then(([t,p]) => { setTeam(t); setPlayers(p) }).catch(e => setError(e.message)) }, [teamId])
  return <div><div className="breadcrumb"><Link to="/admin/teams">← Volver a Equipos</Link></div><div className="admin-header"><h1>Plantel {team ? `— ${team.name}` : ''}</h1><Link className="btn btn-primary" to={`/admin/teams/${teamId}/players/new`}>+ Nuevo Jugador</Link></div>
    {error && <StatusMessage kind="error" message={error} />}{!players && !error && <StatusMessage kind="loading" message="Cargando plantel..." />}
    {players && <div className="table-wrap"><table className="admin-table"><thead><tr><th>Nombre</th><th>Número</th><th>Posición</th><th>Estado</th><th /></tr></thead><tbody>{players.map(p => <tr key={p.id}><td><strong>{p.displayName}</strong></td><td>{p.shirtNumber ?? '—'}</td><td>{p.position ?? '—'}</td><td>{p.active ? 'Activo' : 'Inactivo'}</td><td><Link className="btn btn-secondary" to={`/admin/team-players/${p.id}/edit`}>Editar</Link></td></tr>)}</tbody></table></div>}
  </div>
}
