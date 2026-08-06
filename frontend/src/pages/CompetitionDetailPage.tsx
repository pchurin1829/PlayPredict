import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition, Edition, LeagueSummary, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function CompetitionDetailPage() {
  const { competitionId } = useParams()

  const [competition, setCompetition] = useState<Competition | null>(null)
  const [activeEdition, setActiveEdition] = useState<Edition | null>(null)
  const [roundsCount, setRoundsCount] = useState(0)
  const [myLeagues, setMyLeagues] = useState<LeagueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    Promise.all([
      api.get<Competition>(`/competitions/${competitionId}`),
      api.get<Edition[]>(`/competitions/${competitionId}/editions`),
      api.get<LeagueSummary[]>('/leagues/mine'),
    ])
      .then(async ([c, editions, leagues]) => {
        if (cancelled) return
        setCompetition(c)
        setMyLeagues(leagues.filter((l) => l.competitionId === Number(competitionId)))

        const active = editions.find((e) => e.status === 'Active') ?? null
        setActiveEdition(active)

        if (active) {
          const rounds = await api.get<Round[]>(`/editions/${active.id}/rounds`)
          if (!cancelled) setRoundsCount(rounds.length)
        }
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudo cargar la Competencia.')
      })

    return () => {
      cancelled = true
    }
  }, [competitionId])

  if (error) {
    return (
      <div>
        <div className="breadcrumb">
          <Link to="/competitions/explore">← Explorar Competencias</Link>
        </div>
        <StatusMessage kind="error" message={error} />
      </div>
    )
  }

  if (!competition) {
    return <StatusMessage kind="loading" message="Cargando Competencia..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/competitions/explore">← Explorar Competencias</Link>
      </div>
      <div className="admin-header">
        <h1>{competition.name}</h1>
        <Link to={`/leagues/new?competitionId=${competition.id}`} className="btn btn-primary">
          + Crear nueva Liga
        </Link>
      </div>

      <div className="form-card">
        {competition.description && (
          <div className="form-field">
            <label>Descripción</label>
            <span>{competition.description}</span>
          </div>
        )}

        <div className="form-field">
          <label>Deporte</label>
          <span>{competition.sport}</span>
        </div>

        <div className="form-field">
          <label>Edición activa</label>
          <span>{activeEdition ? activeEdition.name : 'Sin edición activa'}</span>
        </div>

        <div className="form-field">
          <label>Fechas</label>
          <span>{roundsCount}</span>
        </div>
      </div>

      <h2>Mis Ligas en esta Competencia</h2>

      {!myLeagues && <StatusMessage kind="loading" message="Cargando tus Ligas..." />}

      {myLeagues && myLeagues.length === 0 && (
        <div className="empty-state">Todavía no participás en ninguna Liga de esta Competencia.</div>
      )}

      {myLeagues && myLeagues.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Liga</th>
                <th>Participantes</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {myLeagues.map((l) => (
                <tr key={l.id}>
                  <td>
                    {l.name}
                    {l.isCreator && <span> (creador)</span>}
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
