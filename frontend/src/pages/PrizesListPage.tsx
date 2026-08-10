import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Edition, Prize } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

export default function PrizesListPage() {
  const { editionId } = useParams()

  const [edition, setEdition] = useState<Edition | null>(null)
  const [prizes, setPrizes] = useState<Prize[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    Promise.all([
      api.get<Edition>(`/editions/${editionId}`),
      api.get<Prize[]>(`/prizes/editions/${editionId}`),
    ])
      .then(([ed, pr]) => {
        if (cancelled) return
        setEdition(ed)
        setPrizes(pr)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar los premios.')
      })

    return () => {
      cancelled = true
    }
  }, [editionId])

  if (error) {
    return (
      <div>
        <Link to="/prizes" className="pp-back">← Premios</Link>
        <StatusMessage kind="error" message={error} />
      </div>
    )
  }

  return (
    <div>
      <Link to={edition ? `/prizes/competitions/${edition.competitionId}/editions` : '/prizes'} className="pp-back">
        ← {edition ? 'Ediciones' : 'Premios'}
      </Link>

      <div className="pp-header">
        <div>
          <h1>Premios</h1>
          {edition && <p className="pp-header__subtitle">{edition.name}</p>}
        </div>
      </div>

      {!prizes && <StatusMessage kind="loading" message="Cargando premios..." />}

      {prizes && prizes.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">🎁</span>
          <p className="pp-empty__text">Todavía no hay premios publicados para esta edición.</p>
        </div>
      )}

      {prizes && prizes.length > 0 && (
        <div className="pp-grid">
          {prizes.map((p) => (
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
