import { Navigate, Route, Routes } from 'react-router-dom'
import Layout from './components/Layout'
import CompetitionsListPage from './pages/CompetitionsListPage'
import CompetitionFormPage from './pages/CompetitionFormPage'
import EditionsListPage from './pages/EditionsListPage'
import EditionFormPage from './pages/EditionFormPage'
import RoundsListPage from './pages/RoundsListPage'
import RoundFormPage from './pages/RoundFormPage'
import MatchesListPage from './pages/MatchesListPage'
import MatchFormPage from './pages/MatchFormPage'
import './components/admin.css'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<Navigate to="/competitions" replace />} />

        <Route path="/competitions" element={<CompetitionsListPage />} />
        <Route path="/competitions/new" element={<CompetitionFormPage />} />
        <Route path="/competitions/:competitionId/edit" element={<CompetitionFormPage />} />
        <Route path="/competitions/:competitionId/editions" element={<EditionsListPage />} />
        <Route
          path="/competitions/:competitionId/editions/new"
          element={<EditionFormPage />}
        />

        <Route path="/editions/:editionId/edit" element={<EditionFormPage />} />
        <Route path="/editions/:editionId/rounds" element={<RoundsListPage />} />
        <Route path="/editions/:editionId/rounds/new" element={<RoundFormPage />} />

        <Route path="/rounds/:roundId/edit" element={<RoundFormPage />} />
        <Route path="/rounds/:roundId/matches" element={<MatchesListPage />} />
        <Route path="/rounds/:roundId/matches/new" element={<MatchFormPage />} />

        <Route path="/matches/:matchId/edit" element={<MatchFormPage />} />

        <Route path="*" element={<Navigate to="/competitions" replace />} />
      </Route>
    </Routes>
  )
}

export default App
