// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import type { LeagueDetail, MatchWithPrediction } from '../api/types'
import { ApiError } from '../api/client'
import LeagueDetailPage from './LeagueDetailPage'

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

const league = {
  id: 1,
  name: 'COPA EL NENE',
  description: 'Liga de prueba',
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
    <MemoryRouter initialEntries={['/leagues/1?tab=pronosticos&round=5']}>
      <Routes>
        <Route path="/leagues/:leagueId" element={<LeagueDetailPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('LeagueDetailPage delete action', () => {
  beforeEach(() => {
    apiGet.mockReset()
    apiDel.mockReset()
    apiGet.mockImplementation((path: string) => {
      if (path === '/leagues/1') return Promise.resolve(league)
      if (path === '/leagues/1/participants') return Promise.resolve([])
      if (path === '/leagues/1/matches') return Promise.resolve([match])
      return Promise.reject(new Error(`Unexpected request: ${path}`))
    })
  })

  afterEach(cleanup)

  it('shows delete for a saved prediction even when it has no changes', async () => {
    renderPage()

    expect(await screen.findByRole('button', { name: 'Pronosticado' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Eliminar pronóstico' })).toBeTruthy()
  })

  it('removes the saved prediction after confirmation', async () => {
    apiDel.mockResolvedValue(undefined)
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar pronóstico' }))
    expect(screen.getByRole('heading', { name: 'Eliminar pronóstico' })).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }))

    await waitFor(() => expect(apiDel).toHaveBeenCalledWith('/predictions/77?leagueId=1'))
    expect(await screen.findByText('Pronóstico eliminado correctamente.')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Eliminar pronóstico' })).toBeNull()
    expect((screen.getByLabelText('Goles Vélez Sarsfield') as HTMLInputElement).value).toBe('')
    expect((screen.getByLabelText('Goles Belgrano') as HTMLInputElement).value).toBe('')
  })

  it('does not delete anything when the confirmation is cancelled', async () => {
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar pronóstico' }))
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(apiDel).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Eliminar pronóstico' })).toBeTruthy()
  })

  it('keeps the prediction visible when the API fails', async () => {
    apiDel.mockRejectedValue(new ApiError('No se pudo eliminar el pronóstico.', 409))
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar pronóstico' }))
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }))

    expect(await screen.findByText('No se pudo eliminar el pronóstico.')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Eliminar pronóstico' })).toBeTruthy()
  })

  it('does not allow deletion when the match is closed', async () => {
    apiGet.mockImplementation((path: string) => {
      if (path === '/leagues/1') return Promise.resolve(league)
      if (path === '/leagues/1/participants') return Promise.resolve([])
      if (path === '/leagues/1/matches') return Promise.resolve([{ ...match, canPredict: false }])
      return Promise.reject(new Error(`Unexpected request: ${path}`))
    })
    renderPage()

    expect(await screen.findByText('Pronóstico cerrado')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Eliminar pronóstico' })).toBeNull()
  })

  it('does not show delete when there is no saved prediction', async () => {
    apiGet.mockImplementation((path: string) => {
      if (path === '/leagues/1') return Promise.resolve(league)
      if (path === '/leagues/1/participants') return Promise.resolve([])
      if (path === '/leagues/1/matches') return Promise.resolve([{ ...match, myPrediction: null }])
      return Promise.reject(new Error(`Unexpected request: ${path}`))
    })
    renderPage()

    expect(await screen.findByRole('button', { name: '¡Pronosticá!' })).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Eliminar pronóstico' })).toBeNull()
  })

  it('does not send duplicate DELETE requests while deleting', async () => {
    let resolveDelete: (value: void) => void = () => undefined
    apiDel.mockReturnValue(new Promise<void>((resolve) => {
      resolveDelete = resolve
    }))
    renderPage()

    fireEvent.click(await screen.findByRole('button', { name: 'Eliminar pronóstico' }))
    const confirmButton = screen.getByRole('button', { name: 'Eliminar' })
    fireEvent.click(confirmButton)
    fireEvent.click(confirmButton)

    await waitFor(() => expect(apiDel).toHaveBeenCalledTimes(1))
    resolveDelete()
  })
})
