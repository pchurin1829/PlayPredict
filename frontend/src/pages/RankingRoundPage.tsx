import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { RankingEntry, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { useAuth } from '../auth/AuthContext'
import './PlayerPages.css'

export default function RankingRoundPage() {
  const { roundId } = useParams()

  const [round, setRound] = useState<Round | null>(null)
  const [ranking, setRanking] = useState<RankingEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { user } = useAuth()

  useEffect(() => {
    let cancelled = false

    Promise.all([
      api.get<Round>(`/rounds/${roundId}`),
      api.get<RankingEntry[]>(`/rankings/rounds/${roundId}`),
    ])
      .then(([r, ranked]) => {
        if (cancelled) return
        setRound(r)
        setRanking(ranked)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudo cargar el ranking.')
      })

    return () => {
      cancelled = true
    }
  }, [roundId])

  if (error) {
    return (
      <div>
        <Link to="/rankings" className="pp-back">← Ranking</Link>
        <StatusMessage kind="error" message={error} />
      </div>
    )
  }

  return (
    <div>
      <Link to={round ? `/rankings/editions/${round.editionId}/rounds` : '/rankings'} className="pp-back">
        ← {round ? 'Fechas' : 'Ranking'}
      </Link>

      <div className="pp-header">
        <div>
          <h1>Ranking por Fecha</h1>
          {round && <p className="pp-header__subtitle">{round.name}</p>}
        </div>
      </div>

      {!ranking && <StatusMessage kind="loading" message="Cargando ranking..." />}

      {ranking && ranking.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">📊</span>
          <p className="pp-empty__text">Todavía no hay pronósticos evaluados en esta fecha.</p>
        </div>
      )}

      {ranking && ranking.length > 0 && (
        <div className="pp-ranking">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Jugador</th>
                <th>Puntos</th>
                <th>Exactos</th>
                <th>Correctos</th>
                <th>Evaluados</th>
              </tr>
            </thead>
            <tbody>
              {ranking.map((r) => {
                const isMe = user && r.userId === user.id
                return (
                  <tr key={r.userId} className={isMe ? 'pp-ranking__me' : ''}>
                    <td>
                      <span className={`pp-ranking__pos ${r.position <= 3 ? `pp-ranking__pos--${r.position}` : ''}`}>
                        {r.position}°
                      </span>
                    </td>
                    <td>
                      {r.firstName} {r.lastName}
                      {isMe && <span className="pp-ranking__me-badge">(Vos)</span>}
                    </td>
                    <td className="pp-ranking__points">{r.points}</td>
                    <td>{r.exactCount}</td>
                    <td>{r.correctCount}</td>
                    <td>{r.evaluatedCount}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
