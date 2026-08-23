import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { AdminOfficialLeague } from '../api/types'
import StatusMessage from '../components/StatusMessage'

export default function AdminOfficialLeaguesListPage() {
  const [leagues, setLeagues] = useState<AdminOfficialLeague[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api.get<AdminOfficialLeague[]>('/admin/official-leagues')
      .then(setLeagues)
      .catch((reason) => setError(reason.message ?? 'No se pudieron cargar las Ligas Oficiales.'))
  }, [])

  return (
    <div>
      <div className="admin-header">
        <div>
          <h1>Ligas Oficiales PlayPredict</h1>
          <p className="admin-help">Nombres comerciales montados sobre una Competencia y Edición deportiva.</p>
        </div>
        <Link to="/admin/official-leagues/new" className="btn btn-primary">+ Nueva Liga Oficial</Link>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {!leagues && !error && <StatusMessage kind="loading" message="Cargando Ligas Oficiales..." />}
      {leagues?.length === 0 && <div className="empty-state">No hay Ligas Oficiales creadas.</div>}

      {leagues && leagues.length > 0 && (
        <div className="official-league-admin-grid">
          {leagues.map((league) => (
            <article className="official-league-admin-card" key={league.id}>
              <div className="official-league-admin-card__header">
                <h2>{league.name}</h2>
                <span className={`badge badge--${league.isActive ? 'active' : 'inactive'}`}>{league.isActive ? 'Activa' : 'Suspendida'}</span>
              </div>
              <dl className="official-league-admin-card__details">
                <div><dt>Fuente</dt><dd>{league.competitionName} · {league.editionName}</dd></div>
                <div><dt>Alcance</dt><dd>{league.scopeType === 'FullCompetition' ? 'Toda la edición' : `${league.roundFromName} → ${league.roundToName}`}</dd></div>
                <div className="official-league-admin-card__fixture"><dt>Fixture utilizado</dt><dd>{league.roundsCount} {league.roundsCount === 1 ? 'fecha' : 'fechas'} · {league.matchesCount} {league.matchesCount === 1 ? 'partido' : 'partidos'}</dd></div>
                <div><dt>Participantes</dt><dd>{league.participantsCount}</dd></div>
              </dl>
              <div className="official-league-admin-card__actions"><Link className="btn btn-secondary" to={`/admin/official-leagues/${league.id}/edit`}>Editar</Link></div>
            </article>
          ))}
        </div>
      )}
    </div>
  )
}
