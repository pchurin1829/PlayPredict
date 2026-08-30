import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { PLAYER_POSITIONS, type PlayerPosition, type Team, type TeamPlayer } from '../api/types'
import ConfirmModal from '../components/ConfirmModal'
import StatusMessage from '../components/StatusMessage'

function PlayerAvatar({ player }: { player: TeamPlayer }) {
  const initials = `${player.firstName[0] ?? ''}${player.lastName[0] ?? ''}`.toUpperCase()
  return <span className="player-avatar">{player.photoUrl ? <img src={player.photoUrl} alt={`Foto de ${player.displayName}`} /> : initials}</span>
}

function PlayerName({ player }: { player: TeamPlayer }) {
  const name = `${player.firstName} ${player.lastName}`.trim()
  const nickname = player.displayName.toLocaleLowerCase() === name.toLocaleLowerCase() ? null : player.displayName
  return <span className="player-admin-name"><strong>{name}</strong>{nickname && <small>“{nickname}”</small>}</span>
}

type RosterSort = 'alphabetical' | 'position'

const playerNameCollator = new Intl.Collator('es', { sensitivity: 'base', numeric: true })

function comparePlayerNames(left: TeamPlayer, right: TeamPlayer) {
  return playerNameCollator.compare(left.lastName.trim(), right.lastName.trim())
    || playerNameCollator.compare(left.firstName.trim(), right.firstName.trim())
    || left.id - right.id
}

function positionOrder(position: string | null) {
  const index = PLAYER_POSITIONS.indexOf(position as PlayerPosition)
  return index === -1 ? PLAYER_POSITIONS.length : index
}

export default function TeamRosterPage() {
  const { teamId } = useParams(); const [team, setTeam] = useState<Team | null>(null); const [players, setPlayers] = useState<TeamPlayer[] | null>(null); const [sortBy, setSortBy] = useState<RosterSort>('alphabetical'); const [deleteTarget, setDeleteTarget] = useState<TeamPlayer | null>(null); const [error, setError] = useState<string | null>(null); const [message, setMessage] = useState<string | null>(null)
  useEffect(() => { Promise.all([api.get<Team>(`/teams/${teamId}`), api.get<TeamPlayer[]>(`/teams/${teamId}/players`)]).then(([t,p]) => { setTeam(t); setPlayers(p) }).catch(e => setError(e.message)) }, [teamId])
  async function removePlayer() { if (!deleteTarget) return; setError(null); try { await api.del(`/team-players/${deleteTarget.id}`); setPlayers(current => current?.filter(p => p.id !== deleteTarget.id) ?? null); setMessage('Jugador eliminado correctamente.') } catch (reason) { setError(reason instanceof ApiError ? reason.message : 'No se pudo eliminar el jugador.') } finally { setDeleteTarget(null) } }
  const sortedPlayers = useMemo(() => players ? [...players].sort((left, right) => sortBy === 'position'
    ? positionOrder(left.position) - positionOrder(right.position) || comparePlayerNames(left, right)
    : comparePlayerNames(left, right)) : null, [players, sortBy])
  return <div><div className="breadcrumb"><Link to="/admin/teams">← Volver a Equipos</Link></div><div className="admin-header"><h1>Plantel {team ? `— ${team.name}` : ''}</h1><div className="roster-header-actions"><label className="roster-sort">Ordenar por:<select value={sortBy} onChange={event => setSortBy(event.target.value as RosterSort)}><option value="alphabetical">Alfabético</option><option value="position">Posición</option></select></label><Link className="btn btn-primary" to={`/admin/teams/${teamId}/players/new`}>+ Nuevo Jugador</Link></div></div>
    {error && <StatusMessage kind="error" message={error} />}{message && <StatusMessage kind="success" message={message} />}{!players && !error && <StatusMessage kind="loading" message="Cargando plantel..." />}
    {sortedPlayers && <div className="table-wrap"><table className="admin-table"><thead><tr><th>Foto</th><th>Nombre</th><th>Número</th><th>Posición</th><th>Estado</th><th>Acciones</th></tr></thead><tbody>{sortedPlayers.map(p => <tr key={p.id}><td><PlayerAvatar player={p} /></td><td><PlayerName player={p} /></td><td>{p.shirtNumber ?? '—'}</td><td>{p.position ?? '—'}</td><td>{p.active ? 'Activo' : 'Inactivo'}</td><td><div className="match-row-actions"><Link className="btn btn-secondary" to={`/admin/team-players/${p.id}/edit`}>Editar</Link><button className="btn btn-danger" onClick={() => setDeleteTarget(p)}>Eliminar</button></div></td></tr>)}</tbody></table></div>}
    <ConfirmModal open={Boolean(deleteTarget)} title="Eliminar jugador" message="¿Eliminar este jugador del plantel?" confirmLabel="Eliminar" onConfirm={removePlayer} onCancel={() => setDeleteTarget(null)} />
  </div>
}
