import type { Prize } from '../../api/types'
import './PrizeHighlight.css'

interface PrizeHighlightProps {
  prizes: Prize[]
}

export default function PrizeHighlight({ prizes }: PrizeHighlightProps) {
  const mainPrize = prizes.find((p) => p.status === 'Published') ?? prizes[0]

  return (
    <div className="phl">
      <h3 className="phl__title">Premios destacados</h3>
      {mainPrize ? (
        <div className="phl__card">
          <div className="phl__card-header">
            <span className="phl__icon">🎁</span>
            <span className="phl__name">{mainPrize.name}</span>
          </div>
          {mainPrize.referenceValue && (
            <p className="phl__value">{mainPrize.referenceValue}</p>
          )}
          {mainPrize.sponsorName && (
            <p className="phl__sponsor">Sponsor: {mainPrize.sponsorName}</p>
          )}
          {mainPrize.hasProvisionalWinner && (
            <p className="phl__winner">
              Líder actual:{' '}
              {mainPrize.currentWinners.map((w) => `${w.firstName} ${w.lastName}`).join(', ')}
            </p>
          )}
        </div>
      ) : (
        <p className="phl__empty">No hay premios publicados todavía</p>
      )}
    </div>
  )
}
