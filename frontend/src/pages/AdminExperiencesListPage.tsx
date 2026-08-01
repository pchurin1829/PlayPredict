import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Experience } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function AdminExperiencesListPage() {
  const [experiences, setExperiences] = useState<Experience[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [updatingId, setUpdatingId] = useState<number | null>(null)

  function loadExperiences() {
    setError(null)
    api
      .get<Experience[]>('/admin/experiences')
      .then(setExperiences)
      .catch((err) => setError(err.message ?? 'No se pudieron cargar las experiencias.'))
  }

  useEffect(() => {
    loadExperiences()
  }, [])

  async function runAction(experience: Experience, action: 'publish' | 'archive') {
    setUpdatingId(experience.id)
    setError(null)
    try {
      const updated = await api.put<Experience>(`/admin/experiences/${experience.id}/${action}`, {})
      setExperiences((prev) => (prev ? prev.map((e) => (e.id === updated.id ? updated : e)) : prev))
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError('Ocurrió un error inesperado al actualizar la experiencia.')
      }
    } finally {
      setUpdatingId(null)
    }
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/competitions">← Panel administrativo</Link>
      </div>
      <div className="admin-header">
        <h1>Experiencias</h1>
        <Link to="/admin/experiences/new" className="btn btn-primary">
          + Nueva Experiencia
        </Link>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!experiences && !error && <StatusMessage kind="loading" message="Cargando experiencias..." />}

      {experiences && experiences.length === 0 && (
        <div className="empty-state">No hay experiencias creadas todavía.</div>
      )}

      {experiences && experiences.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Estado</th>
                <th>Pública</th>
                <th>Puntuación por defecto</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {experiences.map((e) => (
                <tr key={e.id}>
                  <td>{e.name}</td>
                  <td>
                    <span className={`badge badge--${e.status}`}>{e.statusLabel}</span>
                  </td>
                  <td>{e.isPublic ? 'Sí' : 'No'}</td>
                  <td>
                    {e.defaultExactScorePoints} / {e.defaultCorrectOutcomePoints} / {e.defaultIncorrectPoints}
                  </td>
                  <td>
                    <div className="match-row-actions">
                      <Link to={`/admin/experiences/${e.id}/edit`} className="btn btn-secondary">
                        Editar
                      </Link>
                      {e.status === 'Draft' && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          disabled={updatingId === e.id}
                          onClick={() => runAction(e, 'publish')}
                        >
                          Publicar
                        </button>
                      )}
                      {e.status !== 'Archived' && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          disabled={updatingId === e.id}
                          onClick={() => runAction(e, 'archive')}
                        >
                          Archivar
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
