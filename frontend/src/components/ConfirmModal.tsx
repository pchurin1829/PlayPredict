import './ConfirmModal.css'

interface ConfirmModalProps {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  cancelLabel?: string
  onConfirm: () => void
  onCancel: () => void
}

export default function ConfirmModal({
  open,
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  onConfirm,
  onCancel,
}: ConfirmModalProps) {
  if (!open) return null

  return (
    <div className="cmodal-overlay" onClick={onCancel}>
      <div className="cmodal" onClick={(e) => e.stopPropagation()}>
        <h3 className="cmodal__title">{title}</h3>
        <p className="cmodal__message">{message}</p>
        <div className="cmodal__actions">
          <button type="button" className="cmodal__cancel" onClick={onCancel}>
            {cancelLabel}
          </button>
          <button type="button" className="cmodal__confirm" onClick={onConfirm}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
