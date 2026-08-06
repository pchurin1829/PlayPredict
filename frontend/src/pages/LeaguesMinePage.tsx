import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { LEAGUE_SCOPE_LABELS, type LeagueSummary } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function LeaguesMinePage() {
  const [leagues, setLeagues] = useState<LeagueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)
    api
      .get<LeagueSummary[]>('/leagues/mine')
      .then((data) => {
        if (!cancelled) setLeagues(data)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar tus Ligas.')
      })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div>
      <div className="admin-header">
        <h1>Mis Ligas</h1>
        <div className="match-row-actions">
          <Link to="/competitions/explore" className="btn btn-primary">
            Explorar Competencias
          </Link>
          <Link to="/leagues/join" className="btn btn-secondary">
            Unirse por código
          </Link>
        </div>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!leagues && !error && <StatusMessage kind="loading" message="Cargando tus Ligas..." />}

      {leagues && leagues.length === 0 && (
        <div className="empty-state">
          No participás en ninguna Liga todavía.
          <div className="match-row-actions" style={{ marginTop: '0.75rem' }}>
            <Link to="/competitions/explore" className="btn btn-primary">
              Explorar Competencias
            </Link>
            <Link to="/leagues/join" className="btn btn-secondary">
              Unirse mediante código
            </Link>
          </div>
        </div>
      )}

      {leagues && leagues.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Liga</th>
                <th>Competencia</th>
                <th>Alcance</th>
                <th>Participantes</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {leagues.map((l) => (
                <tr key={l.id}>
                  <td>
                    {l.name}
                    {l.isCreator && <span> (creador)</span>}
                  </td>
                  <td>{l.competitionName}</td>
                  <td>
                    {LEAGUE_SCOPE_LABELS[l.scopeType]}
                    {l.scopeType === 'RoundRange' && l.roundFromName && l.roundToName && (
                      <span>
                        {' '}
                        ({l.roundFromName} → {l.roundToName})
                      </span>
                    )}
                  </td>
                  <td>{l.participantsCount}</td>
                  <td>{l.isActive ? 'Activa' : 'Inactiva'}</td>
                  <td>
                    <Link to={`/leagues/${l.id}`} className="btn btn-secondary">
                      Abrir
                    </Link>
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
