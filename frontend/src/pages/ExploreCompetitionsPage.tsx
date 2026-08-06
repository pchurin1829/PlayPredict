import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition, Edition, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

interface ExploreItem {
  competition: Competition
  activeEdition: Edition | null
  roundsCount: number
}

export default function ExploreCompetitionsPage() {
  const [items, setItems] = useState<ExploreItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    api
      .get<Competition[]>('/competitions')
      .then(async (competitions) => {
        const active = competitions.filter((c) => c.isActive)

        const enriched = await Promise.all(
          active.map(async (competition) => {
            const editions = await api.get<Edition[]>(`/competitions/${competition.id}/editions`)
            const activeEdition = editions.find((e) => e.status === 'Active') ?? null

            let roundsCount = 0
            if (activeEdition) {
              const rounds = await api.get<Round[]>(`/editions/${activeEdition.id}/rounds`)
              roundsCount = rounds.length
            }

            return { competition, activeEdition, roundsCount }
          }),
        )

        if (!cancelled) setItems(enriched)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las Competencias.')
      })

    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/leagues">← Mis Ligas</Link>
      </div>
      <div className="admin-header">
        <h1>Explorar Competencias</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!items && !error && <StatusMessage kind="loading" message="Cargando Competencias..." />}

      {items && items.length === 0 && (
        <div className="empty-state">No hay Competencias activas todavía.</div>
      )}

      {items && items.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Deporte</th>
                <th>Edición activa</th>
                <th>Fechas</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map(({ competition, activeEdition, roundsCount }) => (
                <tr key={competition.id}>
                  <td>{competition.name}</td>
                  <td>{competition.sport}</td>
                  <td>{activeEdition ? activeEdition.name : 'Sin edición activa'}</td>
                  <td>{roundsCount}</td>
                  <td>{competition.isActive ? 'Activa' : 'Inactiva'}</td>
                  <td>
                    <Link to={`/competitions/${competition.id}`} className="btn btn-secondary">
                      Ver
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
