import { useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { MATCH_STATUS_LABELS, type AdminOfficialLeague, type Match, type Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { roundDisplayName } from '../utils/roundDisplay'
import { appendReturnTo } from '../utils/adminReturnTo'

type ScopedRound = { round: Round; matches: Match[] }

function Team({ name, logoUrl }: { name: string; logoUrl: string | null }) {
  return <span className="match-team">{logoUrl ? <img src={logoUrl} alt="" /> : <span className="match-team__placeholder" aria-hidden="true">{name.slice(0, 2).toUpperCase()}</span>}<span>{name}</span></span>
}

export default function AdminOfficialLeagueMatchesPage() {
  const { leagueId } = useParams()
  const [league, setLeague] = useState<AdminOfficialLeague | null>(null)
  const [fixture, setFixture] = useState<ScopedRound[] | null>(null)
  const [expandedRoundIds, setExpandedRoundIds] = useState<Set<number>>(new Set())
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    async function load() {
      const current = await api.get<AdminOfficialLeague>(`/admin/official-leagues/${leagueId}`)
      const allRounds = await api.get<Round[]>(`/editions/${current.editionId}/rounds`)
      const ordered = [...allRounds].sort((a, b) => a.order - b.order)
      const from = ordered.find(round => round.id === current.roundFromId)?.order
      const to = ordered.find(round => round.id === current.roundToId)?.order
      const scoped = current.scopeType === 'RoundRange'
        ? ordered.filter(round => from != null && to != null && round.order >= from && round.order <= to)
        : ordered
      const matches = await Promise.all(scoped.map(round => api.get<Match[]>(`/rounds/${round.id}/matches`)))
      return { current, fixture: scoped.map((round, index) => ({ round, matches: matches[index] })) }
    }
    load().then(result => {
      if (cancelled) return
      setLeague(result.current); setFixture(result.fixture)
      const relevant = [...result.fixture].reverse().find(item => item.matches.some(match => match.status === 'Finished'))
        ?? result.fixture.find(item => item.matches.some(match => match.status === 'Scheduled'))
        ?? result.fixture[result.fixture.length - 1]
      setExpandedRoundIds(relevant ? new Set([relevant.round.id]) : new Set())
    }).catch(reason => { if (!cancelled) setError(reason.message ?? 'No se pudo cargar el fixture compartido.') })
    return () => { cancelled = true }
  }, [leagueId])

  const matchCount = useMemo(() => fixture?.reduce((total, item) => total + item.matches.length, 0) ?? 0, [fixture])
  const returnTo = league ? `/admin/official-leagues/${league.id}/matches` : null
  function toggleRound(roundId: number) {
    setExpandedRoundIds(current => {
      const next = new Set(current)
      if (next.has(roundId)) next.delete(roundId)
      else next.add(roundId)
      return next
    })
  }

  return <div>
    <div className="breadcrumb"><Link to="/admin/official-leagues">← Competencias EL NENE</Link></div>
    {league && <>
      <div className="admin-header"><div><h1>{league.name}</h1><p className="admin-help">Partidos compartidos de la competencia de referencia; no son copias del fixture.</p></div><Link className="btn btn-secondary" to={`/admin/official-leagues/${league.id}/edit`}>Editar</Link></div>
      <dl className="official-fixture-context">
        <div><dt>Competencia de referencia</dt><dd>{league.competitionName}</dd></div>
        <div><dt>Edición</dt><dd>{league.editionName}</dd></div>
        <div><dt>Alcance</dt><dd>{league.scopeType === 'FullCompetition' ? 'Toda la edición' : `${league.roundFromName} a ${league.roundToName}`}</dd></div>
        <div><dt>Fixture utilizado</dt><dd>{fixture?.length ?? 0} fechas · {matchCount} partidos</dd></div>
      </dl>
    </>}
    {error && <StatusMessage kind="error" message={error} />}
    {!fixture && !error && <StatusMessage kind="loading" message="Cargando partidos compartidos..." />}
    {fixture?.map(({ round, matches }) => <section className="official-fixture-round" key={round.id}>
      <div className="official-fixture-round__header">
        <button type="button" className="official-fixture-round__toggle" aria-expanded={expandedRoundIds.has(round.id)} aria-controls={`official-round-${round.id}`} onClick={() => toggleRound(round.id)}>
          <span>{roundDisplayName(round, matches)}</span><small>{expandedRoundIds.has(round.id) ? 'Ocultar' : 'Ver'}</small>
        </button>
        <Link to={appendReturnTo(`/rounds/${round.id}/matches`, returnTo)} className="btn btn-secondary">Administrar Partidos</Link>
      </div>
      {expandedRoundIds.has(round.id) && <div id={`official-round-${round.id}`}>
        {matches.length === 0 ? <div className="empty-state">Esta fecha no tiene partidos.</div> : <div className="table-wrap"><table className="admin-table"><thead><tr><th>ID</th><th>Partido</th><th>Inicio</th><th>Estado</th><th>Resultado</th></tr></thead><tbody>{matches.map(match => <tr key={match.id}><td>#{match.id}</td><td><div className="match-versus"><Team name={match.participantHome} logoUrl={match.homeTeamLogoUrl}/><strong>vs</strong><Team name={match.participantAway} logoUrl={match.awayTeamLogoUrl}/></div></td><td>{new Date(match.startsAtUtc).toLocaleString('es-AR')}</td><td><span className={`badge badge--${match.status}`}>{MATCH_STATUS_LABELS[match.status]}</span></td><td>{match.homeGoals != null && match.awayGoals != null ? `${match.homeGoals} – ${match.awayGoals}` : '—'}</td></tr>)}</tbody></table></div>}
      </div>}
    </section>)}
  </div>
}
