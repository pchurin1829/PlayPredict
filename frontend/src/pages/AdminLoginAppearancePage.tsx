import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { AdminLoginAppearanceSlot, LoginImageFitMode, LoginImageSlot } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import LoginAppearanceHelpModal from './LoginAppearanceHelpModal'
import './AdminLoginAppearancePage.css'

const SLOT_META: Record<LoginImageSlot, { label: string; hint: string; minWidth: number; minHeight: number; recommendedWidth: number; recommendedHeight: number }> = {
  Main: { label: 'Panel principal', hint: 'Imagen central de la pantalla de login.', minWidth: 1024, minHeight: 768, recommendedWidth: 1440, recommendedHeight: 1080 },
  AdTop: { label: 'Publicidad superior', hint: 'Primer panel de la columna de publicidad.', minWidth: 480, minHeight: 360, recommendedWidth: 960, recommendedHeight: 720 },
  AdMiddle: { label: 'Publicidad media', hint: 'Segundo panel de la columna de publicidad.', minWidth: 480, minHeight: 360, recommendedWidth: 960, recommendedHeight: 720 },
  AdBottom: { label: 'Publicidad inferior', hint: 'Tercer panel de la columna de publicidad.', minWidth: 480, minHeight: 360, recommendedWidth: 960, recommendedHeight: 720 },
}

const SLOT_ORDER: LoginImageSlot[] = ['Main', 'AdTop', 'AdMiddle', 'AdBottom']
const RECOMMENDED_RATIO = 4 / 3
const RATIO_WARNING_THRESHOLD = 0.05

interface PendingSelection {
  file: File
  previewUrl: string
  width: number
  height: number
  warnings: { code: string; message: string }[]
}

function buildClientWarnings(slot: LoginImageSlot, width: number, height: number) {
  const meta = SLOT_META[slot]
  const warnings: { code: string; message: string }[] = []
  if (width < meta.minWidth || height < meta.minHeight) {
    warnings.push({ code: 'LOW_RESOLUTION', message: `Resolución inferior a la mínima recomendada de ${meta.minWidth}×${meta.minHeight}.` })
  }
  const ratio = width / height
  if (Math.abs(ratio / RECOMMENDED_RATIO - 1) > RATIO_WARNING_THRESHOLD) {
    warnings.push({ code: 'ASPECT_RATIO_MISMATCH', message: 'La proporción difiere más de 5% de la recomendada 4:3 y puede dejar márgenes o requerir recorte.' })
  }
  return warnings
}

function readImageDimensions(file: File): Promise<{ width: number; height: number }> {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file)
    const image = new Image()
    image.onload = () => { resolve({ width: image.naturalWidth, height: image.naturalHeight }); URL.revokeObjectURL(url) }
    image.onerror = () => { reject(new Error('No se pudo leer la imagen seleccionada.')); URL.revokeObjectURL(url) }
    image.src = url
  })
}

function SlotCard({ slot, data, onChanged }: { slot: LoginImageSlot; data: AdminLoginAppearanceSlot; onChanged: (updated: AdminLoginAppearanceSlot) => void }) {
  const meta = SLOT_META[slot]
  const input = useRef<HTMLInputElement>(null)
  const [pending, setPending] = useState<PendingSelection | null>(null)
  const [fitMode, setFitMode] = useState<LoginImageFitMode>(data.fitMode)
  const [uploading, setUploading] = useState(false)
  const [savingFitMode, setSavingFitMode] = useState(false)
  const [restoring, setRestoring] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => { setFitMode(data.fitMode) }, [data.fitMode])
  useEffect(() => () => { if (pending) URL.revokeObjectURL(pending.previewUrl) }, [pending])

  async function choose(file?: File) {
    if (!file) return
    setError(null)
    try {
      const { width, height } = await readImageDimensions(file)
      if (pending) URL.revokeObjectURL(pending.previewUrl)
      setPending({ file, previewUrl: URL.createObjectURL(file), width, height, warnings: buildClientWarnings(slot, width, height) })
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo leer la imagen seleccionada.')
    }
  }

  function cancelSelection() {
    if (pending) URL.revokeObjectURL(pending.previewUrl)
    setPending(null)
    if (input.current) input.current.value = ''
  }

  async function upload() {
    if (!pending) return
    setUploading(true)
    setError(null)
    try {
      const form = new FormData()
      form.append('file', pending.file)
      const updated = await api.upload<AdminLoginAppearanceSlot>(`/admin/login-appearance/${slot.toLowerCase()}/image`, form)
      onChanged(updated)
      cancelSelection()
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo guardar la imagen.')
    } finally {
      setUploading(false)
    }
  }

  async function saveFitMode() {
    setSavingFitMode(true)
    setError(null)
    try {
      const updated = await api.put<AdminLoginAppearanceSlot>(`/admin/login-appearance/${slot.toLowerCase()}/fit-mode`, { fitMode })
      onChanged(updated)
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo guardar el modo de ajuste.')
    } finally {
      setSavingFitMode(false)
    }
  }

  async function restoreDefault() {
    setRestoring(true)
    setError(null)
    try {
      const updated = await api.del<AdminLoginAppearanceSlot>(`/admin/login-appearance/${slot.toLowerCase()}`)
      onChanged(updated)
      cancelSelection()
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo restaurar la imagen por defecto.')
    } finally {
      setRestoring(false)
    }
  }

  const previewFitMode: LoginImageFitMode = pending ? fitMode : data.fitMode
  const previewUrl = pending?.previewUrl ?? data.effectiveImageUrl
  const previewWidth = pending?.width ?? data.originalWidth
  const previewHeight = pending?.height ?? data.originalHeight
  const previewRatio = pending ? previewWidth / previewHeight : data.aspectRatio
  const warnings = pending ? pending.warnings : data.warnings

  return (
    <article className="login-slot-card">
      <div className="login-slot-card__header">
        <div>
          <h2>{meta.label}</h2>
          <p>{meta.hint}</p>
        </div>
        {data.isDefault && !pending && <span className="badge badge--draft">Imagen por defecto</span>}
      </div>

      <div className="login-slot-card__preview" data-fit={previewFitMode.toLowerCase()}>
        <img src={previewUrl} alt={`Vista previa de ${meta.label}`} style={{ objectFit: previewFitMode === 'Cover' ? 'cover' : 'contain' }} />
      </div>

      <dl className="login-slot-card__meta">
        <div><dt>Dimensiones</dt><dd>{previewWidth}×{previewHeight}px</dd></div>
        <div><dt>Proporción</dt><dd>{previewRatio.toFixed(2)}:1 <span className="login-slot-card__muted">(recomendado {RECOMMENDED_RATIO.toFixed(2)}:1 ≈ 4:3)</span></dd></div>
        <div><dt>Mínimo / recomendado</dt><dd>{meta.minWidth}×{meta.minHeight}px / {meta.recommendedWidth}×{meta.recommendedHeight}px</dd></div>
        {data.updatedAtUtc && !pending && <div><dt>Actualizado</dt><dd>{new Date(data.updatedAtUtc).toLocaleString()}</dd></div>}
      </dl>

      {warnings.length > 0 && (
        <ul className="login-slot-card__warnings">
          {warnings.map((warning) => <li key={warning.code}>{warning.message}</li>)}
        </ul>
      )}

      {error && <StatusMessage kind="error" message={error} />}

      <div className="login-slot-card__fit">
        <label>
          Modo de ajuste
          <select value={fitMode} onChange={(e) => setFitMode(e.target.value as LoginImageFitMode)}>
            <option value="Contain">Contain (muestra la imagen completa, puede dejar bandas)</option>
            <option value="Cover">Cover (llena el panel, puede recortar)</option>
          </select>
        </label>
        {fitMode !== data.fitMode && !pending && (
          <button type="button" className="btn btn-secondary" onClick={saveFitMode} disabled={savingFitMode}>
            {savingFitMode ? 'Guardando...' : 'Guardar modo de ajuste'}
          </button>
        )}
      </div>

      <div className="login-slot-card__actions">
        <input ref={input} className="visually-hidden" type="file" accept="image/jpeg,image/png,image/webp" onChange={(e) => void choose(e.target.files?.[0])} />
        <button type="button" className="btn btn-secondary" onClick={() => input.current?.click()}>Seleccionar imagen</button>
        {pending && (
          <>
            <button type="button" className="btn btn-primary" onClick={upload} disabled={uploading}>{uploading ? 'Guardando...' : 'Guardar imagen'}</button>
            <button type="button" className="btn btn-secondary" onClick={cancelSelection} disabled={uploading}>Cancelar</button>
          </>
        )}
        {!pending && !data.isDefault && (
          <button type="button" className="btn btn-secondary" onClick={restoreDefault} disabled={restoring}>
            {restoring ? 'Restaurando...' : 'Restaurar imagen por defecto'}
          </button>
        )}
      </div>
    </article>
  )
}

export default function AdminLoginAppearancePage() {
  const [slots, setSlots] = useState<AdminLoginAppearanceSlot[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [helpOpen, setHelpOpen] = useState(false)

  useEffect(() => {
    let active = true
    api.get<AdminLoginAppearanceSlot[]>('/admin/login-appearance')
      .then((data) => { if (active) setSlots(data) })
      .catch((reason) => { if (active) setError(reason instanceof ApiError ? reason.message : 'No se pudo cargar la apariencia del login.') })
    return () => { active = false }
  }, [])

  function updateSlot(updated: AdminLoginAppearanceSlot) {
    setSlots((current) => current?.map((slot) => (slot.slot === updated.slot ? updated : slot)) ?? current)
  }

  return (
    <div>
      <div className="breadcrumb"><Link to="/admin">← Volver a Administración</Link></div>
      <div className="admin-header">
        <div>
          <span className="admin-eyebrow">LOGIN</span>
          <h1>Apariencia del login</h1>
          <p className="admin-help">Configurá las imágenes de los cuatro paneles de la pantalla de inicio de sesión. Proporción recomendada: 4:3.</p>
        </div>
        <button type="button" className="btn btn-secondary" onClick={() => setHelpOpen(true)}>
          ? Ayuda sobre las imágenes
        </button>
      </div>

      <LoginAppearanceHelpModal open={helpOpen} onClose={() => setHelpOpen(false)} />

      {error && <StatusMessage kind="error" message={error} />}
      {!slots && !error && <StatusMessage kind="loading" message="Cargando apariencia..." />}

      {slots && (
        <div className="login-slots-grid">
          {SLOT_ORDER.map((slot) => {
            const data = slots.find((x) => x.slot === slot)
            return data ? <SlotCard key={slot} slot={slot} data={data} onChanged={updateSlot} /> : null
          })}
        </div>
      )}
    </div>
  )
}
