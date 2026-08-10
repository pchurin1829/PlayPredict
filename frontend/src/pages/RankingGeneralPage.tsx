import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Edition, RankingEntry } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { useAuth } from '../auth/AuthContext'
import './PlayerPages.css'

export default function RankingGeneralPage() {
  const { editionId } = useParams()

  const [edition, setEdition] = useState<Edition | null>(null)
  const [ranking, setRanking] = useState<RankingEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { user } = useAuth()

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
      <Link to={edition ? `/rankings/competitions/${edition.competitionId}/editions` : '/rankings'} className="pp-back">
        ← {edition ? 'Ediciones' : 'Ranking'}
      </Link>

      <div className="pp-header">
        <div>
          <h1>Ranking General</h1>
          {edition && <p className="pp-header__subtitle">{edition.name}</p>}
        </div>
        {edition && (
          <Link to={`/rankings/editions/${edition.id}/rounds`} className="pp-btn pp-btn--secondary">
            Ranking por Fecha
          </Link>
        )}
      </div>

      {!ranking && <StatusMessage kind="loading" message="Cargando ranking..." />}

      {ranking && ranking.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">📊</span>
          <p className="pp-empty__text">Todavía no hay pronósticos evaluados en esta edición.</p>
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
