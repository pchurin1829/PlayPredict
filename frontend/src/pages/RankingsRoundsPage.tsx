import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Edition, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

export default function RankingsRoundsPage() {
  const { editionId } = useParams()
  const navigate = useNavigate()

  const [edition, setEdition] = useState<Edition | null>(null)
  const [rounds, setRounds] = useState<Round[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    Promise.all([
      api.get<Edition>(`/editions/${editionId}`),
      api.get<Round[]>(`/editions/${editionId}/rounds`),
    ])
      .then(([ed, rs]) => {
        if (cancelled) return
        setEdition(ed)
        setRounds(rs)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las fechas.')
      })

    return () => {
      cancelled = true
    }
  }, [editionId])

  return (
    <div>
      <Link to={edition ? `/rankings/competitions/${edition.competitionId}/editions` : '/rankings'} className="pp-back">
        ← {edition ? 'Ediciones' : 'Ranking'}
      </Link>
      <div className="pp-header">
        <div>
          <h1>Ranking por Fecha</h1>
          {edition && <p className="pp-header__subtitle">{edition.name}</p>}
        </div>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!rounds && !error && <StatusMessage kind="loading" message="Cargando fechas..." />}

      {rounds && rounds.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">📅</span>
          <p className="pp-empty__text">Esta edición todavía no tiene fechas.</p>
        </div>
      )}

      {rounds && rounds.length > 0 && (
        <div className="pp-grid">
          {rounds.map((r) => (
            <div
              key={r.id}
              className="pp-comp-card"
              style={{ cursor: 'pointer' }}
              onClick={() => navigate(`/rankings/rounds/${r.id}`)}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => { if (e.key === 'Enter') navigate(`/rankings/rounds/${r.id}`) }}
            >
              <h3 className="pp-comp-card__name">📅 {r.name}</h3>
              <span className="pp-comp-card__action" style={{ marginTop: 'auto' }}>
                Ver Ranking
              </span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
