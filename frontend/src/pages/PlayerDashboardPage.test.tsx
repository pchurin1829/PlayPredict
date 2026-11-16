// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import type { LeagueDetail, LeagueSummary, MatchWithPrediction } from '../api/types'
import PlayerDashboardPage from './PlayerDashboardPage'

const { apiGet, apiDel } = vi.hoisted(() => ({
  apiGet: vi.fn(),
  apiDel: vi.fn(),
}))

vi.mock('../api/client', () => {
  class TestApiError extends Error {
    status = 0
    fieldErrors: Record<string, string[]> = {}
  }

  return {
    api: {
      get: apiGet,
      post: vi.fn(),
      put: vi.fn(),
      upload: vi.fn(),
      del: apiDel,
      download: vi.fn(),
    },
    ApiError: TestApiError,
  }
})

vi.mock('../auth/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 10, firstName: 'Test', lastName: 'Player', roles: ['PLAYER'] },
    loading: false,
    sessionExpired: false,
    login: vi.fn(),
    logout: vi.fn(),
    updateUser: vi.fn(),
    viewMode: 'player',
    setViewMode: vi.fn(),
  }),
}))

vi.mock('../company/CompanySettingsContext', () => ({
  useCompanySettings: () => ({
    company: { name: 'PlayPredict', shortName: 'PlayPredict' },
    loading: false,
    refreshCompany: vi.fn(),
    updateCompany: vi.fn(),
  }),
}))

const leagueSummary = {
  id: 1,
  name: 'COPA EL NENE',
  description: null,
  competitionId: 1,
  competitionName: 'Torneo Clausura AFA 2026',
  editionId: 1,
  editionName: 'Clausura 2026',
  scopeType: 'FullCompetition',
  leagueType: 'Official',
  sourceLeagueId: null,
  sourceLeagueName: null,
  usesFullSourceScope: false,
  roundFromId: null,
  roundToId: null,
  roundFromName: null,
  roundToName: null,
  createdByUserId: 10,
  isCreator: false,
  participantsCount: 2,
  isActive: true,
  inviteCode: null,
  isParticipant: true,
} as LeagueSummary

const leagueDetail = {
  ...leagueSummary,
  createdByName: 'Test Player',
  rounds: [{ id: 5, name: 'Octavos', order: 1 }],
} as LeagueDetail

const prediction = {
  id: 77,
  matchId: 61,
  userId: 10,
  predictedHomeScore: 1,
  predictedAwayScore: 0,
  preferredPlayerId: null,
  preferredPlayerName: null,
  createdAtUtc: '2026-11-01T12:00:00Z',
  updatedAtUtc: '2026-11-01T12:00:00Z',
  points: null,
  resultPoints: null,
  preferredPlayerPoints: null,
  evaluationType: null,
  evaluationLabel: null,
  officialHomeScore: null,
  officialAwayScore: null,
}

const match = {
  id: 61,
  roundId: 5,
  homeTeamId: 1,
  awayTeamId: 2,
  participantHome: 'Vélez Sarsfield',
  participantAway: 'Belgrano',
  startsAtUtc: '2026-11-20T23:00:00Z',
  status: 'Scheduled',
  homeGoals: null,
  awayGoals: null,
  homePlayers: [],
  awayPlayers: [],
  quickPreferredPlayers: [],
  preferredPlayerEnabled: false,
  predictionEligible: true,
  myPrediction: prediction,
  canPredict: true,
} as MatchWithPrediction

function renderPage() {
  return render(
    <MemoryRouter>
      <PlayerDashboardPage />
    </MemoryRouter>,
  )
}

describe('PlayerDashboardPage delete action', () => {
  beforeEach(() => {
    apiGet.mockReset()
    apiDel.mockReset()
    apiGet.mockImplementation((path: string) => {
      if (path === '/leagues/mine') return Promise.resolve([leagueSummary])
      if (path === '/competitions') return Promise.resolve([])
      if (path === '/rankings/me/league-positions') return Promise.resolve([])
      if (path === '/leagues/1') return Promise.resolve(leagueDetail)
      if (path === '/leagues/1/matches') return Promise.resolve([match])
      if (path === '/competitions/1/editions') return Promise.resolve([])
      return Promise.reject(new Error(`Unexpected request: ${path}`))
    })
  })

  afterEach(cleanup)

  it('deletes a saved prediction from the Inicio card and updates the sections', async () => {
    apiDel.mockResolvedValue(undefined)
    renderPage()

    expect(await screen.findByRole('button', { name: 'Eliminar pronóstico' })).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar pronóstico' }))
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }))

    await waitFor(() => expect(apiDel).toHaveBeenCalledWith('/predictions/77?leagueId=1'))
    await waitFor(() => expect(screen.queryByRole('button', { name: 'Eliminar pronóstico' })).toBeNull())
    expect(await screen.findByText('1 partido pendiente')).toBeTruthy()
  })
})
