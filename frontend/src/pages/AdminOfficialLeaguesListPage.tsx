import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { AdminOfficialLeague, OfficialLeagueDependencies } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { useCompanySettings } from '../company/CompanySettingsContext'
import ConfirmModal from '../components/ConfirmModal'

export default function AdminOfficialLeaguesListPage() {
  const { company } = useCompanySettings()
  const companyName = company.shortName || 'PlayPredict'
  const [leagues, setLeagues] = useState<AdminOfficialLeague[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<AdminOfficialLeague | null>(null)
  const [dependencies, setDependencies] = useState<OfficialLeagueDependencies | null>(null)

  useEffect(() => {
    api.get<AdminOfficialLeague[]>('/admin/official-leagues')
      .then(setLeagues)
      .catch((reason) => setError(reason.message ?? `No se pudieron cargar las competencias ${companyName}.`))
  }, [])

  async function inspectDelete(league: AdminOfficialLeague) {
    setError(null); setDependencies(null); setDeleteTarget(league)
    try { setDependencies(await api.get<OfficialLeagueDependencies>(`/admin/official-leagues/${league.id}/dependencies`)) }
    catch (reason) { setDeleteTarget(null); setError(reason instanceof Error ? reason.message : 'No se pudieron consultar las dependencias.') }
  }

  async function confirmDelete() {
    if (!deleteTarget || !dependencies) return
    if (!dependencies.canDelete) { setDeleteTarget(null); setError(`No se puede eliminar esta competencia ${companyName}: tiene participantes, pronósticos o evaluaciones relacionadas.`); return }
    try {
      await api.del(`/admin/official-leagues/${deleteTarget.id}`)
      setLeagues(current => current?.filter(item => item.id !== deleteTarget.id) ?? current)
      setMessage(`Competencia ${companyName} eliminada correctamente.`)
    } catch (reason) { setError(reason instanceof Error ? reason.message : `No se pudo eliminar la competencia ${companyName}.`) }
    finally { setDeleteTarget(null); setDependencies(null) }
  }

  const dependencyMessage = dependencies
    ? `Participantes: ${dependencies.participants}\nPronósticos: ${dependencies.predictions}\nEvaluaciones/ranking derivado: ${dependencies.evaluations}\nResultados oficiales del fixture compartido: ${dependencies.officialResults}\n\n${dependencies.canDelete ? 'Puede eliminarse. El fixture y sus resultados no se modificarán.' : 'La eliminación está bloqueada para no borrar participantes, pronósticos ni ranking en cascada.'}`
    : 'Consultando dependencias...'

  return (
    <div>
      <div className="admin-header">
        <div>
          <h1>Competencias {companyName}</h1>
          <p className="admin-help">Competencias propias de {companyName} basadas en competencias de referencia.</p>
        </div>
        <Link to="/admin/official-leagues/new" className="btn btn-primary">+ Nueva Competencia {companyName}</Link>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {message && <StatusMessage kind="success" message={message} />}
      {!leagues && !error && <StatusMessage kind="loading" message={`Cargando competencias ${companyName}...`} />}
      {leagues?.length === 0 && <div className="empty-state">No hay competencias {companyName} creadas.</div>}

      {leagues && leagues.length > 0 && (
        <div className="official-league-admin-grid">
          {leagues.map((league) => (
            <article className="official-league-admin-card" key={league.id}>
              <div className="official-league-admin-card__header">
                <h2>{league.name}</h2>
                <span className={`badge badge--${league.isActive ? 'active' : 'inactive'}`}>{league.isActive ? 'Activa' : 'Suspendida'}</span>
              </div>
              <dl className="official-league-admin-card__details">
                <div><dt>Fuente</dt><dd>{league.competitionName} · {league.editionName}</dd></div>
                <div><dt>Alcance</dt><dd>{league.scopeType === 'FullCompetition' ? 'Toda la edición' : `${league.roundFromName} → ${league.roundToName}`}</dd></div>
                <div className="official-league-admin-card__fixture"><dt>Fixture utilizado</dt><dd>{league.roundsCount} {league.roundsCount === 1 ? 'fecha' : 'fechas'} · {league.matchesCount} {league.matchesCount === 1 ? 'partido' : 'partidos'}</dd></div>
                <div><dt>Participantes</dt><dd>{league.participantsCount}</dd></div>
              </dl>
              <div className="official-league-admin-card__actions"><Link className="btn btn-secondary" to={`/admin/official-leagues/${league.id}/edit`}>Editar</Link><button type="button" className="btn btn-danger" onClick={() => void inspectDelete(league)}>Eliminar</button></div>
            </article>
          ))}
        </div>
      )}
      <ConfirmModal open={Boolean(deleteTarget)} title={`Eliminar ${deleteTarget?.name ?? `competencia ${companyName}`}`} message={dependencyMessage} confirmLabel={!dependencies ? 'Consultando...' : dependencies.canDelete ? 'Eliminar' : 'Cerrar'} onConfirm={confirmDelete} onCancel={() => { setDeleteTarget(null); setDependencies(null) }} />
    </div>
  )
}
