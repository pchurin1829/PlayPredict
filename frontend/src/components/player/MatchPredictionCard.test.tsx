// @vitest-environment jsdom
import { useState } from 'react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import type { MatchWithPrediction } from '../../api/types'
import { ApiError } from '../../api/client'
import MatchPredictionCard from './MatchPredictionCard'

const { apiDel } = vi.hoisted(() => ({
  apiDel: vi.fn(),
}))

vi.mock('../../api/client', () => {
  class TestApiError extends Error {
    status = 0
    fieldErrors: Record<string, string[]> = {}
  }

  return {
    api: {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
      upload: vi.fn(),
      del: apiDel,
      download: vi.fn(),
    },
    ApiError: TestApiError,
  }
})

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

function CardHarness({ onUpdated, initialMatch = match }: {
  onUpdated: (updated: MatchWithPrediction) => void
  initialMatch?: MatchWithPrediction
}) {
  const [currentMatch, setCurrentMatch] = useState(initialMatch)
  return (
    <MatchPredictionCard
      match={currentMatch}
      leagueId={1}
      onPredictionUpdated={(updated) => {
        setCurrentMatch(updated)
        onUpdated(updated)
      }}
    />
  )
}

describe('MatchPredictionCard delete action', () => {
  beforeEach(() => {
    apiDel.mockReset()
  })

  afterEach(cleanup)

  it('shows delete for a saved prediction and removes it after confirmation', async () => {
    const onUpdated = vi.fn()
    apiDel.mockResolvedValue(undefined)
    render(<CardHarness onUpdated={onUpdated} />)

    expect(screen.getByRole('button', { name: 'Actualizar' })).toBeTruthy()
    const deleteButton = screen.getByRole('button', { name: 'Eliminar pronóstico' })
    expect(deleteButton.getAttribute('title')).toBe('Eliminar pronóstico')
    expect(deleteButton.querySelector('svg')).toBeTruthy()
    expect(screen.queryByText('Eliminar pronóstico')).toBeNull()

    fireEvent.click(deleteButton)
    expect(screen.getByRole('heading', { name: 'Eliminar pronóstico' })).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }))

    await waitFor(() => expect(apiDel).toHaveBeenCalledWith('/predictions/77?leagueId=1'))
    await waitFor(() => expect(onUpdated).toHaveBeenCalledWith(expect.objectContaining({ myPrediction: null })))
    expect(screen.queryByRole('button', { name: 'Eliminar pronóstico' })).toBeNull()
    expect((screen.getByLabelText('Goles Vélez Sarsfield') as HTMLInputElement).value).toBe('')
    expect((screen.getByLabelText('Goles Belgrano') as HTMLInputElement).value).toBe('')
    expect(screen.getByRole('button', { name: '¡Pronosticá!' })).toBeTruthy()
  })

  it('does not delete anything when the confirmation is cancelled', () => {
    const onUpdated = vi.fn()
    render(<CardHarness onUpdated={onUpdated} />)

    fireEvent.click(screen.getByRole('button', { name: 'Eliminar pronóstico' }))
    fireEvent.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(apiDel).not.toHaveBeenCalled()
    expect(onUpdated).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Eliminar pronóstico' })).toBeTruthy()
  })

  it('keeps the prediction visible when the API fails', async () => {
    const onUpdated = vi.fn()
    apiDel.mockRejectedValue(new ApiError('No se pudo eliminar el pronóstico.', 409))
    render(<CardHarness onUpdated={onUpdated} />)

    fireEvent.click(screen.getByRole('button', { name: 'Eliminar pronóstico' }))
    fireEvent.click(screen.getByRole('button', { name: 'Eliminar' }))

    expect(await screen.findByText('No se pudo eliminar el pronóstico.')).toBeTruthy()
    expect(onUpdated).not.toHaveBeenCalled()
    expect(screen.getByRole('button', { name: 'Actualizar' })).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Eliminar pronóstico' })).toBeTruthy()
  })

  it('does not show delete for a closed match', () => {
    const closedMatch = { ...match, canPredict: false }
    render(<CardHarness onUpdated={vi.fn()} initialMatch={closedMatch} />)

    expect(screen.getByText(/Pronóstico cerrado/)).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Eliminar pronóstico' })).toBeNull()
  })

  it('does not show delete when there is no prediction', () => {
    const pendingMatch = { ...match, myPrediction: null }
    render(<CardHarness onUpdated={vi.fn()} initialMatch={pendingMatch} />)

    expect(screen.queryByRole('button', { name: 'Eliminar pronóstico' })).toBeNull()
  })

  it('does not send duplicate DELETE requests while deleting', async () => {
    const onUpdated = vi.fn()
    let resolveDelete: (value: void) => void = () => undefined
    apiDel.mockReturnValue(new Promise<void>((resolve) => {
      resolveDelete = resolve
    }))
    render(<CardHarness onUpdated={onUpdated} />)

    fireEvent.click(screen.getByRole('button', { name: 'Eliminar pronóstico' }))
    const confirmButton = screen.getByRole('button', { name: 'Eliminar' })
    fireEvent.click(confirmButton)
    fireEvent.click(confirmButton)

    await waitFor(() => expect(apiDel).toHaveBeenCalledTimes(1))
    resolveDelete()
  })
})
