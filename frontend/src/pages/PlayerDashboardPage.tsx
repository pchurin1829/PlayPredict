import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type {
  LeagueSummary,
  MatchWithPrediction,
  RankingEntry,
  Prize,
  Competition,
  Edition,
  Round,
  LeagueDetail,
} from '../api/types'
import { useAuth } from '../auth/AuthContext'
import { useCompanySettings } from '../company/CompanySettingsContext'
import DashboardHero from '../components/player/DashboardHero'
import MatchPredictionCard from '../components/player/MatchPredictionCard'
import RankingPreview from '../components/player/RankingPreview'
import CurrentRoundCard from '../components/player/CurrentRoundCard'
import PrizeHighlight from '../components/player/PrizeHighlight'
import SponsorBanner from '../components/player/SponsorBanner'
import ComingSoonBadge from '../components/player/ComingSoonBadge'
import './PlayerDashboardPage.css'

export default function PlayerDashboardPage() {
  const { user } = useAuth()
  const { company } = useCompanySettings()
  const companyName = company.shortName || 'PlayPredict'

  const [leagues, setLeagues] = useState<LeagueSummary[]>([])
  const [leagueContexts, setLeagueContexts] = useState<Array<{ league: LeagueDetail; matches: MatchWithPrediction[] }>>([])
  const [ranking, setRanking] = useState<RankingEntry[]>([])
  const [prizes, setPrizes] = useState<Prize[]>([])
  const [activeEdition, setActiveEdition] = useState<Edition | null>(null)
  const [rounds, setRounds] = useState<Round[]>([])
  const [competitions, setCompetitions] = useState<Competition[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let cancelled = false

    async function loadDashboard() {
      try {
        const [leaguesData, competitionsData] = await Promise.all([
          api.get<LeagueSummary[]>('/leagues/mine'),
          api.get<Competition[]>('/competitions'),
        ])

        if (cancelled) return
        setLeagues(leaguesData)
        setCompetitions(competitionsData)

        if (leaguesData.length > 0) {
          const firstLeague = leaguesData[0]
          const contexts = await Promise.all(leaguesData.map(async (league) => {
            const [detail, leagueMatches] = await Promise.all([
              api.get<LeagueDetail>(`/leagues/${league.id}`),
              api.get<MatchWithPrediction[]>(`/leagues/${league.id}/matches`),
            ])
            return { league: detail, matches: leagueMatches }
          }))

          if (cancelled) return
          setLeagueContexts(contexts)

          // Get active edition from the league's competition
          const compEditions = await api.get<Edition[]>(`/competitions/${firstLeague.competitionId}/editions`).catch(() => [])
          const activeEd = compEditions.find((e) => e.status === 'Active')
          const editionId = activeEd?.id

          if (editionId) {
            const [rankingData, editionData, editionRounds] = await Promise.all([
              api.get<RankingEntry[]>(`/rankings/editions/${editionId}`).catch(() => []),
              api.get<Edition>(`/editions/${editionId}`).catch(() => null),
              api.get<Round[]>(`/editions/${editionId}/rounds`).catch(() => []),
            ])

            if (cancelled) return
            setRanking(rankingData)
            setActiveEdition(editionData)
            setRounds(editionRounds)

            const prizesData = await api.get<Prize[]>(`/prizes/editions/${editionId}`).catch(() => [])
            if (!cancelled) setPrizes(prizesData)
          }
        }
      } catch {
        // silently fail — dashboard shows what it can
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    loadDashboard()
    return () => { cancelled = true }
  }, [])

  function handlePredictionUpdated(updatedMatch: MatchWithPrediction) {
    const leagueId = updatedMatch.myPrediction?.leagueId
    setLeagueContexts((prev) => prev.map((context) => (
      context.league.id === leagueId
        ? { ...context, matches: context.matches.map((match) => match.id === updatedMatch.id ? updatedMatch : match) }
        : context
    )))
  }

  const pendingGroups = leagueContexts.flatMap((context) => {
    const byRound = new Map<number, MatchWithPrediction[]>()
    context.matches
      .filter((match) => match.canPredict && !match.myPrediction)
      .forEach((match) => byRound.set(match.roundId, [...(byRound.get(match.roundId) ?? []), match]))
    return Array.from(byRound, ([roundId, groupMatches]) => ({
      league: context.league,
      roundId,
      roundName: context.league.rounds.find((round) => round.id === roundId)?.name ?? 'Fecha',
      matches: groupMatches.sort((a, b) => Date.parse(a.startsAtUtc) - Date.parse(b.startsAtUtc)),
    }))
  }).sort((a, b) => Date.parse(a.matches[0].startsAtUtc) - Date.parse(b.matches[0].startsAtUtc))

  const upcomingGroups = leagueContexts.flatMap((context) => {
    const byRound = new Map<number, MatchWithPrediction[]>()
    context.matches
      .filter((match) => !!match.myPrediction && (
        match.canPredict || (match.status === 'Scheduled' && Date.parse(match.startsAtUtc) > Date.now())
      ))
      .forEach((match) => byRound.set(match.roundId, [...(byRound.get(match.roundId) ?? []), match]))
    return Array.from(byRound, ([roundId, groupMatches]) => ({
      league: context.league,
      roundId,
      roundName: context.league.rounds.find((round) => round.id === roundId)?.name ?? 'Fecha',
      matches: groupMatches.sort((a, b) => {
        const pendingDifference = Number(!!a.myPrediction) - Number(!!b.myPrediction)
        return pendingDifference || Date.parse(a.startsAtUtc) - Date.parse(b.startsAtUtc)
      }),
    }))
  }).sort((a, b) => Date.parse(a.matches[0].startsAtUtc) - Date.parse(b.matches[0].startsAtUtc))
  const bestPosition = ranking.length > 0
    ? ranking.find((r) => r.userId === user?.id)?.position ?? null
    : null
  const totalPoints = ranking.length > 0
    ? ranking.find((r) => r.userId === user?.id)?.points ?? null
    : null
  const activeCompetition = leagues.length > 0
    ? competitions.find((c) => c.id === leagues[0].competitionId)
    : competitions.find((c) => c.isActive)
  const currentRoundIndex = rounds.length > 0
    ? 1
    : null

  if (loading) {
    return (
      <div className="pdash">
        <div className="pdash__loading">Cargando tu dashboard...</div>
      </div>
    )
  }

  if (leagues.length === 0) {
    return (
      <div className="pdash">
        <div className="pdash__main">
          <div className="pdash__empty-state">
            <span className="pdash__empty-icon">⚽</span>
            <h2 className="pdash__empty-title">¡Bienvenido a PlayPredict!</h2>
            <p className="pdash__empty-text">
              Para empezar a pronosticar, participá en una competencia {companyName} o creá tu propia Liga con amigos sobre una competencia de referencia.
            </p>
            <div className="pdash__empty-actions">
              <Link to="/competitions/explore" className="pp-btn pp-btn--primary" style={{ fontSize: '1rem', padding: '0.7rem 1.5rem' }}>
                Explorar Competencias
              </Link>
              <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
                ✋ Unirme a una Liga de amigos
              </Link>
            </div>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="pdash">
      <div className="pdash__main">
        <DashboardHero
          bestPosition={bestPosition}
          totalPoints={totalPoints}
        />

        <section className="pdash__section pdash__pending-section">
          <h2 className="pdash__section-title">Pronósticos pendientes</h2>
          {pendingGroups.length === 0 ? (
            <div className="pdash__empty-card">No tenés pronósticos pendientes por el momento.</div>
          ) : (
            <div className="pdash__pending-groups">
              {pendingGroups.map((group) => (
                <article key={`${group.league.id}-${group.roundId}`} className={`pdash__context-card pdash__context-card--${group.league.leagueType.toLowerCase()}`}>
                  <div className="pdash__context-main">
                    <span className="pdash__context-competition">{group.league.competitionName}</span>
                    <strong className="pdash__context-line">
                      {group.league.name} · {group.roundName}
                    </strong>
                    <span className="pdash__context-count">
                      {group.matches.length} partido{group.matches.length === 1 ? '' : 's'} pendiente{group.matches.length === 1 ? '' : 's'}
                    </span>
                    <span className="pdash__context-time">
                      Primer cierre: {new Date(group.matches[0].startsAtUtc).toLocaleString('es-AR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                  <Link className="pp-btn pp-btn--primary pdash__context-cta" to={`/leagues/${group.league.id}?tab=pronosticos&round=${group.roundId}`}>
                    Pronosticar
                  </Link>
                </article>
              ))}
            </div>
          )}
        </section>

        <div id="proximos-partidos" className="pdash__section">
          <h2 className="pdash__section-title">Tus próximos partidos</h2>
          {upcomingGroups.length === 0 ? (
            <div className="pdash__empty-card">
              No hay próximos partidos disponibles en este momento.
            </div>
          ) : (
            <div className="pdash__upcoming-groups">
              {upcomingGroups.map((group) => (
                <section key={`${group.league.id}-${group.roundId}`} className="pdash__upcoming-group">
                  <div className="pdash__upcoming-heading">
                    <div>
                      <h3>{group.league.competitionName} — {group.roundName}</h3>
                      <p>{group.league.name}</p>
                    </div>
                    <Link to={`/leagues/${group.league.id}?tab=pronosticos&round=${group.roundId}`}>Ver fecha</Link>
                  </div>
                  <div className="pdash__matches">
                    {group.matches.map((match) => (
                      <MatchPredictionCard
                        key={`${group.league.id}-${match.id}`}
                        match={match}
                        leagueId={group.league.id}
                        onPredictionUpdated={handlePredictionUpdated}
                      />
                    ))}
                  </div>
                </section>
              ))}
            </div>
          )}
        </div>

        <div className="pdash__future-section">
          <div className="pdash__future-card">
            <h3 className="pdash__future-title">Más funcionalidades en desarrollo</h3>
            <p className="pdash__future-text">
              Estadísticas avanzadas, goleadores, comparativas y mucho más.
            </p>
            <ComingSoonBadge />
          </div>
        </div>
      </div>

      <div className="pdash__sidebar-right">
        <RankingPreview ranking={ranking} />

        <CurrentRoundCard
          roundName={rounds.length > 0 ? rounds[0].name : null}
          competitionName={activeCompetition?.name ?? activeEdition?.name ?? null}
          currentRound={currentRoundIndex}
          totalRounds={rounds.length > 0 ? rounds.length : null}
        />

        <PrizeHighlight prizes={prizes} />

        <SponsorBanner />
      </div>
    </div>
  )
}
