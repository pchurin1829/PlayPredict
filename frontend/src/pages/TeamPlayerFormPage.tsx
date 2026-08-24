import { useEffect, useRef, useState, type DragEvent, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { TeamPlayer } from '../api/types'
import StatusMessage from '../components/StatusMessage'

const FOOTBALL_POSITIONS = ['Arquero', 'Defensor', 'Mediocampista', 'Delantero'] as const
const ACCEPTED_PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_ORIGINAL_PHOTO_BYTES = 8 * 1024 * 1024
const initials = (first: string, last: string) => `${first[0] ?? ''}${last[0] ?? ''}`.toUpperCase() || 'JP'
const realName = (player: Pick<TeamPlayer, 'firstName' | 'lastName'>) => `${player.firstName} ${player.lastName}`.trim()

async function optimizePhoto(file: File): Promise<File> {
  if (!ACCEPTED_PHOTO_TYPES.includes(file.type)) throw new Error('Usá una imagen JPG, PNG o WEBP.')
  if (file.size > MAX_ORIGINAL_PHOTO_BYTES) throw new Error('La imagen original no puede superar los 8 MB.')
  const bitmap = await createImageBitmap(file)
  const side = Math.min(bitmap.width, bitmap.height)
  const canvas = document.createElement('canvas')
  canvas.width = 512
  canvas.height = 512
  canvas.getContext('2d')?.drawImage(bitmap, (bitmap.width - side) / 2, (bitmap.height - side) / 2, side, side, 0, 0, 512, 512)
  bitmap.close()
  const blob = await new Promise<Blob | null>(resolve => canvas.toBlob(resolve, 'image/webp', 0.82))
  if (!blob) throw new Error('No se pudo procesar la imagen seleccionada.')
  if (blob.size > 1_500_000) throw new Error('La imagen procesada sigue siendo demasiado grande.')
  return new File([blob], 'player-photo.webp', { type: 'image/webp' })
}

export default function TeamPlayerFormPage() {
  const { teamId, playerId } = useParams()
  const navigate = useNavigate()
  const edit = Boolean(playerId)
  const fileInput = useRef<HTMLInputElement>(null)
  const [resolvedTeamId, setResolvedTeamId] = useState(teamId ?? '')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [nickname, setNickname] = useState('')
  const [shirtNumber, setShirtNumber] = useState('')
  const [position, setPosition] = useState('')
  const [photoUrl, setPhotoUrl] = useState<string | null>(null)
  const [photoFile, setPhotoFile] = useState<File | null>(null)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)
  const [removePhoto, setRemovePhoto] = useState(false)
  const [dragging, setDragging] = useState(false)
  const [active, setActive] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!edit) return
    api.get<TeamPlayer>(`/team-players/${playerId}`).then(player => {
      setResolvedTeamId(String(player.teamId)); setFirstName(player.firstName); setLastName(player.lastName)
      setNickname(player.displayName.toLocaleLowerCase() === realName(player).toLocaleLowerCase() ? '' : player.displayName)
      setShirtNumber(player.shirtNumber == null ? '' : String(player.shirtNumber)); setPosition(player.position ?? '')
      setPhotoUrl(player.photoUrl); setActive(player.active)
    }).catch(reason => setError(reason.message))
  }, [edit, playerId])

  useEffect(() => () => { if (photoPreview) URL.revokeObjectURL(photoPreview) }, [photoPreview])

  async function choosePhoto(file?: File) {
    if (!file) return
    setError(null)
    try {
      const optimized = await optimizePhoto(file)
      if (photoPreview) URL.revokeObjectURL(photoPreview)
      setPhotoFile(optimized); setPhotoPreview(URL.createObjectURL(optimized)); setRemovePhoto(false)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'No se pudo procesar la imagen.')
      if (fileInput.current) fileInput.current.value = ''
    }
  }

  function dropPhoto(event: DragEvent<HTMLDivElement>) { event.preventDefault(); setDragging(false); void choosePhoto(event.dataTransfer.files[0]) }
  function clearPhoto() {
    if (photoPreview) URL.revokeObjectURL(photoPreview)
    setPhotoFile(null); setPhotoPreview(null); setRemovePhoto(Boolean(photoUrl))
    if (fileInput.current) fileInput.current.value = ''
  }

  async function submit(event: FormEvent) {
    event.preventDefault(); setSaving(true); setError(null)
    try {
      const body = { firstName, lastName, displayName: nickname || null, shirtNumber: shirtNumber === '' ? null : Number(shirtNumber), position: position || null, active, photoUrl }
      const saved = edit ? await api.put<TeamPlayer>(`/team-players/${playerId}`, body) : await api.post<TeamPlayer>(`/teams/${resolvedTeamId}/players`, body)
      if (photoFile) { const form = new FormData(); form.append('file', photoFile); await api.upload<TeamPlayer>(`/team-players/${saved.id}/photo`, form) }
      else if (removePhoto) await api.del<TeamPlayer>(`/team-players/${saved.id}/photo`)
      navigate(`/admin/teams/${resolvedTeamId}/players`)
    } catch (reason) { setError(reason instanceof ApiError ? reason.message : reason instanceof Error ? reason.message : 'No se pudo guardar el jugador.') }
    finally { setSaving(false) }
  }

  const back = `/admin/teams/${resolvedTeamId}/players`
  const visiblePhoto = photoPreview ?? (removePhoto ? null : photoUrl)
  return <div><div className="breadcrumb"><Link to={back}>← Volver a Plantel</Link></div><div className="admin-header"><h1>{edit ? 'Editar Jugador' : 'Nuevo Jugador'}</h1></div>{error && <StatusMessage kind="error" message={error} />}<form className="form-card team-player-form" onSubmit={submit}>
    <aside className="team-player-form__photo">
      <div className={`player-photo-dropzone${dragging ? ' player-photo-dropzone--dragging' : ''}`} onDragOver={event => { event.preventDefault(); setDragging(true) }} onDragLeave={() => setDragging(false)} onDrop={dropPhoto} onClick={() => fileInput.current?.click()} role="button" tabIndex={0} onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') fileInput.current?.click() }}>
        <span className="player-avatar player-avatar--large">{visiblePhoto ? <img src={visiblePhoto} alt="Vista previa del jugador" /> : <span>{initials(firstName, lastName)}</span>}</span>
        <span>Arrastrá una foto aquí</span><small>o</small><strong>Seleccionar archivo</strong><small>JPG, PNG o WEBP · máximo 8 MB</small>
        <input ref={fileInput} className="visually-hidden" type="file" accept="image/jpeg,image/png,image/webp" onChange={event => void choosePhoto(event.target.files?.[0])} />
      </div>
      {(visiblePhoto || photoUrl) && <button className="btn btn-secondary player-photo-remove" type="button" onClick={clearPhoto}>Quitar foto</button>}
    </aside>
    <div className="team-player-form__fields"><div className="form-row"><div className="form-field"><label>Nombre</label><input value={firstName} onChange={event => setFirstName(event.target.value)} required /></div><div className="form-field"><label>Apellido</label><input value={lastName} onChange={event => setLastName(event.target.value)} required /></div></div><div className="form-field"><label>Apodo (opcional)</label><input value={nickname} onChange={event => setNickname(event.target.value)} placeholder="Ej.: Leo" /></div><div className="form-row"><div className="form-field"><label>Número (opcional)</label><input type="number" min="0" max="99" value={shirtNumber} onChange={event => setShirtNumber(event.target.value)} /></div><div className="form-field"><label>Posición (opcional)</label><select value={position} onChange={event => setPosition(event.target.value)}><option value="">Sin especificar</option>{position && !FOOTBALL_POSITIONS.includes(position as typeof FOOTBALL_POSITIONS[number]) && <option value={position}>{position} (existente)</option>}{FOOTBALL_POSITIONS.map(item => <option value={item} key={item}>{item}</option>)}</select></div></div><label className="checkbox-label"><input type="checkbox" checked={active} onChange={event => setActive(event.target.checked)} /> Jugador activo</label><div className="form-actions"><button className="btn btn-primary" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</button><Link className="btn btn-secondary" to={back}>Cancelar</Link></div></div>
  </form></div>
}
