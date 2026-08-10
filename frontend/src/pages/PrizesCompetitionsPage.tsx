import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition, Edition, Prize } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

interface CompetitionWithEdition {
  competition: Competition
  activeEdition: Edition | null
  prizes: Prize[]
}

export default function PrizesCompetitionsPage() {
  const [items, setItems] = useState<CompetitionWithEdition[] | null>(null)
  const [selectedCompId, setSelectedCompId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    api
      .get<Competition[]>('/competitions')
      .then(async (competitions) => {
        const active = competitions.filter((c) => c.isActive)
        const enriched: CompetitionWithEdition[] = []

        for (const competition of active) {
          const editions = await api.get<Edition[]>(`/competitions/${competition.id}/editions`)
          const activeEd = editions.find((e) => e.status === 'Active') ?? null

          let prizes: Prize[] = []
          if (activeEd) {
            prizes = await api.get<Prize[]>(`/prizes/editions/${activeEd.id}`).catch(() => [])
          }

          enriched.push({ competition, activeEdition: activeEd, prizes })
        }

        if (cancelled) return
        setItems(enriched)

        const first = enriched.find((e) => e.activeEdition)
        if (first) {
          setSelectedCompId(first.competition.id)
        }
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las competencias.')
      })

    return () => {
      cancelled = true
    }
  }, [])

  if (error) {
    return <StatusMessage kind="error" message={error} />
  }

  if (!items) {
    return <StatusMessage kind="loading" message="Cargando premios..." />
  }

  const selectedItem = items.find((i) => i.competition.id === selectedCompId)
  const publishedPrizes = selectedItem?.prizes.filter((p) => p.status === 'Published') ?? []

  return (
    <div>
      <div className="pp-header">
        <h1>Premios</h1>
      </div>

      {items.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">🎁</span>
          <p className="pp-empty__text">No hay competencias activas todavía.</p>
        </div>
      )}

      {items.length > 0 && (
        <div className="pp-edition-select">
          <span className="pp-edition-select__label">Competencia:</span>
          <select
            className="pp-edition-select__select"
            value={selectedCompId ?? ''}
            onChange={(e) => setSelectedCompId(Number(e.target.value))}
          >
            {items.map((item) => (
              <option key={item.competition.id} value={item.competition.id}>
                {item.competition.name}
                {item.activeEdition ? ` — ${item.activeEdition.name}` : ' (sin edición activa)'}
              </option>
            ))}
          </select>
          {selectedItem?.activeEdition && (
            <Link
              to={`/prizes/competitions/${selectedItem.competition.id}/editions`}
              className="pp-btn pp-btn--secondary"
              style={{ fontSize: '0.8rem' }}
            >
              Ver todas las ediciones
            </Link>
          )}
        </div>
      )}

      {selectedItem && !selectedItem.activeEdition && (
        <div className="pp-empty">
          <span className="pp-empty__icon">🎁</span>
          <p className="pp-empty__text">Esta competencia no tiene edición activa.</p>
        </div>
      )}

      {selectedItem?.activeEdition && publishedPrizes.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">🎁</span>
          <p className="pp-empty__text">
            Todavía no hay premios publicados para {selectedItem.activeEdition.name}.
          </p>
        </div>
      )}

      {publishedPrizes.length > 0 && (
        <div className="pp-grid">
          {publishedPrizes.map((p) => (
            <div key={p.id} className="pp-prize-card">
              <div className="pp-prize-card__header">
                <h3 className="pp-prize-card__name">🎁 {p.name}</h3>
                <span className={`pp-prize-card__badge ${p.status === 'Published' ? 'pp-prize-card__badge--published' : 'pp-prize-card__badge--other'}`}>
                  {p.statusLabel}
                </span>
              </div>

              {p.description && <p className="pp-prize-card__description">{p.description}</p>}

              <div className="pp-prize-card__details">
                {p.referenceValue && (
                  <>
                    <dt className="pp-prize-card__dt">Valor</dt>
                    <dd className="pp-prize-card__dd">{p.referenceValue}</dd>
                  </>
                )}
                <dt className="pp-prize-card__dt">Tipo</dt>
                <dd className="pp-prize-card__dd">{p.prizeTypeLabel}</dd>
                {p.sponsorName && (
                  <>
                    <dt className="pp-prize-card__dt">Sponsor</dt>
                    <dd className="pp-prize-card__dd">{p.sponsorName}</dd>
                  </>
                )}
                <dt className="pp-prize-card__dt">Para</dt>
                <dd className="pp-prize-card__dd">{p.forLabel}</dd>
              </div>

              <div className="pp-prize-card__winner">
                {p.hasProvisionalWinner ? (
                  <>
                    <span className="pp-prize-card__winner-label">Líder actual: </span>
                    <span className="pp-prize-card__winner-name">
                      {p.currentWinners.map((w) => `${w.firstName} ${w.lastName}`).join(', ')}
                    </span>
                  </>
                ) : (
                  <span className="pp-prize-card__no-winner">Todavía no hay líder provisional</span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
