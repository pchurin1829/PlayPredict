// @vitest-environment jsdom
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { Prediction } from '../../api/types'
import PredictionPointsBreakdown from './PredictionPointsBreakdown'

function prediction(overrides: Partial<Prediction>): Prediction {
  return {
    id: 1,
    matchId: 55,
    userId: 9,
    predictedHomeScore: 2,
    predictedAwayScore: 0,
    preferredPlayerId: 267,
    preferredPlayerName: 'Miguel Merentiel',
    createdAtUtc: '2026-09-22T20:31:03.259311Z',
    updatedAtUtc: '2026-09-22T20:31:03.259311Z',
    points: 5,
    resultPoints: 3,
    preferredPlayerPoints: 2,
    evaluationType: 'CorrectOutcome',
    evaluationLabel: 'Resultado correcto',
    officialHomeScore: 1,
    officialAwayScore: 0,
    ...overrides,
  }
}

describe('PredictionPointsBreakdown (Copa → Resultados)', () => {
  it('renders the 3 + 2 breakdown for Boca (5 pts)', () => {
    render(<PredictionPointsBreakdown prediction={prediction({})} />)

    expect(screen.getByText('Resultado correcto · 3 pts')).toBeTruthy()
    expect(screen.getByText('Miguel Merentiel · +2 pts')).toBeTruthy()
    expect(screen.getByText('Jugador Preferido')).toBeTruthy()
    // The old ambiguous single row must not be the only content anymore.
    expect(screen.queryByText('Resultado correcto', { exact: true })).toBeNull()
  })

  it('renders the 6 + 2 breakdown for River (8 pts)', () => {
    render(
      <PredictionPointsBreakdown
        prediction={prediction({
          matchId: 36,
          predictedHomeScore: 1,
          preferredPlayerId: 763,
          preferredPlayerName: 'Sebastián Driussi',
          points: 8,
          resultPoints: 6,
          evaluationType: 'ExactScore',
          evaluationLabel: 'Marcador exacto',
        })}
      />,
    )

    expect(screen.getByText('Marcador exacto · 6 pts')).toBeTruthy()
    expect(screen.getByText('Sebastián Driussi · +2 pts')).toBeTruthy()
  })

  it('renders nothing without evaluation', () => {
    const { container } = render(
      <PredictionPointsBreakdown
        prediction={prediction({
          points: null,
          resultPoints: null,
          preferredPlayerPoints: null,
          evaluationType: null,
          evaluationLabel: null,
        })}
      />,
    )

    expect(container.textContent).toBe('')
  })
})
