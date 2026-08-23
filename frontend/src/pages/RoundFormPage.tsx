import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function RoundFormPage() {
  const { editionId: editionIdParam, roundId } = useParams()
  const isEdit = Boolean(roundId)
  const navigate = useNavigate()

  const [editionId, setEditionId] = useState<string | undefined>(editionIdParam)
  const [name, setName] = useState('')
  const [order, setOrder] = useState(1)
  const [suggestedOrder, setSuggestedOrder] = useState(1)
  const [existingRounds, setExistingRounds] = useState<Round[]>([])

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    if (!isEdit) {
      api.get<Round[]>(`/editions/${editionIdParam}/rounds`).then((rounds) => {
        setExistingRounds(rounds)
        const used = new Set(rounds.map((round) => round.order))
        let next = 1
        while (used.has(next)) next++
        setSuggestedOrder(next)
        setOrder(next)
        setName(`FECHA ${next}`)
      }).catch((err) => setError(err.message ?? 'No se pudieron cargar las Fechas existentes.'))
        .finally(() => setLoading(false))
      return
    }
    api.get<Round>(`/rounds/${roundId}`).then(async (r) => ({ round: r, rounds: await api.get<Round[]>(`/editions/${r.editionId}/rounds`) }))
      .then(({ round: r, rounds }) => {
        setEditionId(String(r.editionId))
        setName(r.name)
        setOrder(r.order)
        setExistingRounds(rounds.filter((candidate) => candidate.id !== r.id))
        const used = new Set(rounds.map((candidate) => candidate.order))
        let next = 1
        while (used.has(next)) next++
        setSuggestedOrder(next)
      })
      .catch((err) => setError(err.message ?? 'No se pudo cargar la fecha.'))
      .finally(() => setLoading(false))
  }, [roundId, isEdit])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    const payload = {
      name,
      order,
    }

    try {
      if (isEdit) {
        await api.put(`/rounds/${roundId}`, payload)
        setSaved(true)
      } else {
        const created = await api.post<Round>(`/editions/${editionIdParam}/rounds`, payload)
        setSaved(true)
        navigate(`/rounds/${created.id}/edit`, { replace: true })
        return
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.fieldErrors)
      } else {
        setError('Ocurrió un error inesperado al guardar.')
      }
    } finally {
      setSaving(false)
    }
  }

  function handleOrderChange(nextOrder: number) {
    setOrder(nextOrder)
    const occupied = existingRounds.find((round) => round.order === nextOrder)
    setFieldErrors((current) => {
      const next = { ...current }
      if (occupied) next.order = [`El orden ${nextOrder} ya está utilizado por ${occupied.name}. El próximo orden disponible es ${suggestedOrder}.`]
      else delete next.order
      return next
    })
  }

  if (loading) {
    return <StatusMessage kind="loading" message="Cargando fecha..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to={`/editions/${editionId}/rounds`}>← Volver a Fechas</Link>
      </div>
      <div className="admin-header">
        <h1>{isEdit ? 'Editar Fecha' : 'Nueva Fecha'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Fecha guardada correctamente." />}
      {!isEdit && <p className="admin-help">Próxima fecha sugerida: <strong>Fecha {suggestedOrder} (orden {suggestedOrder})</strong></p>}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-field">
          <label htmlFor="name">Nombre</label>
          <input id="name" type="text" value={name} onChange={(e) => setName(e.target.value)} />
          {fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}
        </div>

        <div className="form-field">
          <label htmlFor="order">Orden</label>
          <input
            id="order"
            type="number"
            min={1}
            value={order}
            onChange={(e) => handleOrderChange(Number(e.target.value))}
          />
          {fieldErrors.order && <span className="form-field-error">{fieldErrors.order[0]}</span>}
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving || Boolean(fieldErrors.order)}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          <Link to={`/editions/${editionId}/rounds`} className="btn btn-secondary">Cancelar</Link>
          {isEdit && (
            <Link to={`/rounds/${roundId}/matches`} className="btn btn-tertiary form-actions__contextual">
              Ver Partidos
            </Link>
          )}
        </div>
      </form>
    </div>
  )
}
