import type { RankingEntry } from '../../api/types'
import { useAuth } from '../../auth/AuthContext'
import './RankingPreview.css'

interface RankingPreviewProps {
  ranking: RankingEntry[]
  title?: string
}

export default function RankingPreview({
  ranking,
  title = 'Posiciones — Mis Ligas',
}: RankingPreviewProps) {
  const { user } = useAuth()
  const top5 = ranking.slice(0, 5)

  function positionClass(position: number): string {
    if (position === 1) return 'rpreview__pos--gold'
    if (position === 2) return 'rpreview__pos--silver'
    if (position === 3) return 'rpreview__pos--bronze'
    return ''
  }

  return (
    <div className="rpreview">
      <h3 className="rpreview__title">{title}</h3>
      {top5.length === 0 ? (
        <p className="rpreview__empty">Sin datos todavía</p>
      ) : (
        <div className="rpreview__list">
          {top5.map((r) => {
            const isMe = user && r.userId === user.id
            return (
              <div
                key={r.userId}
                className={`rpreview__row ${isMe ? 'rpreview__row--me' : ''}`}
              >
                <span className={`rpreview__pos ${positionClass(r.position)}`}>
                  {r.position}°
                </span>
                <span className="rpreview__name">
                  {r.firstName} {r.lastName}{!r.isActiveParticipant && <span className="rpreview__inactive">Retirado</span>}
                  {isMe && <span className="rpreview__me-badge">(Vos)</span>}
                </span>
                <span className="rpreview__points">{r.points} pts</span>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
