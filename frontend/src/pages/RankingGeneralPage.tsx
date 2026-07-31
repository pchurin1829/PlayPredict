import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Edition, RankingEntry } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function RankingGeneralPage() {
  const { editionId } = useParams()

  const [edition, setEdition] = useState<Edition | null>(null)
  const [ranking, setRanking] = useState<RankingEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    Promise.all([
      api.get<Edition>(`/editions/${editionId}`),
      api.get<RankingEntry[]>(`/rankings/editions/${editionId}`),
    ])
      .then(([ed, r]) => {
        if (cancelled) return
        setEdition(ed)
        setRanking(r)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudo cargar el ranking.')
      })

    return () => {
      cancelled = true
    }
  }, [editionId])

  return (
    <div>
      <div className="breadcrumb">
        {edition && (
          <Link to={`/rankings/competitions/${edition.competitionId}/editions`}>← Ediciones</Link>
        )}
      </div>
      <div className="admin-header">
        <h1>Ranking General {edition ? `— ${edition.name}` : ''}</h1>
        {edition && (
          <Link to={`/rankings/editions/${edition.id}/rounds`} className="btn btn-secondary">
            Ranking por Fecha
          </Link>
        )}
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!ranking && !error && <StatusMessage kind="loading" message="Cargando ranking..." />}

      {ranking && ranking.length === 0 && (
        <div className="empty-state">Todavía no hay pronósticos evaluados en esta Edición.</div>
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
