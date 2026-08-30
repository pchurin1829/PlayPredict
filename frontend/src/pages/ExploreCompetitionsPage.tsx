import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { LeagueSummary } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { useCompanySettings } from '../company/CompanySettingsContext'
import { leagueCreatePath } from '../utils/leagueCreateReturnTo'
import './PlayerPages.css'

export default function ExploreCompetitionsPage() {
  const { company } = useCompanySettings()
  const companyName = company.shortName || 'PlayPredict'
  const [leagues, setLeagues] = useState<LeagueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [joiningId, setJoiningId] = useState<number | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    api.get<LeagueSummary[]>('/leagues/officials')
      .then(items => { if (!cancelled) setLeagues(items) })
      .catch(reason => { if (!cancelled) setError(reason.message ?? `No se pudieron cargar las competencias ${companyName}.`) })
    return () => { cancelled = true }
  }, [companyName])

  async function handleJoinOfficial(leagueId: number, leagueName: string) {
    setJoiningId(leagueId)
    setMessage(null)
    try {
      await api.post<LeagueSummary>(`/leagues/${leagueId}/join`, {})
      setMessage(`Te uniste correctamente a ${leagueName} (Oficial).`)
      setLeagues(await api.get<LeagueSummary[]>('/leagues/officials'))
    } catch (reason) {
      setMessage(reason instanceof ApiError ? reason.message : 'Ocurrió un error al unirse.')
    } finally {
      setJoiningId(null)
      setTimeout(() => setMessage(null), 4000)
    }
  }

  return <div>
    <div className="pp-header">
      <h1>Explorar Competencias {companyName}</h1>
      <p className="pp-header__subtitle">Participá en las competencias oficiales de {companyName}.</p>
    </div>

    {error && <StatusMessage kind="error" message={error} />}
    {!leagues && !error && <StatusMessage kind="loading" message={`Cargando Competencias ${companyName}...`} />}
    {message && <StatusMessage kind={message.includes('error') || message.includes('incorrecto') ? 'error' : 'success'} message={message} />}

    {leagues?.length === 0 && <div className="pp-empty"><span className="pp-empty__icon">⚽</span><p className="pp-empty__text">No hay competencias oficiales activas todavía.</p></div>}

    {leagues && leagues.length > 0 && <div className="pp-grid">
      {leagues.map(league => <div key={league.id} className="pp-comp-card pp-comp-card--official-league">
        <div className="pp-comp-card__title-row">
          <h3 className="pp-comp-card__name">{league.name}</h3>
          <span className="pp-league-card__badge pp-league-card__badge--official">OFICIAL</span>
        </div>
        <span className="pp-comp-card__edition">Basada en: {league.competitionName} · {league.editionName}</span>
        <div className="pp-comp-card__actions">
          <div className={`pp-comp-card__official-state ${league.isParticipant ? 'pp-comp-card__official-state--joined' : 'pp-comp-card__official-state--available'}`}>
            <strong>{league.isParticipant ? '✓ Estás participando' : 'Todavía no participás'}</strong>
            {league.isParticipant
              ? <Link to={`/leagues/${league.id}`} className="pp-comp-card__action pp-comp-card__action--view">Ver</Link>
              : <button type="button" className="pp-comp-card__action" disabled={joiningId === league.id} onClick={() => handleJoinOfficial(league.id, league.name)}>{joiningId === league.id ? 'Uniéndose...' : 'Participar'}</button>}
          </div>
          <Link to={leagueCreatePath(league.id, '/competitions/explore')} className="pp-comp-card__action pp-comp-card__action--secondary">Crear Liga con amigos</Link>
        </div>
      </div>)}
    </div>}

  </div>
}
