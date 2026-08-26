import { useEffect, useState } from 'react'
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { MATCH_STATUS_LABELS, type AdminOfficialLeague, type Competition, type Edition, type Match, type Round } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import MatchResultModal from '../components/MatchResultModal'
import ConfirmModal from '../components/ConfirmModal'
import { roundDisplayName } from '../utils/roundDisplay'
import { appendReturnTo, officialLeagueIdFromReturnTo, validAdminReturnTo } from '../utils/adminReturnTo'

function TeamInMatch({ name, logoUrl }: { name: string; logoUrl: string | null }) {
  return <span className="match-team">{logoUrl ? <img src={logoUrl} alt="" /> : <span className="match-team__placeholder" aria-hidden="true">{name.slice(0, 2).toUpperCase()}</span>}<span>{name}</span></span>
}

export default function MatchesListPage() {
  const { roundId } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const adminFlow = searchParams.get('adminFlow')
  const returnTo = validAdminReturnTo(searchParams.get('returnTo'))
  const contextLeagueId = officialLeagueIdFromReturnTo(returnTo)

  const [round, setRound] = useState<Round | null>(null)
  const [editionRounds, setEditionRounds] = useState<Round[]>([])
  const [edition, setEdition] = useState<Edition | null>(null)
  const [competition, setCompetition] = useState<Competition | null>(null)
  const [matches, setMatches] = useState<Match[] | null>(null)
  const [officialLeagues, setOfficialLeagues] = useState<AdminOfficialLeague[]>([])
  const [contextLeague, setContextLeague] = useState<AdminOfficialLeague | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [resultTarget, setResultTarget] = useState<Match | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<Match | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [savedMessage, setSavedMessage] = useState<string | null>(
    (location.state as { savedMessage?: string } | null)?.savedMessage ?? null,
  )

  useEffect(() => {
    if (!savedMessage) return
    navigate(`${location.pathname}${location.search}`, { replace: true })
    const timeout = setTimeout(() => setSavedMessage(null), 4000)
    return () => clearTimeout(timeout)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function loadMatches() {
    setError(null)
    return api.get<Match[]>(`/rounds/${roundId}/matches`)
  }

  useEffect(() => {
    let cancelled = false

    async function loadContext() {
      const [r, ms, leagues] = await Promise.all([
        api.get<Round>(`/rounds/${roundId}`),
        loadMatches(),
        api.get<AdminOfficialLeague[]>('/admin/official-leagues').catch(() => []),
      ])
      const ed = await api.get<Edition>(`/editions/${r.editionId}`)
      const [comp, rounds] = await Promise.all([
        api.get<Competition>(`/competitions/${ed.competitionId}`),
        api.get<Round[]>(`/editions/${ed.id}/rounds`),
      ])
      return { r, ms, leagues, ed, comp, rounds }
    }

    loadContext()
      .then(({ r, ms, leagues, ed, comp, rounds }) => {
        if (cancelled) return
        setRound(r)
        setEdition(ed)
        setCompetition(comp)
        setEditionRounds(rounds)
        setMatches(ms)
        setOfficialLeagues(leagues.filter((league) => league.editionId === r.editionId))
        const candidate = contextLeagueId == null ? null : leagues.find((league) => league.id === contextLeagueId) ?? null
        const fromOrder = rounds.find((item) => item.id === candidate?.roundFromId)?.order
        const toOrder = rounds.find((item) => item.id === candidate?.roundToId)?.order
        const consumesRound = candidate?.editionId === r.editionId && (candidate.scopeType === 'FullCompetition'
          || (fromOrder != null && toOrder != null && r.order >= fromOrder && r.order <= toOrder))
        setContextLeague(consumesRound ? candidate : null)
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar los partidos.')
      })

    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [contextLeagueId, roundId])

  function handleResultSaved(updated: Match) {
    setMatches((prev) => (prev ? prev.map((m) => (m.id === updated.id ? updated : m)) : prev))
    setResultTarget(null)
    setSavedMessage('Resultado oficial guardado correctamente.')
    setTimeout(() => setSavedMessage(null), 4000)
  }

  async function deleteMatch() {
    if (!deleteTarget) return
    setDeleting(true); setError(null)
    try {
      await api.del(`/matches/${deleteTarget.id}`)
      setMatches((current) => current?.filter((match) => match.id !== deleteTarget.id) ?? current)
      setSavedMessage('Partido eliminado correctamente.')
      setDeleteTarget(null)
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo eliminar el partido.')
      setDeleteTarget(null)
    } finally {
      setDeleting(false)
    }
  }

  const currentRoundIndex = round ? editionRounds.findIndex((item) => item.id === round.id) : -1
  const previousRound = currentRoundIndex > 0 ? editionRounds[currentRoundIndex - 1] : null
  const nextRound = currentRoundIndex >= 0 && currentRoundIndex < editionRounds.length - 1 ? editionRounds[currentRoundIndex + 1] : null
  const flowParams = new URLSearchParams()
  if (adminFlow) flowParams.set('adminFlow', adminFlow)
  if (returnTo) flowParams.set('returnTo', returnTo)
  const flowQuery = flowParams.size > 0 ? `?${flowParams.toString()}` : ''

  return (
    <div>
      <div className="breadcrumb">
        {returnTo && contextLeague && round ? (
          <><Link to="/admin/official-leagues">Competencias EL NENE</Link> &gt; <Link to={returnTo}>{contextLeague.name}</Link> &gt; Partidos &gt; {roundDisplayName(round, matches ?? [])}</>
        ) : round && edition && competition && (
          <><Link to="/competitions">Competencias</Link> &gt; <Link to={`/competitions/${competition.id}/editions`}>{competition.name}</Link> &gt; <Link to={`/editions/${edition.id}/rounds${adminFlow ? `?adminFlow=${adminFlow}` : ''}`}>{edition.name}</Link> &gt; {adminFlow === 'results' ? 'Resultados' : round.name}</>
        )}
      </div>
      <div className="admin-header">
        <h1>{adminFlow === 'results' ? 'Resultados' : 'Partidos'} {round ? `— ${roundDisplayName(round, matches ?? [])}` : ''}</h1>
        {adminFlow !== 'results' && <Link to={appendReturnTo(`/rounds/${roundId}/matches/new`, returnTo)} className="btn btn-primary">+ Nuevo Partido</Link>}
      </div>
      {(previousRound || nextRound) && (
        <nav className="round-navigation" aria-label="Navegación entre fechas">
          <span>{previousRound && <Link className="btn btn-secondary" to={`/rounds/${previousRound.id}/matches${flowQuery}`}>← {roundDisplayName(previousRound)}</Link>}</span>
          <span>{nextRound && <Link className="btn btn-secondary" to={`/rounds/${nextRound.id}/matches${flowQuery}`}>{roundDisplayName(nextRound)} →</Link>}</span>
        </nav>
      )}
      {!contextLeague && officialLeagues.length > 0 && (
        <p className="admin-help">
          Estos partidos y sus resultados se reutilizan en: <strong>{officialLeagues.map((league) => league.name).join(', ')}</strong> y en las Ligas de Amigos de esta Edición.
        </p>
      )}

      {error && <StatusMessage kind="error" message={error} />}
      {savedMessage && <StatusMessage kind="success" message={savedMessage} />}
      {!matches && !error && <StatusMessage kind="loading" message="Cargando partidos..." />}

      {matches && matches.length === 0 && (
        <div className="empty-state">Esta fecha todavía no tiene partidos.</div>
      )}

      {matches && matches.length > 0 && (
        <div className="table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Partido</th>
                <th>Inicio</th>
                <th>Estado</th>
                <th>Resultado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {matches.map((m) => (
                <tr key={m.id}>
                  <td>
                    <div className="match-versus"><TeamInMatch name={m.participantHome} logoUrl={m.homeTeamLogoUrl} /><strong>vs</strong><TeamInMatch name={m.participantAway} logoUrl={m.awayTeamLogoUrl} /></div>
                  </td>
                  <td>{new Date(m.startsAtUtc).toLocaleString()}</td>
                  <td>
                    <span className={`badge badge--${m.status}`}>{MATCH_STATUS_LABELS[m.status]}</span>
                  </td>
                  <td>
                    {m.status === 'Finished' ? `${m.homeGoals} - ${m.awayGoals}` : '—'}
                  </td>
                  <td>
                    <div className="match-row-actions">
                      {m.status !== 'Finished' && <Link to={appendReturnTo(`/matches/${m.id}/edit`, returnTo)} className="btn btn-secondary">
                        Editar
                      </Link>}
                      {m.status === 'Finished' || m.homeGoals != null || m.awayGoals != null ? (
                        <span className="match-delete-disabled" title="No se puede eliminar este partido porque ya tiene un resultado cargado.">
                          <button className="btn btn-danger" disabled aria-describedby={`delete-reason-${m.id}`}>Eliminar</button>
                          <small id={`delete-reason-${m.id}`}>Resultado cargado</small>
                        </span>
                      ) : (
                        <button className="btn btn-danger" onClick={() => setDeleteTarget(m)}>Eliminar</button>
                      )}
                      <button className="btn btn-primary" onClick={() => setResultTarget(m)}>
                        {m.status === 'Finished' ? 'Corregir resultado' : 'Cargar resultado'}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {resultTarget && (
        <MatchResultModal
          match={resultTarget}
          onClose={() => setResultTarget(null)}
          onSaved={handleResultSaved}
        />
      )}
      <ConfirmModal open={Boolean(deleteTarget)} title="Eliminar partido" message={'¿Eliminar este partido?\nEsta acción no se puede deshacer.'} confirmLabel={deleting ? 'Eliminando...' : 'Eliminar'} onConfirm={deleteMatch} onCancel={() => setDeleteTarget(null)} />
    </div>
  )
}
