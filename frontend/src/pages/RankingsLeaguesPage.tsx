import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { LeagueSummary, RankingEntry } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { useAuth } from '../auth/AuthContext'
import './PlayerPages.css'

export default function RankingsLeaguesPage() {
  const [leagues, setLeagues] = useState<LeagueSummary[] | null>(null)
  const [selectedLeagueId, setSelectedLeagueId] = useState<number | null>(null)
  const [ranking, setRanking] = useState<RankingEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { user } = useAuth()

  useEffect(() => {
    let cancelled = false
    Promise.all([
      api.get<LeagueSummary[]>('/leagues/officials'),
      api.get<LeagueSummary[]>('/leagues/mine'),
    ]).then(([officials, mine]) => {
      if (cancelled) return
      const accessible = new Map<number, LeagueSummary>()
      officials.forEach(league => accessible.set(league.id, league))
      mine.forEach(league => accessible.set(league.id, league))
      const items = [...accessible.values()].filter(league => league.isActive).sort((a, b) => a.name.localeCompare(b.name))
      setLeagues(items)
      setSelectedLeagueId((mine.find(league => league.isActive) ?? items[0])?.id ?? null)
    }).catch(reason => {
      if (!cancelled) setError(reason.message ?? 'No se pudieron cargar las Competencias EL NENE.')
    })
    return () => { cancelled = true }
  }, [])

  useEffect(() => {
    if (selectedLeagueId == null) {
      setRanking([])
      return
    }
    let cancelled = false
    setRanking(null)
    setError(null)
    api.get<RankingEntry[]>(`/rankings/leagues/${selectedLeagueId}`)
      .then(data => { if (!cancelled) setRanking(data) })
      .catch(reason => { if (!cancelled) setError(reason.message ?? 'No se pudo cargar el ranking.') })
    return () => { cancelled = true }
  }, [selectedLeagueId])

  if (error) return <StatusMessage kind="error" message={error} />
  if (!leagues) return <StatusMessage kind="loading" message="Cargando rankings..." />
  const selectedLeague = leagues.find(league => league.id === selectedLeagueId)

  return <div>
    <div className="pp-header"><h1>Ranking</h1></div>
    {leagues.length === 0 ? <div className="pp-empty"><span className="pp-empty__icon">📊</span><p className="pp-empty__text">No hay Competencias EL NENE disponibles.</p></div> : <>
      <div className="pp-edition-select">
        <span className="pp-edition-select__label">Competencia EL NENE:</span>
        <select className="pp-edition-select__select" value={selectedLeagueId ?? ''} onChange={event => setSelectedLeagueId(Number(event.target.value))}>
          {leagues.map(league => <option key={league.id} value={league.id}>{league.name}</option>)}
        </select>
      </div>
      {selectedLeague && <p className="pp-header__subtitle">Basada en: {selectedLeague.competitionName} · {selectedLeague.editionName}</p>}
      {ranking === null && <StatusMessage kind="loading" message="Cargando ranking..." />}
      {ranking && ranking.length === 0 && <div className="pp-empty"><span className="pp-empty__icon">📊</span><p className="pp-empty__text">Todavía no hay pronósticos evaluados en esta Competencia EL NENE.</p></div>}
      {ranking && ranking.length > 0 && <div className="pp-ranking">
        <div className="pp-ranking__header"><h2>{selectedLeague?.name}</h2></div>
        <table><thead><tr><th>#</th><th>Jugador</th><th>Puntos</th><th>Exactos</th><th>Correctos</th><th>Evaluados</th></tr></thead>
          <tbody>{ranking.map(entry => {
            const isMe = user?.id === entry.userId
            return <tr key={entry.userId} className={isMe ? 'pp-ranking__me' : ''}>
              <td><span className={`pp-ranking__pos ${entry.position <= 3 ? `pp-ranking__pos--${entry.position}` : ''}`}>{entry.position}°</span></td>
              <td>{entry.firstName} {entry.lastName}{isMe && <span className="pp-ranking__me-badge">(Vos)</span>}</td>
              <td className="pp-ranking__points">{entry.points}</td><td>{entry.exactCount}</td><td>{entry.correctCount}</td><td>{entry.evaluatedCount}</td>
            </tr>
          })}</tbody>
        </table>
      </div>}
    </>}
  </div>
}
