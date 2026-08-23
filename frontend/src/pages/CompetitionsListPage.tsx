import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function CompetitionsListPage() {
  const [competitions, setCompetitions] = useState<Competition[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()

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

  return (
    <div>
      <div className="admin-header">
        <h1>Competencias</h1>
        <Link to="/competitions/new" className="btn btn-primary">
          + Nueva Competencia
        </Link>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!competitions && !error && <StatusMessage kind="loading" message="Cargando competencias..." />}

      {competitions && competitions.length === 0 && (
        <div className="empty-state">No hay competencias creadas todavía.</div>
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
