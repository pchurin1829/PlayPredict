import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function PredictionsCompetitionsPage() {
  const [competitions, setCompetitions] = useState<Competition[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const navigate = useNavigate()

  useEffect(() => {
    let cancelled = false
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
      <div className="breadcrumb">
        <Link to="/competitions">← Panel administrativo</Link>
      </div>
      <div className="admin-header">
        <h1>Pronósticos — Competencias</h1>
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
              </tr>
            </thead>
            <tbody>
              {competitions.map((c) => (
                <tr
                  key={c.id}
                  className="is-clickable"
                  onClick={() => navigate(`/predictions/competitions/${c.id}/editions`)}
                >
                  <td>{c.name}</td>
                  <td>{c.sport}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
