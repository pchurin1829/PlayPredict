import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type {
  LeagueSummary,
  MatchWithPrediction,
  RankingEntry,
  Prize,
  Competition,
  Edition,
  Round,
} from '../api/types'
import { useAuth } from '../auth/AuthContext'
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

  const [leagues, setLeagues] = useState<LeagueSummary[]>([])
  const [matches, setMatches] = useState<MatchWithPrediction[]>([])
  const [firstLeagueId, setFirstLeagueId] = useState<number | null>(null)
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
          setFirstLeagueId(firstLeague.id)

          const leagueMatches = await api.get<MatchWithPrediction[]>(`/leagues/${firstLeague.id}/matches`)

          if (cancelled) return
          setMatches(leagueMatches)

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
    setMatches((prev) => prev.map((m) => (m.id === updatedMatch.id ? updatedMatch : m)))
  }

  const pendingCount = matches.filter((m) => m.canPredict && !m.myPrediction).length
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

  return (
    <div className="pdash">
      <div className="pdash__main">
        <DashboardHero
          pendingCount={pendingCount}
          bestPosition={bestPosition}
          totalPoints={totalPoints}
        />

        <div id="proximos-partidos" className="pdash__section">
          <h2 className="pdash__section-title">Próximos partidos</h2>
          {matches.length === 0 ? (
            <div className="pdash__empty-card">
              {leagues.length === 0
                ? 'Unite a una Liga para empezar a pronosticar'
                : 'No hay partidos disponibles en este momento'}
            </div>
          ) : (
            <div className="pdash__matches">
              {matches.map((m) =>
                firstLeagueId ? (
                  <MatchPredictionCard
                    key={m.id}
                    match={m}
                    leagueId={firstLeagueId}
                    onPredictionUpdated={handlePredictionUpdated}
                  />
                ) : null,
              )}
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
