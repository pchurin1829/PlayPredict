import { describe, expect, it } from 'vitest'
import { formatArgentinaDate, formatArgentinaTime } from './argentinaDateTime'

describe('argentinaDateTime', () => {
  it('shows a UTC kickoff in Argentina time', () => {
    expect(formatArgentinaDate('2026-11-20T23:00:00Z')).toBe('20 nov')
    expect(formatArgentinaTime('2026-11-20T23:00:00Z')).toBe('20:00')
  })

  it('keeps the Argentina calendar day when UTC is already the next day', () => {
    expect(formatArgentinaDate('2026-11-21T01:30:00Z')).toBe('20 nov')
    expect(formatArgentinaTime('2026-11-21T01:30:00Z')).toBe('22:30')
  })
})
