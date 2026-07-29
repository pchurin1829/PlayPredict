import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Match, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import MatchResultModal from '../components/MatchResultModal'

export default function MatchesListPage() {
  const { roundId } = useParams()

  const [round, setRound] = useState<Round | null>(null)
  const [matches, setMatches] = useState<Match[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [resultTarget, setResultTarget] = useState<Match | null>(null)
  const [savedMessage, setSavedMessage] = useState<string | null>(null)

  function loadMatches() {
    setError(null)
    return api.get<Match[]>(`/rounds/${roundId}/matches`)
  }

  useEffect(() => {
    let cancelled = false

    Promise.all([api.get<Round>(`/rounds/${roundId}`), loadMatches()])
      .then(([r, ms]) => {
        if (cancelled) return
        setRound(r)
        setMatches(ms)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar los partidos.')
      })

    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [roundId])

  function handleResultSaved(updated: Match) {
    setMatches((prev) => (prev ? prev.map((m) => (m.id === updated.id ? updated : m)) : prev))
    setResultTarget(null)
    setSavedMessage('Resultado oficial guardado correctamente.')
    setTimeout(() => setSavedMessage(null), 4000)
  }

  return (
    <div>
      <div className="breadcrumb">
        {round && <Link to={`/editions/${round.editionId}/rounds`}>← Fechas</Link>}
      </div>
      <div className="admin-header">
        <h1>Partidos {round ? `— ${round.name}` : ''}</h1>
        <Link to={`/rounds/${roundId}/matches/new`} className="btn btn-primary">
          + Nuevo Partido
        </Link>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {savedMessage && <StatusMessage kind="success" message={savedMessage} />}
      {!matches && !error && <StatusMessage kind="loading" message="Cargando partidos..." />}

      {matches && matches.length === 0 && (
        <div className="empty-state">Esta fecha todavía no tiene partidos.</div>
      )}

      {matches && matches.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Partido</th>
                <th>Inicio</th>
                <th>Estado</th>
                <th>Resultado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {matches.map((m) => (
                <tr key={m.id}>
                  <td>
                    {m.participantHome} vs {m.participantAway}
                  </td>
                  <td>{new Date(m.startsAtUtc).toLocaleString()}</td>
                  <td>
                    <span className={`badge badge--${m.status}`}>{m.status}</span>
                  </td>
                  <td>
                    {m.status === 'Finished' ? `${m.homeGoals} - ${m.awayGoals}` : '—'}
                  </td>
                  <td>
                    <div className="match-row-actions">
                      <Link to={`/matches/${m.id}/edit`} className="btn btn-secondary">
                        Editar
                      </Link>
                      <button className="btn btn-primary" onClick={() => setResultTarget(m)}>
                        Cargar resultado
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {resultTarget && (
        <MatchResultModal
          match={resultTarget}
          onClose={() => setResultTarget(null)}
          onSaved={handleResultSaved}
        />
      )}
    </div>
  )
}
