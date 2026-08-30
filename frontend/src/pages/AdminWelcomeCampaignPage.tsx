import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { ActiveWelcomeCampaignSlide, WelcomeCampaign, WelcomeCampaignFitMode, WelcomeCampaignSlide } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import WelcomeCampaignPlayer from '../components/welcomeCampaign/WelcomeCampaignPlayer'
import WelcomeCampaignHelpModal from './WelcomeCampaignHelpModal'
import './AdminWelcomeCampaignPage.css'

const MAX_SLIDES = 3

function toDatetimeLocalValue(iso: string | null): string {
  if (!iso) return ''
  const date = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function fromDatetimeLocalValue(value: string): string | null {
  if (!value) return null
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? null : date.toISOString()
}

function toActiveSlides(slides: WelcomeCampaignSlide[]): ActiveWelcomeCampaignSlide[] {
  return slides.map((s) => ({ id: s.id, imageUrl: s.imageUrl, sortOrder: s.sortOrder, durationSeconds: s.durationSeconds, fitMode: s.fitMode }))
}

function SlideCard({ slide, campaignId, isFirst, isLast, onChanged, onError }: {
  slide: WelcomeCampaignSlide
  campaignId: number
  isFirst: boolean
  isLast: boolean
  onChanged: (slides: WelcomeCampaignSlide[]) => void
  onError: (message: string) => void
}) {
  const input = useRef<HTMLInputElement>(null)
  const [duration, setDuration] = useState(slide.durationSeconds)
  const [fitMode, setFitMode] = useState<WelcomeCampaignFitMode>(slide.fitMode)
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [moving, setMoving] = useState(false)
  const [deleting, setDeleting] = useState(false)

  useEffect(() => { setDuration(slide.durationSeconds); setFitMode(slide.fitMode) }, [slide.durationSeconds, slide.fitMode])

  const dirty = duration !== slide.durationSeconds || fitMode !== slide.fitMode

  async function save() {
    setSaving(true)
    try {
      const updated = await api.put<WelcomeCampaignSlide>(`/admin/welcome-campaigns/${campaignId}/slides/${slide.id}`, { durationSeconds: duration, fitMode })
      onChanged([updated])
    } catch (reason) {
      onError(reason instanceof ApiError ? reason.message : 'No se pudo guardar la duración/ajuste.')
    } finally {
      setSaving(false)
    }
  }

  async function replaceImage(file?: File) {
    if (!file) return
    setUploading(true)
    try {
      const form = new FormData()
      form.append('file', file)
      const updated = await api.upload<WelcomeCampaignSlide>(`/admin/welcome-campaigns/${campaignId}/slides/${slide.id}/image`, form)
      onChanged([updated])
    } catch (reason) {
      onError(reason instanceof ApiError ? reason.message : 'No se pudo reemplazar la imagen.')
    } finally {
      setUploading(false)
      if (input.current) input.current.value = ''
    }
  }

  async function move(direction: -1 | 1) {
    setMoving(true)
    try {
      const slides = await api.put<WelcomeCampaignSlide[]>(`/admin/welcome-campaigns/${campaignId}/slides/${slide.id}/order`, { sortOrder: slide.sortOrder + direction })
      onChanged(slides)
    } catch (reason) {
      onError(reason instanceof ApiError ? reason.message : 'No se pudo cambiar el orden.')
    } finally {
      setMoving(false)
    }
  }

  async function remove() {
    setDeleting(true)
    try {
      const slides = await api.del<WelcomeCampaignSlide[]>(`/admin/welcome-campaigns/${campaignId}/slides/${slide.id}`)
      onChanged(slides)
    } catch (reason) {
      onError(reason instanceof ApiError ? reason.message : 'No se pudo eliminar la imagen.')
    } finally {
      setDeleting(false)
    }
  }

  return (
    <article className="wc-slide-card">
      <div className="wc-slide-card__order">
        <button type="button" className="btn btn-secondary" disabled={isFirst || moving} onClick={() => move(-1)} aria-label="Mover antes">▲</button>
        <span>Orden {slide.sortOrder}</span>
        <button type="button" className="btn btn-secondary" disabled={isLast || moving} onClick={() => move(1)} aria-label="Mover después">▼</button>
      </div>

      <div className="wc-slide-card__preview">
        <img src={slide.imageUrl} alt={`Vista previa slide ${slide.sortOrder}`} style={{ objectFit: fitMode === 'Cover' ? 'cover' : 'contain' }} />
      </div>

      <dl className="wc-slide-card__meta">
        <div><dt>Dimensiones</dt><dd>{slide.originalWidth}×{slide.originalHeight}px</dd></div>
      </dl>

      {slide.warnings.length > 0 && (
        <ul className="wc-slide-card__warnings">
          {slide.warnings.map((w) => <li key={w.code}>{w.message}</li>)}
        </ul>
      )}

      <label className="wc-slide-card__field">
        Duración
        <div className="wc-slide-card__duration">
          <input type="number" min={1} max={10} step={0.5} value={duration} onChange={(e) => setDuration(Number(e.target.value))} />
          <span>segundos (1 a 10)</span>
        </div>
      </label>

      <label className="wc-slide-card__field">
        Modo de ajuste
        <select value={fitMode} onChange={(e) => setFitMode(e.target.value as WelcomeCampaignFitMode)}>
          <option value="Cover">Cubrir todo el panel (Cover)</option>
          <option value="Contain">Mostrar imagen completa (Contain)</option>
        </select>
      </label>

      {dirty && <button type="button" className="btn btn-secondary" onClick={save} disabled={saving}>{saving ? 'Guardando...' : 'Guardar ajustes'}</button>}

      <div className="wc-slide-card__actions">
        <input ref={input} className="visually-hidden" type="file" accept="image/jpeg,image/png,image/webp" onChange={(e) => void replaceImage(e.target.files?.[0])} />
        <button type="button" className="btn btn-secondary" onClick={() => input.current?.click()} disabled={uploading}>{uploading ? 'Subiendo...' : 'Reemplazar imagen'}</button>
        <button type="button" className="btn btn-secondary" onClick={remove} disabled={deleting}>{deleting ? 'Eliminando...' : 'Eliminar'}</button>
      </div>
    </article>
  )
}

function AddSlideCard({ campaignId, onAdded, onError }: { campaignId: number; onAdded: (slide: WelcomeCampaignSlide) => void; onError: (message: string) => void }) {
  const input = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)

  async function choose(file?: File) {
    if (!file) return
    setUploading(true)
    try {
      const form = new FormData()
      form.append('file', file)
      const slide = await api.upload<WelcomeCampaignSlide>(`/admin/welcome-campaigns/${campaignId}/slides`, form)
      onAdded(slide)
    } catch (reason) {
      onError(reason instanceof ApiError ? reason.message : 'No se pudo agregar la imagen.')
    } finally {
      setUploading(false)
      if (input.current) input.current.value = ''
    }
  }

  return (
    <article className="wc-slide-card wc-slide-card--add">
      <input ref={input} className="visually-hidden" type="file" accept="image/jpeg,image/png,image/webp" onChange={(e) => void choose(e.target.files?.[0])} />
      <button type="button" className="btn btn-primary" onClick={() => input.current?.click()} disabled={uploading}>
        {uploading ? 'Subiendo...' : '+ Agregar imagen'}
      </button>
    </article>
  )
}

function CampaignEditor({ campaign, onChanged, onDeleted }: {
  campaign: WelcomeCampaign
  onChanged: (campaign: WelcomeCampaign) => void
  onDeleted: () => void
}) {
  const [name, setName] = useState(campaign.name)
  const [validFrom, setValidFrom] = useState(toDatetimeLocalValue(campaign.validFromUtc))
  const [validTo, setValidTo] = useState(toDatetimeLocalValue(campaign.validToUtc))
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [activating, setActivating] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [previewOpen, setPreviewOpen] = useState(false)

  useEffect(() => {
    setName(campaign.name)
    setValidFrom(toDatetimeLocalValue(campaign.validFromUtc))
    setValidTo(toDatetimeLocalValue(campaign.validToUtc))
  }, [campaign.id, campaign.name, campaign.validFromUtc, campaign.validToUtc])

  const dirty = name !== campaign.name || validFrom !== toDatetimeLocalValue(campaign.validFromUtc) || validTo !== toDatetimeLocalValue(campaign.validToUtc)

  function updateSlides(updater: (slides: WelcomeCampaignSlide[]) => WelcomeCampaignSlide[]) {
    onChanged({ ...campaign, slides: updater(campaign.slides) })
  }

  async function saveDetails() {
    setSaving(true)
    setError(null)
    try {
      const updated = await api.put<WelcomeCampaign>(`/admin/welcome-campaigns/${campaign.id}`, {
        name, validFromUtc: fromDatetimeLocalValue(validFrom), validToUtc: fromDatetimeLocalValue(validTo),
      })
      onChanged({ ...updated, slides: campaign.slides })
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo guardar la campaña.')
    } finally {
      setSaving(false)
    }
  }

  async function toggleActive() {
    setActivating(true)
    setError(null)
    try {
      const updated = await api.post<WelcomeCampaign>(`/admin/welcome-campaigns/${campaign.id}/${campaign.isActive ? 'deactivate' : 'activate'}`, {})
      onChanged(updated)
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo cambiar el estado de la campaña.')
    } finally {
      setActivating(false)
    }
  }

  async function remove() {
    setDeleting(true)
    setError(null)
    try {
      await api.del(`/admin/welcome-campaigns/${campaign.id}`)
      onDeleted()
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo eliminar la campaña.')
    } finally {
      setDeleting(false)
    }
  }

  const orderedSlides = [...campaign.slides].sort((a, b) => a.sortOrder - b.sortOrder)

  return (
    <div className="wc-editor form-card">
      <div className="wc-editor__title">
        <h2>Editando: {campaign.name}</h2>
        <span className={`badge ${campaign.isActive ? 'badge--active' : 'badge--inactive'}`}>{campaign.isActive ? 'Activa' : 'Inactiva'}</span>
      </div>

      {error && <StatusMessage kind="error" message={error} />}

      <div className="form-field">
        <label>Nombre de campaña</label>
        <input value={name} onChange={(e) => setName(e.target.value)} />
      </div>
      <div className="form-row">
        <div className="form-field">
          <label>Vigente desde (opcional)</label>
          <input type="datetime-local" value={validFrom} onChange={(e) => setValidFrom(e.target.value)} />
        </div>
        <div className="form-field">
          <label>Vigente hasta (opcional)</label>
          <input type="datetime-local" value={validTo} onChange={(e) => setValidTo(e.target.value)} />
        </div>
      </div>

      <h3 className="wc-editor__slides-title">Imágenes ({campaign.slides.length}/{MAX_SLIDES})</h3>
      <div className="wc-slides-grid">
        {orderedSlides.map((slide, index) => (
          <SlideCard
            key={slide.id}
            slide={slide}
            campaignId={campaign.id}
            isFirst={index === 0}
            isLast={index === orderedSlides.length - 1}
            onChanged={(updated) => updateSlides((slides) => {
              const map = new Map(slides.map((s) => [s.id, s]))
              updated.forEach((s) => map.set(s.id, s))
              return Array.from(map.values())
            })}
            onError={setError}
          />
        ))}
        {campaign.slides.length < MAX_SLIDES && (
          <AddSlideCard campaignId={campaign.id} onAdded={(slide) => updateSlides((slides) => [...slides, slide])} onError={setError} />
        )}
      </div>

      <section className="wc-editor__campaign-actions">
        <h3 className="wc-editor__slides-title">Acciones de campaña</h3>
        <div className="form-actions">
          {dirty && <button type="button" className="btn btn-primary" onClick={saveDetails} disabled={saving}>{saving ? 'Guardando...' : 'Guardar cambios'}</button>}
          <button type="button" className="btn btn-secondary" onClick={() => setPreviewOpen(true)} disabled={campaign.slides.length === 0}>Previsualizar campaña</button>
        </div>
        <div className="form-actions">
          <button type="button" className="btn btn-secondary" onClick={toggleActive} disabled={activating || (!campaign.isActive && campaign.slides.length === 0)}>
            {activating ? 'Procesando...' : campaign.isActive ? 'Desactivar campaña' : 'Activar campaña'}
          </button>
          {!campaign.isActive && (
            <button type="button" className="btn btn-secondary" onClick={remove} disabled={deleting}>{deleting ? 'Eliminando...' : 'Eliminar campaña'}</button>
          )}
        </div>
        {!campaign.isActive && campaign.slides.length === 0 && <p className="admin-help">Agregá al menos una imagen para poder activar la campaña.</p>}
      </section>

      {previewOpen && (
        <WelcomeCampaignPlayer slides={toActiveSlides(orderedSlides)} onFinished={() => setPreviewOpen(false)} closable />
      )}
    </div>
  )
}

export default function AdminWelcomeCampaignPage() {
  const [campaigns, setCampaigns] = useState<WelcomeCampaign[] | null>(null)
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [helpOpen, setHelpOpen] = useState(false)
  const [creating, setCreating] = useState(false)
  const [newName, setNewName] = useState('')
  const [createSaving, setCreateSaving] = useState(false)

  useEffect(() => {
    let active = true
    api.get<WelcomeCampaign[]>('/admin/welcome-campaigns')
      .then((data) => { if (active) setCampaigns(data) })
      .catch((reason) => { if (active) setError(reason instanceof ApiError ? reason.message : 'No se pudieron cargar las campañas.') })
    return () => { active = false }
  }, [])

  function replaceCampaign(updated: WelcomeCampaign) {
    setCampaigns((current) => current?.map((c) => (c.id === updated.id ? updated : c)) ?? current)
  }

  async function createCampaign() {
    if (!newName.trim()) return
    setCreateSaving(true)
    setError(null)
    try {
      const created = await api.post<WelcomeCampaign>('/admin/welcome-campaigns', { name: newName.trim(), validFromUtc: null, validToUtc: null })
      setCampaigns((current) => [created, ...(current ?? [])])
      setSelectedId(created.id)
      setCreating(false)
      setNewName('')
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo crear la campaña.')
    } finally {
      setCreateSaving(false)
    }
  }

  const selected = campaigns?.find((c) => c.id === selectedId) ?? null

  return (
    <div>
      <div className="breadcrumb"><Link to="/admin">← Volver a Administración</Link></div>
      <div className="admin-header">
        <div>
          <span className="admin-eyebrow">LOGIN</span>
          <h1>Campaña de bienvenida</h1>
          <p className="admin-help">Publicidad de bienvenida que ven los jugadores apenas inician sesión, antes de entrar a PlayPredict.</p>
        </div>
        <button type="button" className="btn btn-secondary" onClick={() => setHelpOpen(true)}>
          ? Ayuda sobre las imágenes
        </button>
      </div>

      <WelcomeCampaignHelpModal open={helpOpen} onClose={() => setHelpOpen(false)} />

      {error && <StatusMessage kind="error" message={error} />}
      {!campaigns && !error && <StatusMessage kind="loading" message="Cargando campañas..." />}

      {campaigns && (
        <>
          <div className="wc-list">
            {campaigns.length === 0 && !creating && <p className="admin-help">Todavía no creaste ninguna campaña.</p>}
            {campaigns.map((c) => (
              <div key={c.id} className={`wc-list__row${c.id === selectedId ? ' wc-list__row--selected' : ''}`}>
                <div>
                  <strong>{c.name}</strong>{' '}
                  <span className={`badge ${c.isActive ? 'badge--active' : 'badge--inactive'}`}>{c.isActive ? 'Activa' : 'Inactiva'}</span>
                  <div className="wc-list__meta">{c.slides.length} imagen{c.slides.length === 1 ? '' : 'es'}</div>
                </div>
                <button type="button" className="btn btn-secondary" onClick={() => setSelectedId(c.id)}>Editar</button>
              </div>
            ))}
          </div>

          {!creating && <button type="button" className="btn btn-primary" onClick={() => setCreating(true)}>+ Nueva campaña</button>}

          {creating && (
            <div className="wc-create form-card">
              <div className="form-field">
                <label>Nombre de la nueva campaña</label>
                <input value={newName} onChange={(e) => setNewName(e.target.value)} autoFocus />
              </div>
              <div className="form-actions">
                <button type="button" className="btn btn-primary" onClick={createCampaign} disabled={createSaving || !newName.trim()}>
                  {createSaving ? 'Creando...' : 'Crear campaña'}
                </button>
                <button type="button" className="btn btn-secondary" onClick={() => { setCreating(false); setNewName('') }}>Cancelar</button>
              </div>
            </div>
          )}

          {selected && (
            <CampaignEditor
              campaign={selected}
              onChanged={replaceCampaign}
              onDeleted={() => { setCampaigns((current) => current?.filter((c) => c.id !== selected.id) ?? current); setSelectedId(null) }}
            />
          )}
        </>
      )}
    </div>
  )
}
