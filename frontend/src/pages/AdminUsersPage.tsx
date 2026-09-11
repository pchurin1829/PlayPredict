import { useEffect, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { User } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import ConfirmModal from '../components/ConfirmModal'

interface ResetResult {
  userId: number
  email: string
  temporaryPassword: string
}

export default function AdminUsersPage() {
  const [users, setUsers] = useState<User[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [updatingId, setUpdatingId] = useState<number | null>(null)
  const [resetTarget, setResetTarget] = useState<User | null>(null)
  const [resetting, setResetting] = useState(false)
  const [resetResult, setResetResult] = useState<ResetResult | null>(null)

  function loadUsers() {
    setError(null)
    api
      .get<User[]>('/admin/users')
      .then(setUsers)
      .catch((err) => setError(err.message ?? 'No se pudieron cargar los usuarios.'))
  }

  useEffect(() => {
    loadUsers()
  }, [])

  async function toggleActive(u: User) {
    setUpdatingId(u.id)
    setError(null)
    try {
      const updated = await api.put<User>(`/admin/users/${u.id}`, { isActive: !u.isActive })
      setUsers((prev) => (prev ? prev.map((x) => (x.id === updated.id ? updated : x)) : prev))
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError('Ocurrió un error inesperado al actualizar el usuario.')
      }
    } finally {
      setUpdatingId(null)
    }
  }

  async function confirmReset() {
    if (!resetTarget) return
    setResetting(true)
    setError(null)
    setResetResult(null)
    try {
      const result = await api.post<ResetResult>(`/admin/users/${resetTarget.id}/reset-password`, {})
      setResetResult(result)
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError('Ocurrió un error inesperado al resetear la contraseña.')
      }
    } finally {
      setResetting(false)
      setResetTarget(null)
    }
  }

  return (
    <div>
      <div className="admin-header">
        <h1>Administración de Usuarios</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!users && !error && <StatusMessage kind="loading" message="Cargando usuarios..." />}

      {resetResult && (
        <div className="admin-notice">
          <StatusMessage
            kind="success"
            message={`Contraseña temporal para ${resetResult.email}: ${resetResult.temporaryPassword}. Comunicala por un canal seguro: al ingresar deberá cambiarla y este diálogo no podrá recuperarse.`}
          />
          <div className="pp-form__actions">
            <button type="button" className="btn btn-secondary" onClick={() => setResetResult(null)}>
              Cerrar (la contraseña ya no se mostrará)
            </button>
          </div>
        </div>
      )}
      {!users && !error && <StatusMessage kind="loading" message="Cargando usuarios..." />}

      {users && users.length === 0 && <div className="empty-state">No hay usuarios registrados.</div>}

      {users && users.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Email</th>
                <th>Roles</th>
                <th>Estado</th>
                <th>Último acceso</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id}>
                  <td>
                    {u.firstName} {u.lastName}
                  </td>
                  <td>{u.email}</td>
                  <td>{u.roles.join(', ')}</td>
                  <td>
                    <span className={`badge badge--${u.isActive ? 'active' : 'inactive'}`}>
                      {u.isActive ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                  <td>{u.lastAccessUtc ? new Date(u.lastAccessUtc).toLocaleString() : '—'}</td>
                  <td>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      disabled={updatingId === u.id}
                      onClick={() => toggleActive(u)}
                    >
                      {u.isActive ? 'Desactivar' : 'Activar'}
                    </button>{' '}
                    <button
                      type="button"
                      className="btn btn-secondary"
                      disabled={resetting}
                      onClick={() => setResetTarget(u)}
                    >
                      Resetear contraseña
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <ConfirmModal
        open={resetTarget !== null}
        title="Resetear contraseña"
        message={
          resetTarget
            ? `Se generará una contraseña temporal para ${resetTarget.email}. Deberá cambiarla al ingresar. ¿Continuar?`
            : ''
        }
        confirmLabel={resetting ? 'Generando...' : 'Resetear'}
        onConfirm={() => { if (!resetting) void confirmReset() }}
        onCancel={() => { if (!resetting) setResetTarget(null) }}
      />
    </div>
  )
}
