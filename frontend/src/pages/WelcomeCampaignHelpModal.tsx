import { useEffect } from 'react'
import './WelcomeCampaignHelpModal.css'

interface Props {
  open: boolean
  onClose: () => void
}

export default function WelcomeCampaignHelpModal({ open, onClose }: Props) {
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
    <div className="wc-help__overlay" onClick={onClose}>
      <section className="wc-help" role="dialog" aria-modal="true" aria-labelledby="wc-help-title" onClick={(e) => e.stopPropagation()}>
        <div className="wc-help__header">
          <h2 id="wc-help-title">Cómo preparar las imágenes de la campaña</h2>
          <button type="button" onClick={onClose} aria-label="Cerrar ayuda">×</button>
        </div>

        <p>
          La Campaña de Bienvenida se muestra a pantalla completa apenas el jugador inicia sesión, antes de llegar a PlayPredict.
          No es obligatorio usar una proporción exacta: PlayPredict acepta otras proporciones y advierte cuando puede no quedar prolijo,
          sin bloquear la imagen.
        </p>

        <h3>Modos de ajuste</h3>
        <div className="wc-help__modes">
          <div className="wc-help__mode">
            <strong>Mostrar imagen completa (Contain)</strong>
            <p>La imagen se muestra completa y mantiene sus proporciones.</p>
            <p className="wc-help__pro">Ventaja: no se pierde ninguna parte de la imagen.</p>
            <p className="wc-help__con">Consideración: si la proporción difiere de la del panel pueden aparecer márgenes.</p>
          </div>
          <div className="wc-help__mode">
            <strong>Cubrir todo el panel (Cover)</strong>
            <p>La imagen se amplía manteniendo sus proporciones hasta cubrir toda la pantalla.</p>
            <p className="wc-help__pro">Ventaja: no quedan espacios vacíos.</p>
            <p className="wc-help__con">Consideración: algunas partes pueden quedar recortadas.</p>
          </div>
        </div>

        <h3>Duración</h3>
        <p>
          Cada imagen tiene su propia duración, expresada en segundos, entre 1 y 10 (con decimales, por ejemplo 1,5). La secuencia
          completa avanza automáticamente y continúa sola hacia PlayPredict al terminar.
        </p>

        <aside>
          Para publicidades diseñadas específicamente para esta campaña, usar imágenes cercanas a 4:3 y mantener textos, precios y logos
          alejados de los bordes tolera mejor pequeños recortes en distintas pantallas.
        </aside>

        <div className="wc-help__actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>Cerrar</button>
        </div>
      </section>
    </div>
  )
}
