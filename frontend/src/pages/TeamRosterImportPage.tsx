import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { ImportChange, ImportPreviewClassification, RosterImportPreviewRow, TeamImportPreviewRow, TeamRosterImportConfirmationResponse, TeamRosterImportPreviewResponse } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { TEAM_SPORT_OPTIONS } from '../constants/sports'

type DetailRow = (TeamImportPreviewRow | RosterImportPreviewRow) & { kind: 'Equipo' | 'Jugador' }

const classificationLabels: Record<ImportPreviewClassification, string> = {
  TeamNew: 'Nuevo', TeamUnchanged: 'Sin cambios', TeamUpdatable: 'Actualizable',
  TeamSportConflict: 'Conflicto de deporte', TeamAmbiguousConflict: 'Conflicto ambiguo',
  PlayerNew: 'Nuevo', PlayerUnchanged: 'Sin cambios', PlayerUpdatable: 'Actualizable',
  PlayerAmbiguousConflict: 'Conflicto ambiguo', UnresolvedTeamError: 'Equipo no resuelto', StructuralError: 'Error',
}

function Summary({ title, values }: { title:string; values:{total:number;new:number;updatable:number;unchanged:number;conflicts:number;errors:number} }) {
  return <section className="import-summary-card"><h2>{title}</h2><dl>
    <div><dt>Total</dt><dd>{values.total}</dd></div><div><dt>Nuevos</dt><dd>{values.new}</dd></div>
    <div><dt>Actualizables</dt><dd>{values.updatable}</dd></div><div><dt>Sin cambios</dt><dd>{values.unchanged}</dd></div>
    <div><dt>Conflictos</dt><dd>{values.conflicts}</dd></div><div><dt>Errores</dt><dd>{values.errors}</dd></div>
  </dl></section>
}

function Changes({ changes }: { changes:ImportChange[] }) {
  if (!changes.length) return null
  return <ul className="import-changes">{changes.map(change => <li key={change.field}><strong>{change.field}:</strong> {change.currentValue || '—'} → {change.proposedValue || '—'}</li>)}</ul>
}

export default function TeamRosterImportPage() {
  const [sport, setSport] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<TeamRosterImportPreviewResponse | null>(null)
  const [result, setResult] = useState<TeamRosterImportConfirmationResponse | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState('relevant')

  const details = useMemo<DetailRow[]>(() => {
    if (!preview) return []
    const rows: DetailRow[] = [
      ...preview.teams.map(row => ({ ...row, kind: 'Equipo' as const })),
      ...preview.rosters.map(row => ({ ...row, kind: 'Jugador' as const })),
    ]
    if (filter === 'all') return rows
    if (filter === 'relevant') return rows.filter(row => !['TeamUnchanged', 'PlayerUnchanged'].includes(row.classification))
    return rows.filter(row => row.classification === filter)
  }, [preview, filter])

  function resetAnalysis() { setPreview(null); setResult(null); setError(null) }

  async function analyze() {
    if (!sport || !file) return
    setBusy(true); setError(null); setResult(null)
    try {
      const form = new FormData(); form.append('sport', sport); form.append('file', file)
      setPreview(await api.upload<TeamRosterImportPreviewResponse>('/admin/team-roster-import/preview', form))
      setFilter('relevant')
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo analizar el archivo.')
    } finally { setBusy(false) }
  }

  async function confirm() {
    if (!preview?.canConfirm || !file) return
    setBusy(true); setError(null)
    try {
      const form = new FormData(); form.append('sport', sport); form.append('file', file); form.append('expectedHash', preview.hash)
      setResult(await api.upload<TeamRosterImportConfirmationResponse>('/admin/team-roster-import/confirm', form))
      setPreview(null)
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo confirmar la importación.')
    } finally { setBusy(false) }
  }

  if (result) return <div className="import-page">
    <div className="admin-header"><div><span className="admin-eyebrow">IMPORTACIÓN COMPLETADA</span><h1>Equipos y Planteles</h1></div></div>
    <StatusMessage kind="success" message={result.message} />
    <div className="import-summary-grid">
      <Summary title="EQUIPOS" values={{ total: result.teams.created + result.teams.updated + result.teams.unchanged, new: result.teams.created, updatable: result.teams.updated, unchanged: result.teams.unchanged, conflicts: 0, errors: 0 }} />
      <Summary title="PLANTELES" values={{ total: result.rosters.created + result.rosters.updated + result.rosters.unchanged, new: result.rosters.created, updatable: result.rosters.updated, unchanged: result.rosters.unchanged, conflicts: 0, errors: 0 }} />
    </div>
    <Link className="btn btn-primary" to="/admin/teams">Volver a Equipos</Link>
  </div>

  return <div className="import-page">
    <div className="breadcrumb"><Link to="/admin/teams">← Volver a Equipos</Link></div>
    <div className="admin-header"><div><h1>Importar Equipos y Planteles</h1><p className="admin-help">Analizá un archivo XLS/XLSX antes de confirmar cualquier cambio.</p></div></div>
    {error && <StatusMessage kind="error" message={error} />}
    <section className="form-card import-form">
      <div className="form-field"><label htmlFor="importSport">Deporte</label><select id="importSport" value={sport} disabled={busy} onChange={event => { setSport(event.target.value); resetAnalysis() }} required><option value="">Seleccionar deporte</option>{TEAM_SPORT_OPTIONS.map(option => <option key={option} value={option}>{option}</option>)}</select></div>
      <div className="form-field"><label htmlFor="importFile">Archivo XLS/XLSX</label><input id="importFile" type="file" accept=".xls,.xlsx" disabled={busy} onChange={event => { setFile(event.target.files?.[0] ?? null); resetAnalysis() }} /><small>Máximo 10 MB. El archivo original se reenviará al confirmar.</small></div>
      <div className="form-actions"><button type="button" className="btn btn-primary" disabled={busy || !sport || !file} onClick={analyze}>{busy ? 'Analizando...' : 'Analizar archivo'}</button><Link className="btn btn-secondary" to="/admin/teams">Cancelar</Link></div>
    </section>

    {preview && <>
      <div className="import-summary-grid"><Summary title="EQUIPOS" values={preview.teamsSummary} /><Summary title="PLANTELES" values={preview.rostersSummary} /></div>
      {preview.issues.length > 0 && <section className="import-issues"><h2>Problemas estructurales</h2>{preview.issues.map((issue, index) => <article key={`${issue.code}-${issue.rowNumber}-${index}`}><strong>{issue.sheetName || 'ARCHIVO'}{issue.rowNumber ? ` · Fila ${issue.rowNumber}` : ''}</strong><span>{issue.message}</span></article>)}</section>}
      <section className="import-details"><div className="import-details__header"><h2>Detalle</h2><div className="form-field"><label htmlFor="detailFilter">Mostrar</label><select id="detailFilter" value={filter} onChange={event => setFilter(event.target.value)}><option value="relevant">Relevantes</option><option value="all">Todas</option><option value="TeamNew">Equipos nuevos</option><option value="TeamUpdatable">Equipos actualizables</option><option value="PlayerNew">Jugadores nuevos</option><option value="PlayerUpdatable">Jugadores actualizables</option><option value="StructuralError">Errores</option></select></div></div>
        {details.length === 0 ? <p className="admin-help">No hay filas para este filtro.</p> : <div className="table-wrap"><table className="admin-table"><thead><tr><th>Origen</th><th>Entidad</th><th>Clasificación</th><th>Detalle</th></tr></thead><tbody>{details.map(row => <tr key={`${row.sheet}-${row.rowNumber}`}><td><strong>{row.sheet}</strong><small>Fila {row.rowNumber}</small></td><td>{row.entity}</td><td><span className={`import-status import-status--${row.classification}`}>{classificationLabels[row.classification]}</span></td><td>{row.message}<Changes changes={row.proposedChanges} /></td></tr>)}</tbody></table></div>}
      </section>
      {!preview.canConfirm && <StatusMessage kind="error" message="Corregí los errores del archivo y volvé a analizarlo." />}
      <div className="form-actions"><button type="button" className="btn btn-primary" disabled={busy || !preview.canConfirm} onClick={confirm}>{busy ? 'Confirmando...' : 'Confirmar importación'}</button><Link className="btn btn-secondary" to="/admin/teams">Cancelar</Link></div>
    </>}
  </div>
}
