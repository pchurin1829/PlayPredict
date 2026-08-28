import { useEffect } from 'react'
import { TIE_BREAK_POLICY } from '../../ranking/tieBreakPolicy'
import './TieBreakPolicyModal.css'

interface Props {
  open: boolean
  onClose: () => void
}

export default function TieBreakPolicyModal({ open, onClose }: Props) {
  useEffect(() => {
    if (!open) return
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', closeOnEscape)
    return () => document.removeEventListener('keydown', closeOnEscape)
  }, [onClose, open])

  if (!open) return null

  return (
    <div className="tiebreak-policy__overlay" onClick={onClose}>
      <section className="tiebreak-policy" role="dialog" aria-modal="true" aria-labelledby="tiebreak-policy-title" onClick={event => event.stopPropagation()}>
        <div className="tiebreak-policy__header">
          <h2 id="tiebreak-policy-title">{TIE_BREAK_POLICY.title}</h2>
          <button type="button" onClick={onClose} aria-label="Cerrar política de desempate">×</button>
        </div>
        <p>{TIE_BREAK_POLICY.rankingExplanation}</p>
        <div className="tiebreak-policy__views">{TIE_BREAK_POLICY.viewExplanation.map(line => <p key={line}>{line}</p>)}</div>
        <div className="tiebreak-policy__example">{TIE_BREAK_POLICY.example.map(line => <span key={line}>{line}</span>)}</div>
        <p>{TIE_BREAK_POLICY.prizeOrderExplanation}</p>
        <ol>{TIE_BREAK_POLICY.rules.map(rule => <li key={rule}>{rule}</li>)}</ol>
        <aside><strong>{TIE_BREAK_POLICY.clarification}</strong><span>{TIE_BREAK_POLICY.clarificationExample}</span></aside>
        <div className="tiebreak-policy__actions"><button type="button" className="pp-btn pp-btn--secondary" onClick={onClose}>Cerrar</button></div>
      </section>
    </div>
  )
}
