import { useEffect, useRef, useState } from 'react'
import type { ActiveWelcomeCampaignSlide } from '../../api/types'
import './WelcomeCampaignPlayer.css'

const PRELOAD_TIMEOUT_MS = 8000
const MIN_DURATION_MS = 1000
const MAX_DURATION_MS = 10000

interface Props {
  slides: ActiveWelcomeCampaignSlide[]
  onFinished: () => void
  /** Solo para preview ADMIN: agrega una forma de cerrar antes de tiempo. El PLAYER nunca recibe esta prop. */
  closable?: boolean
}

function preloadImage(url: string): Promise<boolean> {
  return new Promise((resolve) => {
    let done = false
    const finish = (ok: boolean) => {
      if (done) return
      done = true
      resolve(ok)
    }
    const img = new Image()
    img.onload = () => finish(true)
    img.onerror = () => finish(false)
    img.src = url
    window.setTimeout(() => finish(false), PRELOAD_TIMEOUT_MS)
  })
}

export default function WelcomeCampaignPlayer({ slides, onFinished, closable }: Props) {
  const [ready, setReady] = useState<ActiveWelcomeCampaignSlide[] | null>(null)
  const [index, setIndex] = useState(0)
  const finishedRef = useRef(false)

  function finish() {
    if (finishedRef.current) return
    finishedRef.current = true
    onFinished()
  }

  useEffect(() => {
    let active = true
    const ordered = [...slides].sort((a, b) => a.sortOrder - b.sortOrder)
    Promise.all(ordered.map((slide) => preloadImage(slide.imageUrl).then((ok) => (ok ? slide : null))))
      .then((results) => {
        if (!active) return
        const usable = results.filter((s): s is ActiveWelcomeCampaignSlide => s !== null)
        if (usable.length === 0) {
          finish()
          return
        }
        setReady(usable)
      })
      .catch(() => {
        if (active) finish()
      })
    return () => { active = false }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (!ready) return
    if (index >= ready.length) {
      finish()
      return
    }
    const durationMs = Math.max(MIN_DURATION_MS, Math.min(MAX_DURATION_MS, Number(ready[index].durationSeconds) * 1000))
    const timer = window.setTimeout(() => setIndex((i) => i + 1), durationMs)
    return () => window.clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [ready, index])

  useEffect(() => {
    if (!closable) return
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') finish()
    }
    document.addEventListener('keydown', closeOnEscape)
    return () => document.removeEventListener('keydown', closeOnEscape)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [closable])

  if (!ready || index >= ready.length) {
    return <div className="wc-player wc-player--loading" role="dialog" aria-modal="true" aria-label="Publicidad" />
  }

  const slide = ready[index]

  return (
    <div className="wc-player" role="dialog" aria-modal="true" aria-label="Publicidad">
      <img
        key={slide.id}
        src={slide.imageUrl}
        alt=""
        className="wc-player__image"
        style={{ objectFit: slide.fitMode === 'Contain' ? 'contain' : 'cover' }}
      />
      {closable && (
        <button type="button" className="wc-player__close" onClick={finish} aria-label="Cerrar previsualización">×</button>
      )}
    </div>
  )
}
