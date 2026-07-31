import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { MatchWithPrediction, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

interface RowState {
  homeInput: string
  awayInput: string
  saving: boolean
  error: string | null
  savedMessage: string | null
}

function sanitizeDigits(value: string): string {
  return value.replace(/\D/g, '')
}

function buildInitialRow(match: MatchWithPrediction): RowState {
  return {
    homeInput: match.myPrediction ? String(match.myPrediction.predictedHomeScore) : '',
    awayInput: match.myPrediction ? String(match.myPrediction.predictedAwayScore) : '',
    saving: false,
    error: null,
    savedMessage: null,
  }
}

export default function PredictionsMatchesPage() {
  const { roundId } = useParams()

  const [round, setRound] = useState<Round | null>(null)
  const [matches, setMatches] = useState<MatchWithPrediction[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [rows, setRows] = useState<Record<number, RowState>>({})

  useEffect(() => {
    let cancelled = false

    Promise.all([
      api.get<Round>(`/rounds/${roundId}`),
      api.get<MatchWithPrediction[]>(`/predictions/rounds/${roundId}`),
    ])
      .then(([r, ms]) => {
        if (cancelled) return
        setRound(r)
        setMatches(ms)
        const initialRows: Record<number, RowState> = {}
        ms.forEach((m) => {
          initialRows[m.id] = buildInitialRow(m)
        })
        setRows(initialRows)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar los partidos.')
      })

    return () => {
      cancelled = true
    }
  }, [roundId])

  function updateRow(matchId: number, patch: Partial<RowState>) {
    setRows((prev) => ({ ...prev, [matchId]: { ...prev[matchId], ...patch } }))
  }

  async function savePrediction(match: MatchWithPrediction) {
    const row = rows[match.id]
    if (!row) return

    const homeScore = Number(row.homeInput)
    const awayScore = Number(row.awayInput)

    if (
      row.homeInput.trim() === '' ||
      row.awayInput.trim() === '' ||
      !Number.isInteger(homeScore) ||
      !Number.isInteger(awayScore) ||
      homeScore < 0 ||
      awayScore < 0
    ) {
      updateRow(match.id, { error: 'Ingresá un resultado válido (0 o mayor) para ambos equipos.', savedMessage: null })
      return
    }

    updateRow(match.id, { saving: true, error: null, savedMessage: null })

    try {
      const updated = match.myPrediction
        ? await api.put<MatchWithPrediction['myPrediction']>(`/predictions/${match.myPrediction.id}`, {
            predictedHomeScore: homeScore,
            predictedAwayScore: awayScore,
          })
        : await api.post<MatchWithPrediction['myPrediction']>('/predictions', {
            matchId: match.id,
            predictedHomeScore: homeScore,
            predictedAwayScore: awayScore,
          })

      setMatches((prev) => (prev ? prev.map((m) => (m.id === match.id ? { ...m, myPrediction: updated } : m)) : prev))
      updateRow(match.id, { saving: false, savedMessage: 'Pronóstico guardado correctamente.' })
      setTimeout(() => updateRow(match.id, { savedMessage: null }), 4000)
    } catch (err) {
      updateRow(match.id, {
        saving: false,
        error: err instanceof ApiError ? err.message : 'Ocurrió un error inesperado al guardar el pronóstico.',
      })
    }
  }

  return (
    <div>
      <div className="breadcrumb">
        {round && <Link to={`/predictions/editions/${round.editionId}/rounds`}>← Fechas</Link>}
      </div>
      <div className="admin-header">
        <h1>Pronósticos — Partidos {round ? `— ${round.name}` : ''}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
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
                <th>Fecha</th>
                <th>Hora</th>
                <th>Resultado oficial</th>
                <th>Mi pronóstico</th>
              </tr>
            </thead>
            <tbody>
              {matches.map((m) => {
                const row = rows[m.id]
                const startsAt = new Date(m.startsAtUtc)
                const editable = m.canPredict

                return (
                  <tr key={m.id}>
                    <td>
                      {m.participantHome} vs {m.participantAway}
                    </td>
                    <td>{startsAt.toLocaleDateString()}</td>
                    <td>{startsAt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</td>
                    <td>{m.status === 'Finished' ? `${m.homeGoals} - ${m.awayGoals}` : '—'}</td>
                    <td>
                      {editable && row && (
                        <div className="prediction-row">
                          <div className="prediction-row__inputs">
                            <input
                              type="text"
                              inputMode="numeric"
                              pattern="[0-9]*"
                              placeholder="-"
                              aria-label="Resultado local"
                              value={row.homeInput}
                              onChange={(e) =>
                                updateRow(m.id, { homeInput: sanitizeDigits(e.target.value), savedMessage: null })
                              }
                              className="prediction-row__input"
                            />
                            <span className="prediction-row__separator">-</span>
                            <input
                              type="text"
                              inputMode="numeric"
                              pattern="[0-9]*"
                              placeholder="-"
                              aria-label="Resultado visitante"
                              value={row.awayInput}
                              onChange={(e) =>
                                updateRow(m.id, { awayInput: sanitizeDigits(e.target.value), savedMessage: null })
                              }
                              className="prediction-row__input"
                            />
                            <button
                              type="button"
                              className="btn btn-primary"
                              disabled={row.saving}
                              onClick={() => savePrediction(m)}
                            >
                              {row.saving
                                ? 'Guardando...'
                                : m.myPrediction
                                  ? 'Actualizar pronóstico'
                                  : 'Guardar pronóstico'}
                            </button>
                          </div>
                          <div className="prediction-row__message">
                            {row.error && <span className="form-field-error">{row.error}</span>}
                            {row.savedMessage && <span className="prediction-row__saved">{row.savedMessage}</span>}
                          </div>
                        </div>
                      )}

                      {!editable && m.status === 'Cancelled' && <span>—</span>}

                      {!editable && m.status === 'Finished' && (
                        m.myPrediction ? (
                          <div className="prediction-result">
                            <div>
                              Mi pronóstico: {m.myPrediction.predictedHomeScore} - {m.myPrediction.predictedAwayScore}
                            </div>
                            <div>
                              Resultado oficial: {m.myPrediction.officialHomeScore} - {m.myPrediction.officialAwayScore}
                            </div>
                            <div>Puntos obtenidos: {m.myPrediction.points}</div>
                            <div>Motivo: {m.myPrediction.evaluationLabel}</div>
                          </div>
                        ) : (
                          <span>Sin pronóstico</span>
                        )
                      )}

                      {!editable && m.status !== 'Cancelled' && m.status !== 'Finished' && (
                        <span>
                          Pronóstico cerrado
                          {m.myPrediction && ` (${m.myPrediction.predictedHomeScore} - ${m.myPrediction.predictedAwayScore})`}
                        </span>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
