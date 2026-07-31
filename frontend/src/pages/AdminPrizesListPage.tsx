import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Prize } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function AdminPrizesListPage() {
  const [prizes, setPrizes] = useState<Prize[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [updatingId, setUpdatingId] = useState<number | null>(null)

  function loadPrizes() {
    setError(null)
    api
      .get<Prize[]>('/admin/prizes')
      .then(setPrizes)
      .catch((err) => setError(err.message ?? 'No se pudieron cargar los premios.'))
  }

  useEffect(() => {
    loadPrizes()
  }, [])

  async function runAction(prize: Prize, action: 'publish' | 'close' | 'cancel') {
    setUpdatingId(prize.id)
    setError(null)
    try {
      const updated = await api.put<Prize>(`/admin/prizes/${prize.id}/${action}`, {})
      setPrizes((prev) => (prev ? prev.map((p) => (p.id === updated.id ? updated : p)) : prev))
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError('Ocurrió un error inesperado al actualizar el premio.')
      }
    } finally {
      setUpdatingId(null)
    }
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to="/competitions">← Panel administrativo</Link>
      </div>
      <div className="admin-header">
        <h1>Premios</h1>
        <Link to="/admin/prizes/new" className="btn btn-primary">
          + Nuevo Premio
        </Link>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!prizes && !error && <StatusMessage kind="loading" message="Cargando premios..." />}

      {prizes && prizes.length === 0 && (
        <div className="empty-state">No hay premios creados todavía.</div>
      )}

      {prizes && prizes.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Edición</th>
                <th>Ámbito</th>
                <th>Criterio</th>
                <th>Estado</th>
                <th>Sponsor</th>
                <th>Ganador actual</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {prizes.map((p) => (
                <tr key={p.id}>
                  <td>{p.name}</td>
                  <td>
                    {p.editionName}
                    {p.roundName ? ` — ${p.roundName}` : ''}
                  </td>
                  <td>{p.scopeLabel}</td>
                  <td>{p.criteriaLabel}</td>
                  <td>
                    <span className={`badge badge--${p.status}`}>{p.statusLabel}</span>
                  </td>
                  <td>{p.sponsorName ?? '—'}</td>
                  <td>
                    {p.currentWinners.length === 0
                      ? '—'
                      : p.currentWinners.map((w) => `${w.firstName} ${w.lastName}`).join(', ')}
                  </td>
                  <td>
                    <div className="match-row-actions">
                      <Link to={`/admin/prizes/${p.id}/edit`} className="btn btn-secondary">
                        Editar
                      </Link>
                      {p.status === 'Draft' && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          disabled={updatingId === p.id}
                          onClick={() => runAction(p, 'publish')}
                        >
                          Publicar
                        </button>
                      )}
                      {p.status === 'Published' && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          disabled={updatingId === p.id}
                          onClick={() => runAction(p, 'close')}
                        >
                          Cerrar
                        </button>
                      )}
                      {(p.status === 'Draft' || p.status === 'Published') && (
                        <button
                          type="button"
                          className="btn btn-secondary"
                          disabled={updatingId === p.id}
                          onClick={() => runAction(p, 'cancel')}
                        >
                          Cancelar
                        </button>
                      )}
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
