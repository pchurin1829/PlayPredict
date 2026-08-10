import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition, Edition, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

interface ExploreItem {
  competition: Competition
  activeEdition: Edition | null
  roundsCount: number
}

export default function ExploreCompetitionsPage() {
  const [items, setItems] = useState<ExploreItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    api
      .get<Competition[]>('/competitions')
      .then(async (competitions) => {
        const active = competitions.filter((c) => c.isActive)

        const enriched = await Promise.all(
          active.map(async (competition) => {
            const editions = await api.get<Edition[]>(`/competitions/${competition.id}/editions`)
            const activeEdition = editions.find((e) => e.status === 'Active') ?? null

            let roundsCount = 0
            if (activeEdition) {
              const rounds = await api.get<Round[]>(`/editions/${activeEdition.id}/rounds`)
              roundsCount = rounds.length
            }

            return { competition, activeEdition, roundsCount }
          }),
        )

        if (!cancelled) setItems(enriched)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las Competencias.')
      })

    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div>
      <div className="pp-header">
        <h1>Explorar Competencias</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!items && !error && <StatusMessage kind="loading" message="Cargando Competencias..." />}

      {items && items.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">⚽</span>
          <p className="pp-empty__text">No hay competencias activas todavía.</p>
        </div>
      )}

      {items && items.length > 0 && (
        <div className="pp-grid">
          {items.map(({ competition, activeEdition, roundsCount }) => (
            <div key={competition.id} className="pp-comp-card">
              <h3 className="pp-comp-card__name">🏆 {competition.name}</h3>
              {activeEdition ? (
                <span className="pp-comp-card__edition">📍 {activeEdition.name}</span>
              ) : (
                <span style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)' }}>
                  Sin edición activa
                </span>
              )}
              <div className="pp-comp-card__details">
                <span>🏅 {competition.sport}</span>
                {roundsCount > 0 && <span>📅 {roundsCount} fecha{roundsCount !== 1 ? 's' : ''}</span>}
              </div>
              <div className="pp-comp-card__actions">
                <Link to={`/competitions/${competition.id}`} className="pp-comp-card__action">
                  Ver competencia
                </Link>
                <Link to={`/leagues/new?competitionId=${competition.id}`} className="pp-comp-card__action pp-comp-card__action--secondary">
                  + Crear Liga
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
