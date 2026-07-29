import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import type { Edition, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function RoundsListPage() {
  const { editionId } = useParams()
  const navigate = useNavigate()

  const [edition, setEdition] = useState<Edition | null>(null)
  const [rounds, setRounds] = useState<Round[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    Promise.all([
      api.get<Edition>(`/editions/${editionId}`),
      api.get<Round[]>(`/editions/${editionId}/rounds`),
    ])
      .then(([ed, rs]) => {
        if (cancelled) return
        setEdition(ed)
        setRounds(rs)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las fechas.')
      })

    return () => {
      cancelled = true
    }
  }, [editionId])

  return (
    <div>
      <div className="breadcrumb">
        {edition && (
          <Link to={`/competitions/${edition.competitionId}/editions`}>← Ediciones</Link>
        )}
      </div>
      <div className="admin-header">
        <h1>Fechas {edition ? `— ${edition.name}` : ''}</h1>
        <Link to={`/editions/${editionId}/rounds/new`} className="btn btn-primary">
          + Nueva Fecha
        </Link>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!rounds && !error && <StatusMessage kind="loading" message="Cargando fechas..." />}

      {rounds && rounds.length === 0 && (
        <div className="empty-state">Esta edición todavía no tiene fechas.</div>
      )}

      {rounds && rounds.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Orden</th>
                <th>Nombre</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {rounds.map((r) => (
                <tr
                  key={r.id}
                  className="is-clickable"
                  onClick={() => navigate(`/rounds/${r.id}/matches`)}
                >
                  <td>{r.order}</td>
                  <td>{r.name}</td>
                  <td>
                    <Link
                      to={`/rounds/${r.id}/edit`}
                      className="btn btn-secondary"
                      onClick={(e) => e.stopPropagation()}
                    >
                      Editar
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
