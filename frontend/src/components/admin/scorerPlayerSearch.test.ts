import { describe, expect, it } from 'vitest'
import type { TeamPlayer } from '../../api/types'
import {
  filterScorerPlayers,
  normalizeScorerQuery,
} from './scorerPlayerSearch'

const merentiel: TeamPlayer = {
  id: 267,
  teamId: 7,
  firstName: 'Miguel',
  lastName: 'Merentiel',
  displayName: 'Miguel Merentiel',
  shirtNumber: null,
  position: 'Delantero',
  active: true,
  photoUrl: null,
}

const driussi: TeamPlayer = {
  id: 763,
  teamId: 22,
  firstName: 'Sebastián',
  lastName: 'Driussi',
  displayName: 'Sebastián Driussi',
  shirtNumber: null,
  position: 'Delantero',
  active: true,
  photoUrl: null,
}

const roster = [merentiel, driussi]

describe('normalizeScorerQuery', () => {
  it('lowercases and strips accents', () => {
    expect(normalizeScorerQuery('Sebastián DRiu')).toBe('sebastian driu')
  })
})

describe('filterScorerPlayers', () => {
  it('finds Miguel Merentiel by fragment "Mere"', () => {
    expect(filterScorerPlayers(roster, 'Mere')).toEqual([merentiel])
  })

  it('finds Sebastián Driussi by fragment "Driu"', () => {
    expect(filterScorerPlayers(roster, 'Driu')).toEqual([driussi])
  })

  it('is case-insensitive', () => {
    expect(filterScorerPlayers(roster, 'MERE')).toEqual([merentiel])
    expect(filterScorerPlayers(roster, 'driussi')).toEqual([driussi])
  })

  it('matches first name without accents', () => {
    expect(filterScorerPlayers(roster, 'sebastian')).toEqual([driussi])
    expect(filterScorerPlayers(roster, 'miguel')).toEqual([merentiel])
  })

  it('returns the list unchanged (same order) on empty query', () => {
    expect(filterScorerPlayers(roster, '')).toEqual(roster)
    expect(filterScorerPlayers(roster, '   ')).toEqual(roster)
  })

  it('returns an empty list when nothing matches', () => {
    expect(filterScorerPlayers(roster, 'zzz')).toEqual([])
  })
})
