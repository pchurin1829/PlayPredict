import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Edition, Prize } from '../api/types'
import StatusMessage from '../components/StatusMessage'

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

  return (
    <div>
      <div className="breadcrumb">
        {edition && (
          <Link to={`/prizes/competitions/${edition.competitionId}/editions`}>← Ediciones</Link>
        )}
      </div>
      <div className="admin-header">
        <h1>Premios {edition ? `— ${edition.name}` : ''}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!prizes && !error && <StatusMessage kind="loading" message="Cargando premios..." />}

      {prizes && prizes.length === 0 && (
        <div className="empty-state">Todavía no hay premios publicados para esta Edición.</div>
      )}

      {prizes && prizes.length > 0 && (
        <div className="prize-cards">
          {prizes.map((p) => (
            <div key={p.id} className="prize-card">
              <div className="prize-card__header">
                <h2>{p.name}</h2>
                <span className={`badge badge--${p.status}`}>{p.statusLabel}</span>
              </div>

              {p.description && <p className="prize-card__description">{p.description}</p>}

              <dl className="prize-card__details">
                <div>
                  <dt>Tipo</dt>
                  <dd>{p.prizeTypeLabel}</dd>
                </div>
                {p.referenceValue && (
                  <div>
                    <dt>Valor de referencia</dt>
                    <dd>{p.referenceValue}</dd>
                  </div>
                )}
                {p.sponsorName && (
                  <div>
                    <dt>Sponsor</dt>
                    <dd>{p.sponsorName}</dd>
                  </div>
                )}
                <div>
                  <dt>Para quién es</dt>
                  <dd>{p.forLabel}</dd>
                </div>
              </dl>

              <div className="prize-card__winner">
                {p.hasProvisionalWinner ? (
                  <>
                    <strong>Ganador actual (provisional):</strong>{' '}
                    {p.currentWinners.map((w) => `${w.firstName} ${w.lastName}`).join(', ')}
                  </>
                ) : (
                  <span className="prize-card__no-winner">Todavía no hay ganador provisional.</span>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
