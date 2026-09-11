import { useEffect, useRef, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { AdminLoginAppearanceSlot, LoginImageSlot } from '../api/types'
import { LOGIN_IMAGE_CONTRACTS, getLoginImageWarnings } from '../login/imageContracts'
import { resolveLoginCampaignUrl } from '../login/appearance'
import StatusMessage from '../components/StatusMessage'
import LoginAppearanceHelpModal from './LoginAppearanceHelpModal'
import './AdminLoginAppearancePage.css'

const SLOT_ORDER: LoginImageSlot[] = ['Main', 'AdTop', 'AdMiddle', 'AdBottom']

interface PendingSelection {
  file: File
  previewUrl: string
  width: number
  height: number
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
  const meta = LOGIN_IMAGE_CONTRACTS[slot]
  const input = useRef<HTMLInputElement>(null)
  const [pending, setPending] = useState<PendingSelection | null>(null)
  const [loadedImage, setLoadedImage] = useState<{ url: string; width: number; height: number } | null>(null)
  const [uploading, setUploading] = useState(false)
  const [restoring, setRestoring] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => () => { if (pending) URL.revokeObjectURL(pending.previewUrl) }, [pending])

  async function choose(file?: File) {
    if (!file) return
    setError(null)
    try {
      const { width, height } = await readImageDimensions(file)
      if (pending) URL.revokeObjectURL(pending.previewUrl)
      setPending({ file, previewUrl: URL.createObjectURL(file), width, height })
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

  const previewUrl = pending?.previewUrl ?? (slot === 'Main' ? resolveLoginCampaignUrl(data.effectiveImageUrl) : data.effectiveImageUrl)
  const actualImage = loadedImage?.url === previewUrl ? loadedImage : null
  const previewWidth = pending?.width ?? actualImage?.width ?? data.originalWidth
  const previewHeight = pending?.height ?? actualImage?.height ?? data.originalHeight
  const previewRatio = previewWidth / previewHeight
  // Replace only obsolete geometry advice from the server; retain other warnings.
  const warnings = [
    ...getLoginImageWarnings(slot, previewWidth, previewHeight, pending?.file.size),
    ...(!pending ? data.warnings.filter((warning) => !['LOW_RESOLUTION', 'ASPECT_RATIO_MISMATCH'].includes(warning.code)) : []),
  ]

  return (
    <article className="login-slot-card">
      <div className="login-slot-card__header">
        <div>
          <h2>{meta.label}</h2>
          <p>{meta.hint}</p>
        </div>
        {data.isDefault && !pending && <span className="badge badge--draft">Imagen por defecto</span>}
      </div>

      <div className="login-slot-card__preview" style={{ aspectRatio: `${meta.width} / ${meta.height}` }}>
        <img
          src={previewUrl}
          alt={`Vista previa de ${meta.label}`}
          style={{ objectFit: meta.fit, objectPosition: slot === 'Main' ? 'left center' : 'center' }}
          onLoad={(event) => setLoadedImage({ url: previewUrl, width: event.currentTarget.naturalWidth, height: event.currentTarget.naturalHeight })}
        />
      </div>

      <dl className="login-slot-card__meta">
        <div><dt>Dimensiones</dt><dd>{previewWidth}×{previewHeight}px</dd></div>
        <div><dt>Proporción</dt><dd>{previewRatio.toFixed(2)}:1 <span className="login-slot-card__muted">(recomendado {meta.ratio})</span></dd></div>
        <div><dt>Canvas recomendado</dt><dd>{meta.width}×{meta.height} px</dd></div>
        <div><dt>Formatos</dt><dd>{meta.formats}</dd></div>
        <div><dt>Safe area</dt><dd>{meta.safeArea} px desde cada borde</dd></div>
        <div><dt>Peso recomendado</dt><dd>≤{meta.recommendedKB} KB</dd></div>
        {data.updatedAtUtc && !pending && <div><dt>Actualizado</dt><dd>{new Date(data.updatedAtUtc).toLocaleString()}</dd></div>}
      </dl>

      {warnings.length > 0 && (
        <ul className="login-slot-card__warnings">
          {warnings.map((warning) => <li key={warning.code}>{warning.message}</li>)}
        </ul>
      )}

      {error && <StatusMessage kind="error" message={error} />}

      <p className="login-slot-card__muted">
        Ajuste fijo: {meta.fit === 'contain' ? 'imagen completa (contain), sin recortar la campaña.' : 'cubrir el slot (cover), con posible recorte si no es 3:2.'}
      </p>

      <div className="login-slot-card__actions">
        <input ref={input} className="visually-hidden" type="file" accept={meta.accept} onChange={(e) => void choose(e.target.files?.[0])} />
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
      <div className="admin-header">
        <div>
          <span className="admin-eyebrow">LOGIN</span>
          <h1>Apariencia del login</h1>
          <p className="admin-help">Configurá la campaña 8:9 y las tres publicidades 3:2. El estadio es un fondo fijo de PlayPredict, independiente de estas imágenes.</p>
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
