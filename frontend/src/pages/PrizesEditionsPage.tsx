import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { EDITION_STATUS_LABELS, type Competition, type Edition } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function PrizesEditionsPage() {
  const { competitionId } = useParams()
  const navigate = useNavigate()

  const [competition, setCompetition] = useState<Competition | null>(null)
  const [editions, setEditions] = useState<Edition[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    Promise.all([
      api.get<Competition>(`/competitions/${competitionId}`),
      api.get<Edition[]>(`/competitions/${competitionId}/editions`),
    ])
      .then(([c, eds]) => {
        if (cancelled) return
        setCompetition(c)
        setEditions(eds)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las ediciones.')
      })

    return () => {
      cancelled = true
    }
  }, [competitionId])

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/prizes">← Competencias</Link>
      </div>
      <div className="admin-header">
        <h1>Premios — Ediciones {competition ? `— ${competition.name}` : ''}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!editions && !error && <StatusMessage kind="loading" message="Cargando ediciones..." />}

      {editions && editions.length === 0 && (
        <div className="empty-state">Esta competencia todavía no tiene ediciones.</div>
      )}

      {editions && editions.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Inicio</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {editions.map((ed) => (
                <tr
                  key={ed.id}
                  className="is-clickable"
                  onClick={() => navigate(`/prizes/editions/${ed.id}`)}
                >
                  <td>{ed.name}</td>
                  <td>{new Date(ed.startDateUtc).toLocaleDateString()}</td>
                  <td>
                    <span className={`badge badge--${ed.status}`}>{EDITION_STATUS_LABELS[ed.status]}</span>
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
