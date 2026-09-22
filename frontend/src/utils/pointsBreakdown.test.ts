import { describe, expect, it } from 'vitest'
import { buildPointsBreakdown } from './pointsBreakdown'

describe('buildPointsBreakdown', () => {
  it('exact score + bonus renders 6 + 2 = total 8', () => {
    expect(
      buildPointsBreakdown({
        points: 8,
        resultPoints: 6,
        preferredPlayerPoints: 2,
        preferredPlayerName: 'Sebastián Driussi',
        evaluationLabel: 'Marcador exacto',
      }),
    ).toEqual([
      { kind: 'result', label: 'Marcador exacto', pointsText: '6 pts' },
      {
        kind: 'bonus',
        label: 'Sebastián Driussi · Jugador Preferido',
        pointsText: '+2 pts',
      },
    ])
  })

  it('correct outcome + bonus renders 3 + 2 = total 5', () => {
    expect(
      buildPointsBreakdown({
        points: 5,
        resultPoints: 3,
        preferredPlayerPoints: 2,
        preferredPlayerName: 'Miguel Merentiel',
        evaluationLabel: 'Resultado correcto',
      }),
    ).toEqual([
      { kind: 'result', label: 'Resultado correcto', pointsText: '3 pts' },
      {
        kind: 'bonus',
        label: 'Miguel Merentiel · Jugador Preferido',
        pointsText: '+2 pts',
      },
    ])
  })

  it('exact score without bonus renders a single 6 pts row', () => {
    expect(
      buildPointsBreakdown({
        points: 6,
        resultPoints: 6,
        preferredPlayerPoints: 0,
        preferredPlayerName: null,
        evaluationLabel: 'Marcador exacto',
      }),
    ).toEqual([{ kind: 'result', label: 'Marcador exacto', pointsText: '6 pts' }])
  })

  it('incorrect outcome with bonus still shows both rows (0 + bonus)', () => {
    expect(
      buildPointsBreakdown({
        points: 2,
        resultPoints: 0,
        preferredPlayerPoints: 2,
        preferredPlayerName: 'Sebastián Driussi',
        evaluationLabel: 'Incorrecto',
      }),
    ).toEqual([
      { kind: 'result', label: 'Incorrecto', pointsText: '0 pts' },
      {
        kind: 'bonus',
        label: 'Sebastián Driussi · Jugador Preferido',
        pointsText: '+2 pts',
      },
    ])
  })

  it('returns null without evaluation instead of inventing a breakdown', () => {
    expect(
      buildPointsBreakdown({
        points: null,
        resultPoints: null,
        preferredPlayerPoints: null,
        preferredPlayerName: null,
        evaluationLabel: null,
      }),
    ).toBeNull()
  })
})
