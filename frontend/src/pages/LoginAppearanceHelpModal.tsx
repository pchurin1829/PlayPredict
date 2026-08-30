import { useEffect } from 'react'
import './LoginAppearanceHelpModal.css'

interface Props {
  open: boolean
  onClose: () => void
}

export default function LoginAppearanceHelpModal({ open, onClose }: Props) {
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
    <div className="login-help__overlay" onClick={onClose}>
      <section className="login-help" role="dialog" aria-modal="true" aria-labelledby="login-help-title" onClick={(e) => e.stopPropagation()}>
        <div className="login-help__header">
          <h2 id="login-help-title">Cómo preparar las imágenes del login</h2>
          <button type="button" onClick={onClose} aria-label="Cerrar ayuda">×</button>
        </div>

        <p>
          PlayPredict puede utilizar imágenes de distintas dimensiones y proporciones. No es obligatorio preparar una imagen con una medida exacta,
          pero utilizar las proporciones recomendadas permite obtener un mejor resultado visual.
        </p>

        <h3>Modos de ajuste</h3>
        <div className="login-help__modes">
          <div className="login-help__mode">
            <strong>Mostrar completa (Contain)</strong>
            <p>La imagen se muestra completa y mantiene sus proporciones.</p>
            <p className="login-help__pro">Ventaja: no se pierde ninguna parte de la imagen.</p>
            <p className="login-help__con">Consideración: si la proporción de la imagen es diferente a la del panel pueden aparecer márgenes o franjas alrededor.</p>
          </div>
          <div className="login-help__mode">
            <strong>Cubrir panel (Cover)</strong>
            <p>La imagen se amplía manteniendo sus proporciones hasta cubrir completamente el panel.</p>
            <p className="login-help__pro">Ventaja: no quedan espacios ni franjas vacías.</p>
            <p className="login-help__con">Consideración: algunas partes de la imagen pueden quedar recortadas.</p>
          </div>
        </div>

        <h3>Tamaños recomendados</h3>
        <div className="login-help__sizes">
          <div>
            <strong>Panel principal</strong>
            <span>Proporción recomendada: 4:3</span>
            <span>Resolución recomendada: 1440 × 1080 px</span>
          </div>
          <div>
            <strong>Paneles publicitarios</strong>
            <span>Proporción recomendada: 4:3</span>
            <span>Resolución recomendada: 960 × 720 px</span>
            <span>Para mayor calidad también puede utilizarse 1200 × 900 px</span>
          </div>
        </div>
        <p className="login-help__note">
          No es obligatorio utilizar exactamente estas resoluciones. Lo más importante para evitar márgenes o recortes importantes es utilizar una
          imagen con una proporción cercana a 4:3.
        </p>

        <h3>Si la imagen tiene otra proporción</h3>
        <p>
          PlayPredict analiza automáticamente las dimensiones y la proporción de la imagen seleccionada. Si la imagen no es adecuada para el panel,
          el sistema muestra una advertencia.
        </p>
        <p>
          El administrador puede igualmente utilizarla y elegir:
        </p>
        <ul>
          <li>Mostrar completa → puede dejar márgenes.</li>
          <li>Cubrir panel → puede producir recortes.</li>
        </ul>

        <aside>
          Para publicidades diseñadas específicamente para PlayPredict, utilizar imágenes 4:3 y mantener textos, precios, logos y productos
          importantes alejados de los bordes para tolerar pequeños recortes en diferentes resoluciones de pantalla.
        </aside>

        <div className="login-help__actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>Cerrar</button>
        </div>
      </section>
    </div>
  )
}
