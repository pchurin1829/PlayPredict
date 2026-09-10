import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Competition, Edition, ImportChange, MatchImportClassification, MatchImportConfirmationResponse, MatchImportPreviewResponse, MatchImportPreviewRow } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import ConfirmModal from '../components/ConfirmModal'

const classificationLabels: Record<MatchImportClassification, string> = {
  MatchCreate: 'Nuevo',
  MatchUpdate: 'Actualizable',
  MatchUnchanged: 'Sin cambios',
  MatchFinishedConflict: 'Conflicto: partido finalizado',
  MatchTeamChangeConflict: 'Conflicto: cambio de equipos',
  MatchRoundChangeConflict: 'Conflicto: cambio de fecha',
  UnresolvedTeamError: 'Equipo no resuelto',
  DuplicateMatchRowError: 'Fila duplicada',
  StructuralError: 'Error',
}

function Changes({ changes }: { changes: ImportChange[] }) {
  if (!changes.length) return null
  return <ul className="import-changes">{changes.map(change => <li key={change.field}><strong>{change.field}:</strong> {change.currentValue || '—'} → {change.proposedValue || '—'}</li>)}</ul>
}

export default function MatchFixtureImportPage() {
  const [competitions, setCompetitions] = useState<Competition[]>([])
  const [editions, setEditions] = useState<Edition[]>([])
  const [competitionId, setCompetitionId] = useState('')
  const [editionId, setEditionId] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<MatchImportPreviewResponse | null>(null)
  const [result, setResult] = useState<MatchImportConfirmationResponse | null>(null)
  const [busy, setBusy] = useState(false)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState('relevant')

  useEffect(() => {
    api.get<Competition[]>('/competitions').then(setCompetitions).catch(() => setCompetitions([]))
  }, [])

  useEffect(() => {
    if (!competitionId) {
      setEditions([])
      return
    }
    api.get<Edition[]>(`/competitions/${competitionId}/editions`).then(setEditions).catch(() => setEditions([]))
  }, [competitionId])

  const details = useMemo<MatchImportPreviewRow[]>(() => {
    if (!preview) return []
    if (filter === 'all') return preview.matches
    if (filter === 'relevant') return preview.matches.filter(row => row.classification !== 'MatchUnchanged')
    return preview.matches.filter(row => row.classification === filter)
  }, [preview, filter])

  function resetAnalysis() { setPreview(null); setResult(null); setError(null); setConfirmOpen(false) }

  async function analyze() {
    if (!editionId || !file) return
    setBusy(true); setError(null); setResult(null)
    try {
      const form = new FormData(); form.append('editionId', editionId); form.append('file', file)
      setPreview(await api.upload<MatchImportPreviewResponse>('/admin/match-import/preview', form))
      setFilter('relevant')
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo analizar el archivo.')
    } finally { setBusy(false) }
  }

  async function confirm() {
    if (!preview?.canConfirm || !editionId || !file) return
    setConfirmOpen(false)
    setBusy(true); setError(null)
    try {
      const form = new FormData(); form.append('editionId', editionId); form.append('file', file); form.append('expectedHash', preview.hash)
      setResult(await api.upload<MatchImportConfirmationResponse>('/admin/match-import/confirm', form))
      setPreview(null)
    } catch (reason) {
      const message = reason instanceof ApiError ? reason.message : 'No se pudo confirmar la importación.'
      setError(message)
      // Si el archivo ya no coincide con el analizado, el preview pierde validez.
      if (/hash|huella/i.test(message)) resetAnalysis()
    } finally { setBusy(false) }
  }

  if (result) return <div className="import-page">
    <div className="admin-header"><div><span className="admin-eyebrow">IMPORTACIÓN COMPLETADA</span><h1>Fixture</h1></div></div>
    <StatusMessage kind={result.status === 'Success' ? 'success' : 'error'} message={result.message} />
    <div className="import-summary-grid">
      <section className="import-summary-card"><h2>PARTIDOS</h2><dl>
        <div><dt>Total</dt><dd>{result.matches.created + result.matches.updated + result.matches.unchanged}</dd></div>
        <div><dt>Creados</dt><dd>{result.matches.created}</dd></div>
        <div><dt>Actualizados</dt><dd>{result.matches.updated}</dd></div>
        <div><dt>Sin cambios</dt><dd>{result.matches.unchanged}</dd></div>
      </dl></section>
    </div>
    {result.issues.length > 0 && <section className="import-issues"><h2>Problemas</h2>{result.issues.map((issue, index) => <article key={`${issue.code}-${issue.rowNumber}-${index}`}><strong>{issue.sheetName || 'ARCHIVO'}{issue.rowNumber ? ` · Fila ${issue.rowNumber}` : ''}</strong><span>{issue.message}</span></article>)}</section>}
    <Link className="btn btn-primary" to="/admin/fixture">Volver a Fixture</Link>
  </div>

  return <div className="import-page">
    <div className="breadcrumb"><Link to="/admin/fixture">← Volver a Fixture</Link></div>
    <div className="admin-header"><div><h1>Importar Fixture</h1><p className="admin-help">Analizá un archivo XLS/XLSX antes de confirmar cualquier cambio. Nada se aplica hasta la confirmación explícita.</p></div></div>
    {error && <StatusMessage kind="error" message={error} />}
    <section className="form-card import-form">
      <div className="form-field"><label htmlFor="importCompetition">Competencia</label><select id="importCompetition" value={competitionId} disabled={busy} onChange={event => { setCompetitionId(event.target.value); setEditionId(''); resetAnalysis() }} required><option value="">Seleccionar competencia</option>{competitions.map(option => <option key={option.id} value={option.id}>{option.name}</option>)}</select></div>
      <div className="form-field"><label htmlFor="importEdition">Edición</label><select id="importEdition" value={editionId} disabled={busy || !competitionId} onChange={event => { setEditionId(event.target.value); resetAnalysis() }} required><option value="">Seleccionar edición</option>{editions.map(option => <option key={option.id} value={option.id}>{option.name}</option>)}</select></div>
      <div className="form-field"><label htmlFor="importFile">Archivo XLS/XLSX</label><input id="importFile" type="file" accept=".xls,.xlsx" disabled={busy} onChange={event => { setFile(event.target.files?.[0] ?? null); resetAnalysis() }} /><small>Máximo 10 MB. Hoja IMPORTAR_PARTIDOS con columnas FECHA_NRO, FECHA, HORA, LOCAL, VISITANTE, ESTADO. El archivo original se reenviará al confirmar.</small></div>
      <div className="form-actions"><button type="button" className="btn btn-primary" disabled={busy || !editionId || !file} onClick={analyze}>{busy ? 'Analizando...' : 'Analizar archivo'}</button><Link className="btn btn-secondary" to="/admin/fixture">Cancelar</Link></div>
    </section>

    {preview && <>
      <div className="import-summary-grid"><section className="import-summary-card"><h2>PARTIDOS</h2><dl>
        <div><dt>Total</dt><dd>{preview.summary.total}</dd></div>
        <div><dt>Nuevos</dt><dd>{preview.summary.create}</dd></div>
        <div><dt>Actualizables</dt><dd>{preview.summary.update}</dd></div>
        <div><dt>Sin cambios</dt><dd>{preview.summary.unchanged}</dd></div>
        <div><dt>Conflictos</dt><dd>{preview.summary.conflicts}</dd></div>
        <div><dt>Errores</dt><dd>{preview.summary.errors}</dd></div>
      </dl></section></div>
      {preview.issues.length > 0 && <section className="import-issues"><h2>Problemas estructurales</h2>{preview.issues.map((issue, index) => <article key={`${issue.code}-${issue.rowNumber}-${index}`}><strong>{issue.sheetName || 'ARCHIVO'}{issue.rowNumber ? ` · Fila ${issue.rowNumber}` : ''}</strong><span>{issue.message}</span></article>)}</section>}
      <section className="import-details"><div className="import-details__header"><h2>Detalle</h2><div className="form-field"><label htmlFor="detailFilter">Mostrar</label><select id="detailFilter" value={filter} onChange={event => setFilter(event.target.value)}><option value="relevant">Relevantes</option><option value="all">Todas</option><option value="MatchCreate">Nuevos</option><option value="MatchUpdate">Actualizables</option><option value="MatchUnchanged">Sin cambios</option><option value="MatchFinishedConflict">Conflicto: finalizado</option><option value="MatchTeamChangeConflict">Conflicto: equipos</option><option value="MatchRoundChangeConflict">Conflicto: fecha</option><option value="UnresolvedTeamError">Equipo no resuelto</option><option value="DuplicateMatchRowError">Duplicadas</option><option value="StructuralError">Errores</option></select></div></div>
        {details.length === 0 ? <p className="admin-help">No hay filas para este filtro.</p> : <div className="table-wrap"><table className="admin-table"><thead><tr><th>Origen</th><th>Partido</th><th>Clasificación</th><th>Detalle</th></tr></thead><tbody>{details.map(row => <tr key={`${row.sheet}-${row.rowNumber}`}><td><strong>{row.sheet}</strong><small>Fila {row.rowNumber}</small></td><td>{row.entity}</td><td><span className={`import-status import-status--${row.classification}`}>{classificationLabels[row.classification]}</span></td><td>{row.message}<Changes changes={row.proposedChanges} /></td></tr>)}</tbody></table></div>}
      </section>
      {!preview.canConfirm && <StatusMessage kind="error" message="Corregí los errores del archivo y volvé a analizarlo." />}
      <div className="form-actions"><button type="button" className="btn btn-primary" disabled={busy || !preview.canConfirm} onClick={() => setConfirmOpen(true)}>{busy ? 'Confirmando...' : 'Confirmar importación'}</button><Link className="btn btn-secondary" to="/admin/fixture">Cancelar</Link></div>
    </>}

    <ConfirmModal
      open={confirmOpen}
      title="Confirmar importación de fixture"
      message={preview ? `Se crearán ${preview.summary.create} partidos y se actualizarán ${preview.summary.update} en la edición seleccionada. Esta acción no se puede deshacer. ¿Confirmás?` : ''}
      confirmLabel="Sí, importar"
      cancelLabel="Revisar antes"
      onConfirm={confirm}
      onCancel={() => setConfirmOpen(false)}
    />
  </div>
}
