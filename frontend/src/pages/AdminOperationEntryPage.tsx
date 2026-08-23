import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition, Edition } from '../api/types'
import StatusMessage from '../components/StatusMessage'

type Operation = 'fixture' | 'results' | 'scoring'

const COPY: Record<Operation, { title: string; description: string; action: string }> = {
  fixture: {
    title: 'Fixture / Partidos',
    description: 'Seleccioná la competencia y edición cuyo fixture querés gestionar.',
    action: 'Ver Fixture / Partidos',
  },
  results: {
    title: 'Resultados',
    description: 'Seleccioná la competencia y edición cuyos resultados querés gestionar.',
    action: 'Gestionar resultados',
  },
  scoring: {
    title: 'Configuración de puntuación',
    description: 'Las reglas de puntuación se configuran por Edición. Seleccioná una competencia y una edición.',
    action: 'Configurar puntuación',
  },
}

export default function AdminOperationEntryPage({ operation }: { operation: Operation }) {
  const copy = COPY[operation]
  const [competitions, setCompetitions] = useState<Competition[] | null>(null)
  const [selected, setSelected] = useState<Competition | null>(null)
  const [editions, setEditions] = useState<Edition[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.get<Competition[]>('/competitions').then(setCompetitions)
      .catch((reason) => setError(reason.message ?? 'No se pudieron cargar las Competencias.'))
  }, [])

  function selectCompetition(competition: Competition) {
    setSelected(competition); setEditions(null); setError(null)
    api.get<Edition[]>(`/competitions/${competition.id}/editions`).then(setEditions)
      .catch((reason) => setError(reason.message ?? 'No se pudieron cargar las Ediciones.'))
  }

  function destination(editionId: number): string {
    if (operation === 'scoring') return `/editions/${editionId}/scoring-configuration`
    return `/editions/${editionId}/rounds?adminFlow=${operation}`
  }

  return (
    <div>
      <div className="breadcrumb"><Link to="/admin">← Administración</Link></div>
      <div className="admin-header"><div><h1>{copy.title}</h1><p className="admin-help">{copy.description}</p></div></div>
      {error && <StatusMessage kind="error" message={error} />}
      {!competitions && !error && <StatusMessage kind="loading" message="Cargando Competencias..." />}

      {competitions && (
        <section className="admin-selection">
          <h2>1. Competencia</h2>
          <div className="admin-selection__grid">
            {competitions.map((competition) => (
              <button key={competition.id} type="button" className={`admin-selection__option ${selected?.id === competition.id ? 'admin-selection__option--active' : ''}`} onClick={() => selectCompetition(competition)}>
                <strong>{competition.name}</strong><span>{competition.sport}</span>
              </button>
            ))}
          </div>
        </section>
      )}

      {selected && (
        <section className="admin-selection">
          <h2>2. Edición — {selected.name}</h2>
          {!editions && !error && <StatusMessage kind="loading" message="Cargando Ediciones..." />}
          {editions?.length === 0 && <div className="empty-state">Esta Competencia no tiene Ediciones.</div>}
          <div className="admin-selection__grid">
            {editions?.map((edition) => (
              <article key={edition.id} className="admin-selection__edition">
                <div><strong>{edition.name}</strong><span>{new Date(edition.startDateUtc).getFullYear()} · {edition.status}</span></div>
                <Link className="btn btn-primary" to={destination(edition.id)}>{copy.action}</Link>
              </article>
            ))}
          </div>
        </section>
      )}
    </div>
  )
}
