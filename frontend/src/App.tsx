import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import { RequireAdmin, RequireAuth, useAuth } from './auth/AuthContext'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ProfilePage from './pages/ProfilePage'
import AdminUsersPage from './pages/AdminUsersPage'
import CompetitionsListPage from './pages/CompetitionsListPage'
import CompetitionFormPage from './pages/CompetitionFormPage'
import ExploreCompetitionsPage from './pages/ExploreCompetitionsPage'
import CompetitionDetailPage from './pages/CompetitionDetailPage'
import EditionsListPage from './pages/EditionsListPage'
import EditionFormPage from './pages/EditionFormPage'
import EditionScoringConfigurationPage from './pages/EditionScoringConfigurationPage'
import RoundsListPage from './pages/RoundsListPage'
import RoundFormPage from './pages/RoundFormPage'
import MatchesListPage from './pages/MatchesListPage'
import MatchFormPage from './pages/MatchFormPage'
import PredictionsMatchesPage from './pages/PredictionsMatchesPage'
import LeaguesMinePage from './pages/LeaguesMinePage'
import LeagueCreatePage from './pages/LeagueCreatePage'
import LeagueJoinPage from './pages/LeagueJoinPage'
import LeagueDetailPage from './pages/LeagueDetailPage'
import RankingsCompetitionsPage from './pages/RankingsCompetitionsPage'
import RankingsEditionsPage from './pages/RankingsEditionsPage'
import RankingsRoundsPage from './pages/RankingsRoundsPage'
import RankingGeneralPage from './pages/RankingGeneralPage'
import RankingRoundPage from './pages/RankingRoundPage'
import AdminPrizesListPage from './pages/AdminPrizesListPage'
import AdminPrizeFormPage from './pages/AdminPrizeFormPage'
import PrizesCompetitionsPage from './pages/PrizesCompetitionsPage'
import PrizesEditionsPage from './pages/PrizesEditionsPage'
import PrizesListPage from './pages/PrizesListPage'
import AdminExperiencesListPage from './pages/AdminExperiencesListPage'
import AdminExperienceFormPage from './pages/AdminExperienceFormPage'
import PlayerDashboardPage from './pages/PlayerDashboardPage'
import AdminOfficialLeaguesListPage from './pages/AdminOfficialLeaguesListPage'
import AdminOfficialLeagueFormPage from './pages/AdminOfficialLeagueFormPage'
import AdminDashboardPage from './pages/AdminDashboardPage'
import AdminOperationEntryPage from './pages/AdminOperationEntryPage'
import TeamsListPage from './pages/TeamsListPage'
import TeamFormPage from './pages/TeamFormPage'
import './components/admin.css'
import './components/player/ComingSoonBadge.css'

function HomeRedirect() {
  const { user } = useAuth()
  const isAdmin = user?.roles.includes('ADMIN') ?? false
  return <Navigate to={isAdmin ? '/admin' : '/'} replace />
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route
        element={
          <RequireAuth>
            <Layout />
          </RequireAuth>
        }
      >
        {/* PLAYER ROUTES */}
        <Route path="/" element={<PlayerDashboardPage />} />
        <Route path="/leagues" element={<LeaguesMinePage />} />
        <Route path="/leagues/new" element={<LeagueCreatePage />} />
        <Route path="/leagues/join" element={<LeagueJoinPage />} />
        <Route path="/leagues/:leagueId" element={<LeagueDetailPage />} />
        <Route path="/leagues/:leagueId/matches" element={<PredictionsMatchesPage />} />
        <Route path="/competitions/explore" element={<ExploreCompetitionsPage />} />
        <Route path="/competitions/:competitionId" element={<CompetitionDetailPage />} />
        <Route path="/rankings" element={<RankingsCompetitionsPage />} />
        <Route path="/rankings/competitions/:competitionId/editions" element={<RankingsEditionsPage />} />
        <Route path="/rankings/editions/:editionId" element={<RankingGeneralPage />} />
        <Route path="/rankings/editions/:editionId/rounds" element={<RankingsRoundsPage />} />
        <Route path="/rankings/rounds/:roundId" element={<RankingRoundPage />} />
        <Route path="/prizes" element={<PrizesCompetitionsPage />} />
        <Route path="/prizes/competitions/:competitionId/editions" element={<PrizesEditionsPage />} />
        <Route path="/prizes/editions/:editionId" element={<PrizesListPage />} />
        <Route path="/profile" element={<ProfilePage />} />

        {/* ADMIN ROUTES */}
        <Route path="/admin" element={<RequireAdmin><AdminDashboardPage /></RequireAdmin>} />
        <Route path="/admin/fixture" element={<RequireAdmin><AdminOperationEntryPage operation="fixture" /></RequireAdmin>} />
        <Route path="/admin/results" element={<RequireAdmin><AdminOperationEntryPage operation="results" /></RequireAdmin>} />
        <Route path="/admin/scoring" element={<RequireAdmin><AdminOperationEntryPage operation="scoring" /></RequireAdmin>} />
        <Route path="/admin/users" element={<RequireAdmin><AdminUsersPage /></RequireAdmin>} />
        <Route path="/admin/teams" element={<RequireAdmin><TeamsListPage /></RequireAdmin>} />
        <Route path="/admin/teams/new" element={<RequireAdmin><TeamFormPage /></RequireAdmin>} />
        <Route path="/admin/teams/:teamId/edit" element={<RequireAdmin><TeamFormPage /></RequireAdmin>} />
        <Route path="/competitions" element={<RequireAdmin><CompetitionsListPage /></RequireAdmin>} />
        <Route path="/competitions/new" element={<RequireAdmin><CompetitionFormPage /></RequireAdmin>} />
        <Route path="/competitions/:competitionId/edit" element={<RequireAdmin><CompetitionFormPage /></RequireAdmin>} />
        <Route path="/competitions/:competitionId/editions" element={<RequireAdmin><EditionsListPage /></RequireAdmin>} />
        <Route path="/competitions/:competitionId/editions/new" element={<RequireAdmin><EditionFormPage /></RequireAdmin>} />
        <Route path="/editions/:editionId/edit" element={<RequireAdmin><EditionFormPage /></RequireAdmin>} />
        <Route path="/editions/:editionId/scoring-configuration" element={<RequireAdmin><EditionScoringConfigurationPage /></RequireAdmin>} />
        <Route path="/editions/:editionId/rounds" element={<RequireAdmin><RoundsListPage /></RequireAdmin>} />
        <Route path="/editions/:editionId/rounds/new" element={<RequireAdmin><RoundFormPage /></RequireAdmin>} />
        <Route path="/rounds/:roundId/edit" element={<RequireAdmin><RoundFormPage /></RequireAdmin>} />
        <Route path="/rounds/:roundId/matches" element={<RequireAdmin><MatchesListPage /></RequireAdmin>} />
        <Route path="/rounds/:roundId/matches/new" element={<RequireAdmin><MatchFormPage /></RequireAdmin>} />
        <Route path="/matches/:matchId/edit" element={<RequireAdmin><MatchFormPage /></RequireAdmin>} />
        <Route path="/admin/prizes" element={<RequireAdmin><AdminPrizesListPage /></RequireAdmin>} />
        <Route path="/admin/prizes/new" element={<RequireAdmin><AdminPrizeFormPage /></RequireAdmin>} />
        <Route path="/admin/prizes/:prizeId/edit" element={<RequireAdmin><AdminPrizeFormPage /></RequireAdmin>} />
        <Route path="/admin/experiences" element={<RequireAdmin><AdminExperiencesListPage /></RequireAdmin>} />
        <Route path="/admin/experiences/new" element={<RequireAdmin><AdminExperienceFormPage /></RequireAdmin>} />
        <Route path="/admin/experiences/:experienceId/edit" element={<RequireAdmin><AdminExperienceFormPage /></RequireAdmin>} />
        <Route path="/admin/official-leagues" element={<RequireAdmin><AdminOfficialLeaguesListPage /></RequireAdmin>} />
        <Route path="/admin/official-leagues/new" element={<RequireAdmin><AdminOfficialLeagueFormPage /></RequireAdmin>} />
        <Route path="/admin/official-leagues/:leagueId/edit" element={<RequireAdmin><AdminOfficialLeagueFormPage /></RequireAdmin>} />
      </Route>

      <Route path="*" element={<HomeRedirect />} />
    </Routes>
  )
}

export default App
