import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition, CompetitionDependencies } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import ConfirmModal from '../components/ConfirmModal'

export default function CompetitionsListPage() {
  const [competitions, setCompetitions] = useState<Competition[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()
  const [deleteTarget, setDeleteTarget] = useState<Competition | null>(null)
  const [dependencies, setDependencies] = useState<CompetitionDependencies | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)
    api
      .get<Competition[]>('/competitions')
      .then((data) => {
        if (!cancelled) setCompetitions(data)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las competencias.')
      })
    return () => {
      cancelled = true
    }
  }, [])

  async function inspectDelete(competition: Competition) {
    setError(null); setDependencies(null); setDeleteTarget(competition)
    try { setDependencies(await api.get<CompetitionDependencies>(`/competitions/${competition.id}/dependencies`)) }
    catch (reason) { setDeleteTarget(null); setError(reason instanceof Error ? reason.message : 'No se pudieron consultar las dependencias.') }
  }

  async function confirmDelete() {
    if (!deleteTarget || !dependencies) return
    if (!dependencies.canDelete) { setDeleteTarget(null); setError('No se puede eliminar esta competencia de referencia porque tiene datos relacionados. Revisá el diagnóstico antes de limpiarla.'); return }
    try {
      await api.del(`/competitions/${deleteTarget.id}`)
      setCompetitions(current => current?.filter(item => item.id !== deleteTarget.id) ?? current)
      setMessage('Competencia de referencia eliminada correctamente.')
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo eliminar la competencia de referencia.') }
    finally { setDeleteTarget(null); setDependencies(null) }
  }

  const dependencyMessage = dependencies
    ? `Ediciones: ${dependencies.editions}\nFechas: ${dependencies.rounds}\nPartidos: ${dependencies.matches}\nCompetencias/Ligas relacionadas: ${dependencies.leagues}\nParticipantes: ${dependencies.participants}\nPronósticos: ${dependencies.predictions}\nEvaluaciones: ${dependencies.evaluations}\nGoleadores: ${dependencies.matchScorers}\nPremios: ${dependencies.prizes}\nConfiguraciones de scoring: ${dependencies.scoringConfigurations}\n\n${dependencies.canDelete ? 'No tiene dependencias críticas y puede eliminarse.' : 'La eliminación está bloqueada para evitar una cascada o pérdida inconsistente.'}`
    : 'Consultando dependencias...'

  return (
    <div>
      <div className="admin-header">
        <div>
          <h1>Competencias de referencia</h1>
          <p className="admin-help">Competencias deportivas reales utilizadas como fuente de fixtures y resultados.</p>
        </div>
        <Link to="/competitions/new" className="btn btn-primary">
          + Nueva Competencia de referencia
        </Link>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {message && <StatusMessage kind="success" message={message} />}
      {!competitions && !error && <StatusMessage kind="loading" message="Cargando competencias..." />}

      {competitions && competitions.length === 0 && (
        <div className="empty-state">No hay competencias de referencia creadas todavía.</div>
      )}

      {competitions && competitions.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Deporte</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {competitions.map((c) => (
                <tr
                  key={c.id}
                  className="is-clickable"
                  onClick={() => navigate(`/competitions/${c.id}/editions`)}
                >
                  <td>{c.name}</td>
                  <td>{c.sport}</td>
                  <td>
                    <span className={`badge badge--${c.isActive ? 'active' : 'inactive'}`}>
                      {c.isActive ? 'Activa' : 'Inactiva'}
                    </span>
                  </td>
                  <td>
                    <div className="match-row-actions">
                      <Link to={`/competitions/${c.id}/edit`} className="btn btn-secondary" onClick={(e) => e.stopPropagation()}>Editar</Link>
                      <Link to={`/competitions/${c.id}/editions`} className="btn btn-primary" onClick={(e) => e.stopPropagation()}>Ver Ediciones</Link>
                      <button type="button" className="btn btn-danger" onClick={(e) => { e.stopPropagation(); void inspectDelete(c) }}>Eliminar</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <ConfirmModal open={Boolean(deleteTarget)} title={`Eliminar ${deleteTarget?.name ?? 'competencia de referencia'}`} message={dependencyMessage} confirmLabel={!dependencies ? 'Consultando...' : dependencies.canDelete ? 'Eliminar' : 'Cerrar'} onConfirm={confirmDelete} onCancel={() => { setDeleteTarget(null); setDependencies(null) }} />
    </div>
  )
}
