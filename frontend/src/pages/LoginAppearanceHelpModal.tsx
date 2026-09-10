import { useEffect } from 'react'
import { LOGIN_IMAGE_CONTRACTS } from '../login/imageContracts'
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
          El estadio es un fondo fijo de PlayPredict. La campaña y las publicidades se reemplazan
          independientemente; el formulario de login siempre es HTML.
        </p>

        <h3>Contrato de imágenes</h3>
        <div className="login-help__sizes">
          {(['Main', 'AdTop'] as const).map((slot) => {
            const contract = LOGIN_IMAGE_CONTRACTS[slot]
            return (
              <div key={slot}>
                <strong>{slot === 'Main' ? 'Campaña principal' : 'Las tres publicidades'}</strong>
                <span>{contract.width} × {contract.height} px · {contract.ratio}</span>
                <span>{contract.formats}</span>
                <span>Safe area: {contract.safeArea} px desde cada borde</span>
                <span>Peso recomendado: ≤{contract.recommendedKB} KB</span>
              </div>
            )
          })}
        </div>
        <p>
          Campaña: transparencia recomendada. No incluir estadio ni césped: PlayPredict aporta el
          fondo. Una imagen opaca conservará su rectángulo; el sistema no elimina su fondo.
        </p>

        <h3>Ajustes fijos del login</h3>
        <ul>
          <li>Campaña: contain, se muestra completa sin deformación. Otras proporciones pueden dejar espacio alrededor.</li>
          <li>Publicidades: cover, cubren slots 3:2. Otras proporciones pueden recortar contenido.</li>
        </ul>
        <p>No es necesario configurar estos ajustes. La vista previa de cada slot utiliza el mismo modo que el login.</p>

        <h3>Preparación y exportación</h3>
        <ul>
          <li>Mantener textos, precios, logos y productos importantes dentro de la safe area.</li>
          <li>Evitar texto pequeño y asegurar contraste, especialmente sobre transparencia.</li>
          <li>Exportar en sRGB y optimizar el peso manteniendo la legibilidad.</li>
        </ul>
        <aside>
          Los tamaños y pesos indicados son recomendaciones de Diseño, no nuevos bloqueos de subida.
          Se mantienen los límites técnicos existentes. Las advertencias no sustituyen la revisión visual.
          Guardar imagen aplica el cambio directamente; la publicación programada no está implementada.
        </aside>

        <div className="login-help__actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>Cerrar</button>
        </div>
      </section>
    </div>
  )
}
