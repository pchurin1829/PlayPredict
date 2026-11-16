// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import type { LeagueDetail, MatchWithPrediction } from '../api/types'
import PredictionsMatchesPage from './PredictionsMatchesPage'

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

const league = {
  id: 1,
  name: 'COPA EL NENE',
  competitionName: 'Torneo Clausura AFA 2026',
} as LeagueDetail

const prediction = {
  id: 77,
  matchId: 61,
  userId: 10,
  predictedHomeScore: 2,
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

describe('PredictionsMatchesPage', () => {
  beforeEach(() => {
    apiGet.mockReset()
    apiDel.mockReset()
    apiGet.mockImplementation((path: string) => {
      if (path === '/leagues/1') return Promise.resolve(league)
      if (path === '/leagues/1/matches') return Promise.resolve([match])
      return Promise.reject(new Error(`Unexpected request: ${path}`))
    })
  })

  afterEach(cleanup)

  it('shows an explicit delete action and resets the card after deleting', async () => {
    apiDel.mockResolvedValue(undefined)
    render(
      <MemoryRouter initialEntries={['/leagues/1/predictions']}>
        <Routes>
          <Route path="/leagues/:leagueId/predictions" element={<PredictionsMatchesPage />} />
        </Routes>
      </MemoryRouter>,
    )

    expect(await screen.findByRole('heading', { name: 'Pronósticos' })).toBeTruthy()
    expect(screen.getByText('20 nov')).toBeTruthy()
    expect(screen.getByText('20:00')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Pronosticado' })).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: 'Eliminar pronóstico' }))
    expect(screen.getByRole('heading', { name: 'Eliminar pronóstico' })).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }))

    await waitFor(() => expect(apiDel).toHaveBeenCalledWith('/predictions/77?leagueId=1'))
    expect(await screen.findByText('Pronóstico eliminado correctamente.')).toBeTruthy()
    expect((screen.getByLabelText('Goles Vélez Sarsfield') as HTMLInputElement).value).toBe('')
    expect((screen.getByLabelText('Goles Belgrano') as HTMLInputElement).value).toBe('')
    expect(screen.getByRole('button', { name: '¡Pronosticá!' })).toBeTruthy()
  })
})
