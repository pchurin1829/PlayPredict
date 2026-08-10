import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { EDITION_STATUS_LABELS, type Competition, type Edition } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

export default function PrizesEditionsPage() {
  const { competitionId } = useParams()

  const [competition, setCompetition] = useState<Competition | null>(null)
  const [editions, setEditions] = useState<Edition[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    Promise.all([
      api.get<Competition>(`/competitions/${competitionId}`),
      api.get<Edition[]>(`/competitions/${competitionId}/editions`),
    ])
      .then(([c, eds]) => {
        if (cancelled) return
        setCompetition(c)
        setEditions(eds)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las ediciones.')
      })

    return () => {
      cancelled = true
    }
  }, [competitionId])

  return (
    <div>
      <Link to="/prizes" className="pp-back">← Premios</Link>
      <div className="pp-header">
        <h1>Premios — {competition?.name ?? 'Ediciones'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!editions && !error && <StatusMessage kind="loading" message="Cargando ediciones..." />}

      {editions && editions.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">🎁</span>
          <p className="pp-empty__text">Esta competencia todavía no tiene ediciones.</p>
        </div>
      )}

      {editions && editions.length > 0 && (
        <div className="pp-grid">
          {editions.map((ed) => (
            <div key={ed.id} className="pp-comp-card">
              <h3 className="pp-comp-card__name">{ed.name}</h3>
              <div className="pp-comp-card__details">
                <span>📅 {new Date(ed.startDateUtc).toLocaleDateString()}</span>
                <span className={`badge badge--${ed.status}`}>{EDITION_STATUS_LABELS[ed.status]}</span>
              </div>
              <Link to={`/prizes/editions/${ed.id}`} className="pp-comp-card__action">
                Ver Premios
              </Link>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
