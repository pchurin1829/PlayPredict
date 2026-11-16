// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import type { Edition, Round } from '../api/types'
import RoundFormPage from './RoundFormPage'
import RoundsListPage from './RoundsListPage'

const { apiGet, apiPost, apiPut } = vi.hoisted(() => ({
  apiGet: vi.fn(),
  apiPost: vi.fn(),
  apiPut: vi.fn(),
}))

vi.mock('../api/client', () => {
  class TestApiError extends Error {
    status = 0
    fieldErrors: Record<string, string[]> = {}
  }

  return {
    api: {
      get: apiGet,
      post: apiPost,
      put: apiPut,
      upload: vi.fn(),
      del: vi.fn(),
      download: vi.fn(),
    },
    ApiError: TestApiError,
  }
})

const edition = {
  id: 1,
  competitionId: 1,
  name: 'Clausura 2026',
  startDateUtc: '2026-07-01T00:00:00Z',
  endDateUtc: null,
  status: 'Active',
  createdAtUtc: '2026-07-01T00:00:00Z',
} as Edition

const round = {
  id: 5,
  editionId: 1,
  name: 'Fecha 17',
  order: 17,
  startDateUtc: null,
  endDateUtc: null,
} as Round

function renderRoundForm(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/rounds/:roundId/edit" element={<RoundFormPage />} />
        <Route path="/editions/:editionId/rounds/new" element={<RoundFormPage />} />
        <Route path="/editions/:editionId/rounds" element={<RoundsListPage />} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('round form navigation', () => {
  beforeEach(() => {
    apiGet.mockReset()
    apiPost.mockReset()
    apiPut.mockReset()
    apiGet.mockImplementation((path: string) => {
      if (path === '/rounds/5') return Promise.resolve(round)
      if (path === '/editions/1') return Promise.resolve(edition)
      if (path === '/editions/1/rounds') return Promise.resolve([round])
      return Promise.reject(new Error(`Unexpected request: ${path}`))
    })
  })

  afterEach(cleanup)

  it('saves an edit once and returns to the list with a clear confirmation', async () => {
    let resolvePut!: () => void
    apiPut.mockImplementation(() => new Promise<void>((resolve) => { resolvePut = resolve }))
    renderRoundForm('/rounds/5/edit')

    expect(await screen.findByRole('heading', { name: 'Editar Fecha' })).toBeTruthy()
    const save = screen.getByRole('button', { name: 'Guardar' })
    fireEvent.click(save)
    fireEvent.click(save)

    await waitFor(() => expect(apiPut).toHaveBeenCalledTimes(1))
    expect(apiPut).toHaveBeenCalledWith('/rounds/5', { name: 'Fecha 17', order: 17 })

    await act(async () => { resolvePut() })

    expect(await screen.findByText('Fecha actualizada correctamente.')).toBeTruthy()
    expect(screen.getByRole('heading', { name: /Fixture \/ Partidos/ })).toBeTruthy()
  })

  it('returns to the list with a confirmation after creating a round', async () => {
    apiGet.mockImplementation((path: string) => {
      if (path === '/editions/1') return Promise.resolve(edition)
      if (path === '/editions/1/rounds') return Promise.resolve([])
      return Promise.reject(new Error(`Unexpected request: ${path}`))
    })
    apiPost.mockResolvedValue({ ...round, id: 6, name: 'FECHA 1', order: 1 })
    renderRoundForm('/editions/1/rounds/new')

    expect(await screen.findByRole('heading', { name: 'Nueva Fecha' })).toBeTruthy()
    await waitFor(() => expect((screen.getByLabelText('Nombre') as HTMLInputElement).value).toBe('FECHA 1'))
    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }))

    await waitFor(() => expect(apiPost).toHaveBeenCalledWith('/editions/1/rounds', { name: 'FECHA 1', order: 1 }))
    expect(await screen.findByText('Fecha creada correctamente.')).toBeTruthy()
    expect(screen.getByRole('heading', { name: /Fixture \/ Partidos/ })).toBeTruthy()
  })
})
