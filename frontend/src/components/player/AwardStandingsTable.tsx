import type { AwardStanding } from '../../api/types'

interface Props {
  standings: AwardStanding[]
  currentUserId?: number
}

export default function AwardStandingsTable({ standings, currentUserId }: Props) {
  return (
    <table className="pp-award-standings">
      <thead><tr><th>Posición</th><th>Jugador</th><th>Puntos</th><th>Exactos</th><th>Correctos</th><th>Error</th><th>Jugador Preferido</th></tr></thead>
      <tbody>{standings.map(entry => {
        const isMe = entry.userId === currentUserId
        return (
          <tr key={entry.userId} className={isMe ? 'pp-ranking__me' : ''}>
            <td>{entry.tieBreakPending
              ? <span className="pp-award-standings__pending-position">{entry.positionFrom}°–{entry.positionTo}°</span>
              : <span className="pp-ranking__pos">{entry.position}°</span>}</td>
            <td>
              {entry.firstName} {entry.lastName}{isMe && <span className="pp-ranking__me-badge">VOS</span>}
              {entry.tieBreakPending && <span className="pp-award-standings__pending">Desempate pendiente<small>Próximo criterio: Desafío de desempate</small></span>}
            </td>
            <td className="pp-ranking__points">{entry.points}</td>
            <td>{entry.exactCount}</td>
            <td>{entry.correctCount}</td>
            <td>{entry.accumulatedScoreError}</td>
            <td>{entry.preferredPlayerPoints} pts</td>
          </tr>
        )
      })}</tbody>
    </table>
  )
}
