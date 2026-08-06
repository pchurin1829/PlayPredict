import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { LEAGUE_SCOPE_LABELS, type LeagueDetail, type LeagueParticipantInfo } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function LeagueDetailPage() {
  const { leagueId } = useParams()

  const [league, setLeague] = useState<LeagueDetail | null>(null)
  const [participants, setParticipants] = useState<LeagueParticipantInfo[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    Promise.all([
      api.get<LeagueDetail>(`/leagues/${leagueId}`),
      api.get<LeagueParticipantInfo[]>(`/leagues/${leagueId}/participants`),
    ])
      .then(([l, p]) => {
        if (cancelled) return
        setLeague(l)
        setParticipants(p)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudo cargar la Liga.')
      })

    return () => {
      cancelled = true
    }
  }, [leagueId])

  if (error) {
    return (
      <div>
        <div className="breadcrumb">
          <Link to="/leagues">← Mis Ligas</Link>
        </div>
        <StatusMessage kind="error" message={error} />
      </div>
    )
  }

  if (!league) {
    return <StatusMessage kind="loading" message="Cargando Liga..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/leagues">← Mis Ligas</Link>
      </div>
      <div className="admin-header">
        <h1>{league.name}</h1>
        <Link to={`/leagues/${league.id}/matches`} className="btn btn-primary">
          Ver partidos / Pronosticar
        </Link>
      </div>

      <div className="form-card">
        {league.description && (
          <div className="form-field">
            <label>Descripción</label>
            <span>{league.description}</span>
          </div>
        )}

        <div className="form-field">
          <label>Competencia</label>
          <span>{league.competitionName}</span>
        </div>

        <div className="form-field">
          <label>Alcance</label>
          <span>
            {LEAGUE_SCOPE_LABELS[league.scopeType]}
            {league.scopeType === 'RoundRange' && league.roundFromName && league.roundToName && (
              <span>
                {' '}
                ({league.roundFromName} → {league.roundToName})
              </span>
            )}
          </span>
        </div>

        <div className="form-field">
          <label>Creador</label>
          <span>
            {league.createdByName}
            {league.isCreator && ' (vos)'}
          </span>
        </div>

        <div className="form-field">
          <label>Estado</label>
          <span>{league.isActive ? 'Activa' : 'Inactiva'}</span>
        </div>

        <div className="form-field">
          <label>Participantes</label>
          <span>{league.participantsCount}</span>
        </div>

        {league.isCreator && league.inviteCode && (
          <div className="form-field">
            <label>Código de invitación</label>
            <span>
              <strong>{league.inviteCode}</strong> — compartilo con quien quieras invitar.
            </span>
          </div>
        )}
      </div>

      <h2>Participantes</h2>
      {!participants && <StatusMessage kind="loading" message="Cargando participantes..." />}
      {participants && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Incorporado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {participants.map((p) => (
                <tr key={p.userId}>
                  <td>
                    {p.firstName} {p.lastName}
                  </td>
                  <td>{new Date(p.joinedAtUtc).toLocaleDateString()}</td>
                  <td>{p.isCreator && <span className="badge">Creador</span>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
