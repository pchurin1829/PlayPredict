import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { Edition, Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { roundDisplayName } from '../utils/roundDisplay'

interface GenerateRoundsResult {
  existingCount: number
  createdCount: number
  totalCount: number
  message: string
  rounds: Round[]
}

export default function RoundsListPage() {
  const { editionId } = useParams()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const adminFlow = searchParams.get('adminFlow')
  const flowQuery = adminFlow ? `?adminFlow=${adminFlow}` : ''

  const [edition, setEdition] = useState<Edition | null>(null)
  const [rounds, setRounds] = useState<Round[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [generateCount, setGenerateCount] = useState(1)
  const [generating, setGenerating] = useState(false)
  const [generationMessage, setGenerationMessage] = useState<string | null>(null)

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
        setGenerateCount(Math.max(1, rs.length))
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las fechas.')
      })

    return () => {
      cancelled = true
    }
  }, [editionId])

  async function generateRounds() {
    setGenerating(true)
    setError(null)
    setGenerationMessage(null)
    try {
      const result = await api.post<GenerateRoundsResult>(`/editions/${editionId}/rounds/generate`, { count: generateCount })
      setRounds(result.rounds)
      setGenerateCount(result.totalCount)
      setGenerationMessage(result.message)
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudieron generar las Fechas.')
    } finally {
      setGenerating(false)
    }
  }

  async function exportFixture() {
    if (!edition) return
    try {
      const blob = await api.download(`/editions/${edition.id}/fixture.csv`)
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url; link.download = `fixture-${edition.id}.csv`; link.click()
      URL.revokeObjectURL(url)
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo exportar el fixture.')
    }
  }

  return (
    <div>
      <div className="breadcrumb">
        {edition && (
          <><Link to="/competitions">Competencias</Link> &gt; <Link to={`/competitions/${edition.competitionId}/editions`}>Ediciones</Link> &gt; {adminFlow === 'results' ? 'Resultados' : 'Fixture'}</>
        )}
      </div>
      <div className="admin-header">
        <div><h1>{adminFlow === 'results' ? 'Resultados' : 'Fixture / Partidos'} {edition ? `— ${edition.name}` : ''}</h1>{rounds && <p className="admin-help">Fechas actuales: <strong>{rounds.length}</strong></p>}</div>
        {edition && <button type="button" className="btn btn-secondary" onClick={exportFixture}>Exportar fixture CSV</button>}
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!rounds && !error && <StatusMessage kind="loading" message="Cargando fechas..." />}

      {rounds && adminFlow !== 'results' && (
        <section className="round-generator form-card">
          <div><h2>Generar Fechas</h2><p className="admin-help">Indicá el total de jornadas que debe tener la Edition. Sólo se crearán las faltantes.</p></div>
          <div className="round-generator__controls">
            <div className="form-field"><label htmlFor="generateRoundCount">Cantidad de fechas</label><input id="generateRoundCount" type="number" min="1" step="1" value={generateCount} onChange={(event) => setGenerateCount(Number(event.target.value))} /></div>
            <button type="button" className="btn btn-primary" disabled={generating || generateCount < 1} onClick={generateRounds}>{generating ? 'Generando...' : 'Generar'}</button>
          </div>
          {generationMessage && <StatusMessage kind="success" message={generationMessage} />}
          <div className="round-generator__manual"><span>¿Necesitás una jornada especial?</span><Link to={`/editions/${editionId}/rounds/new`} className="btn btn-secondary">+ Nueva Fecha</Link></div>
        </section>
      )}

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
                  onClick={() => navigate(`/rounds/${r.id}/matches${flowQuery}`)}
                >
                  <td>{r.order}</td>
                  <td>{roundDisplayName(r)}</td>
                  <td><div className="round-row-actions"><Link to={`/rounds/${r.id}/matches${flowQuery}`} className="btn btn-primary" onClick={(e) => e.stopPropagation()}>Ver Partidos</Link><Link to={`/rounds/${r.id}/edit`} className="btn btn-secondary" onClick={(e) => e.stopPropagation()}>Editar</Link></div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
