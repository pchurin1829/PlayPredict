import { useEffect, useRef, useState, type DragEvent } from 'react'

const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_BYTES = 8 * 1024 * 1024

async function optimizeImage(file: File): Promise<File> {
  if (!ACCEPTED_TYPES.includes(file.type)) throw new Error('Usá una imagen JPG, PNG o WEBP.')
  if (file.size > MAX_BYTES) throw new Error('La imagen no puede superar los 8 MB.')
  const bitmap = await createImageBitmap(file)
  const maxSide = 1024
  const scale = Math.min(1, maxSide / Math.max(bitmap.width, bitmap.height))
  const canvas = document.createElement('canvas')
  canvas.width = Math.max(1, Math.round(bitmap.width * scale)); canvas.height = Math.max(1, Math.round(bitmap.height * scale))
  canvas.getContext('2d')?.drawImage(bitmap, 0, 0, canvas.width, canvas.height); bitmap.close()
  const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/webp', .86))
  if (!blob) throw new Error('No se pudo procesar la imagen seleccionada.')
  return new File([blob], 'image.webp', { type: 'image/webp' })
}

interface Props {
  label: string
  currentUrl: string | null
  fallback: string
  onSelectionChange: (file: File | null, removeExisting: boolean) => void
  onError: (message: string) => void
}

export default function ImageUploadField({ label, currentUrl, fallback, onSelectionChange, onError }: Props) {
  const input = useRef<HTMLInputElement>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [removed, setRemoved] = useState(false)
  const [dragging, setDragging] = useState(false)
  useEffect(() => () => { if (preview) URL.revokeObjectURL(preview) }, [preview])

  async function choose(file?: File) {
    if (!file) return
    try {
      const optimized = await optimizeImage(file)
      if (preview) URL.revokeObjectURL(preview)
      setPreview(URL.createObjectURL(optimized)); setRemoved(false); onSelectionChange(optimized, false)
    } catch (reason) { onError(reason instanceof Error ? reason.message : 'No se pudo procesar la imagen.') }
  }
  function drop(event: DragEvent<HTMLDivElement>) { event.preventDefault(); setDragging(false); void choose(event.dataTransfer.files[0]) }
  function clear() {
    if (preview) URL.revokeObjectURL(preview)
    setPreview(null); setRemoved(true); if (input.current) input.current.value = ''
    onSelectionChange(null, Boolean(currentUrl))
  }
  const visible = preview ?? (removed ? null : currentUrl)

  return <div className="image-upload-field">
    <label>{label}</label>
    <div className={`image-upload-field__drop${dragging ? ' image-upload-field__drop--dragging' : ''}`} onDragOver={event => { event.preventDefault(); setDragging(true) }} onDragLeave={() => setDragging(false)} onDrop={drop}>
      <span className="image-upload-field__preview">{visible ? <img src={visible} alt={`Vista previa: ${label}`} /> : <strong>{fallback}</strong>}</span>
      <div><span>Arrastrá una imagen aquí</span><small>JPG, PNG o WEBP · máximo 8 MB</small><button className="btn btn-secondary" type="button" onClick={() => input.current?.click()}>{visible ? 'Reemplazar' : 'Seleccionar archivo'}</button></div>
      <input ref={input} className="visually-hidden" type="file" accept="image/jpeg,image/png,image/webp" onChange={event => void choose(event.target.files?.[0])} />
    </div>
    {visible && <button className="btn btn-secondary image-upload-field__remove" type="button" onClick={clear}>Quitar imagen</button>}
  </div>
}
