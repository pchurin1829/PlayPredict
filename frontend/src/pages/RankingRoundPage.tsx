import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { RankingEntry, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function RankingRoundPage() {
  const { roundId } = useParams()

  const [round, setRound] = useState<Round | null>(null)
  const [ranking, setRanking] = useState<RankingEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)

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

  return (
    <div>
      <div className="breadcrumb">
        {round && <Link to={`/rankings/editions/${round.editionId}/rounds`}>← Fechas</Link>}
      </div>
      <div className="admin-header">
        <h1>Ranking por Fecha {round ? `— ${round.name}` : ''}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!ranking && !error && <StatusMessage kind="loading" message="Cargando ranking..." />}

      {ranking && ranking.length === 0 && (
        <div className="empty-state">Todavía no hay pronósticos evaluados en esta Fecha.</div>
      )}

      {ranking && ranking.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Usuario</th>
                <th>Puntos</th>
                <th>Exactos</th>
                <th>Correctos</th>
                <th>Incorrectos</th>
                <th>Pronósticos</th>
              </tr>
            </thead>
            <tbody>
              {ranking.map((r) => (
                <tr key={r.userId}>
                  <td>{r.position}</td>
                  <td>
                    {r.firstName} {r.lastName}
                  </td>
                  <td>{r.points}</td>
                  <td>{r.exactCount}</td>
                  <td>{r.correctCount}</td>
                  <td>{r.incorrectCount}</td>
                  <td>{r.evaluatedCount}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
