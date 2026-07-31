import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import { RequireAdmin, RequireAuth } from './auth/AuthContext'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ProfilePage from './pages/ProfilePage'
import AdminUsersPage from './pages/AdminUsersPage'
import CompetitionsListPage from './pages/CompetitionsListPage'
import CompetitionFormPage from './pages/CompetitionFormPage'
import EditionsListPage from './pages/EditionsListPage'
import EditionFormPage from './pages/EditionFormPage'
import EditionScoringConfigurationPage from './pages/EditionScoringConfigurationPage'
import RoundsListPage from './pages/RoundsListPage'
import RoundFormPage from './pages/RoundFormPage'
import MatchesListPage from './pages/MatchesListPage'
import MatchFormPage from './pages/MatchFormPage'
import PredictionsCompetitionsPage from './pages/PredictionsCompetitionsPage'
import PredictionsEditionsPage from './pages/PredictionsEditionsPage'
import PredictionsRoundsPage from './pages/PredictionsRoundsPage'
import PredictionsMatchesPage from './pages/PredictionsMatchesPage'
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
import './components/admin.css'

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
        <Route path="/" element={<Navigate to="/competitions" replace />} />

        <Route path="/profile" element={<ProfilePage />} />
        <Route
          path="/admin/users"
          element={
            <RequireAdmin>
              <AdminUsersPage />
            </RequireAdmin>
          }
        />

        <Route path="/competitions" element={<CompetitionsListPage />} />
        <Route path="/competitions/new" element={<CompetitionFormPage />} />
        <Route path="/competitions/:competitionId/edit" element={<CompetitionFormPage />} />
        <Route path="/competitions/:competitionId/editions" element={<EditionsListPage />} />
        <Route
          path="/competitions/:competitionId/editions/new"
          element={<EditionFormPage />}
        />

        <Route path="/editions/:editionId/edit" element={<EditionFormPage />} />
        <Route
          path="/editions/:editionId/scoring-configuration"
          element={
            <RequireAdmin>
              <EditionScoringConfigurationPage />
            </RequireAdmin>
          }
        />
        <Route path="/editions/:editionId/rounds" element={<RoundsListPage />} />
        <Route path="/editions/:editionId/rounds/new" element={<RoundFormPage />} />

        <Route path="/rounds/:roundId/edit" element={<RoundFormPage />} />
        <Route path="/rounds/:roundId/matches" element={<MatchesListPage />} />
        <Route path="/rounds/:roundId/matches/new" element={<MatchFormPage />} />

        <Route path="/matches/:matchId/edit" element={<MatchFormPage />} />

        <Route path="/predictions" element={<PredictionsCompetitionsPage />} />
        <Route
          path="/predictions/competitions/:competitionId/editions"
          element={<PredictionsEditionsPage />}
        />
        <Route path="/predictions/editions/:editionId/rounds" element={<PredictionsRoundsPage />} />
        <Route path="/predictions/rounds/:roundId" element={<PredictionsMatchesPage />} />

        <Route path="/rankings" element={<RankingsCompetitionsPage />} />
        <Route
          path="/rankings/competitions/:competitionId/editions"
          element={<RankingsEditionsPage />}
        />
        <Route path="/rankings/editions/:editionId" element={<RankingGeneralPage />} />
        <Route path="/rankings/editions/:editionId/rounds" element={<RankingsRoundsPage />} />
        <Route path="/rankings/rounds/:roundId" element={<RankingRoundPage />} />

        <Route path="/prizes" element={<PrizesCompetitionsPage />} />
        <Route
          path="/prizes/competitions/:competitionId/editions"
          element={<PrizesEditionsPage />}
        />
        <Route path="/prizes/editions/:editionId" element={<PrizesListPage />} />

        <Route
          path="/admin/prizes"
          element={
            <RequireAdmin>
              <AdminPrizesListPage />
            </RequireAdmin>
          }
        />
        <Route
          path="/admin/prizes/new"
          element={
            <RequireAdmin>
              <AdminPrizeFormPage />
            </RequireAdmin>
          }
        />
        <Route
          path="/admin/prizes/:prizeId/edit"
          element={
            <RequireAdmin>
              <AdminPrizeFormPage />
            </RequireAdmin>
          }
        />

        <Route path="*" element={<Navigate to="/competitions" replace />} />
      </Route>
    </Routes>
  )
}

export default App
