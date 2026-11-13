// @vitest-environment jsdom
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { ApiError, SESSION_EXPIRED_EVENT } from '../api/client'
import type { User } from '../api/types'
import LoginPage from '../pages/LoginPage'
import { AuthProvider, RequireAuth } from './AuthContext'
import { setToken } from './token'

const { apiGet, apiPost } = vi.hoisted(() => ({
  apiGet: vi.fn(),
  apiPost: vi.fn(),
}))

vi.mock('../api/client', () => {
  class TestApiError extends Error {
    status: number
    fieldErrors: Record<string, string[]>

    constructor(message: string, status = 0, fieldErrors: Record<string, string[]> = {}) {
      super(message)
      this.status = status
      this.fieldErrors = fieldErrors
    }
  }

  return {
    api: {
      get: apiGet,
      post: apiPost,
      put: vi.fn(),
      upload: vi.fn(),
      del: vi.fn(),
      download: vi.fn(),
    },
    ApiError: TestApiError,
    SESSION_EXPIRED_EVENT: 'playpredict:session-expired',
  }
})

const admin: User = {
  id: 1,
  companyId: 1,
  firstName: 'Admin',
  lastName: 'Test',
  email: 'admin@example.com',
  isActive: true,
  createdAtUtc: '2026-01-01T00:00:00Z',
  lastAccessUtc: null,
  roles: ['ADMIN'],
  mustChangePassword: false,
}

function LocationProbe() {
  const location = useLocation()
  return <div>{`${location.pathname}${location.search}`}</div>
}

function renderApp(initialPath: string) {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/leagues/:leagueId" element={<RequireAuth><LocationProbe /></RequireAuth>} />
          <Route path="/rounds/:roundId/edit" element={<RequireAuth><div>Editor de fecha</div></RequireAuth>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  )
}

describe('session expiry flow', () => {
  beforeEach(() => {
    localStorage.clear()
    apiGet.mockReset()
    apiPost.mockReset()
    apiGet.mockImplementation((path: string) =>
      path === '/users/me' ? Promise.resolve(admin) : new Promise(() => {}),
    )
  })

  afterEach(cleanup)

  it('shows the expired-session message and returns an admin to the original prediction route', async () => {
    setToken('expired-token')
    apiPost.mockResolvedValue({ token: 'fresh-token', user: admin })
    renderApp('/leagues/1?tab=pronosticos&round=5')

    expect(await screen.findByText('/leagues/1?tab=pronosticos&round=5')).toBeTruthy()

    act(() => {
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
    })

    expect(await screen.findByText('Tu sesión venció. Iniciá sesión nuevamente para continuar.')).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Iniciar sesión' })).toBeTruthy()

    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'admin@example.com' } })
    fireEvent.change(screen.getByLabelText('Contraseña'), { target: { value: 'admin123' } })
    fireEvent.click(screen.getByRole('button', { name: 'Ingresar' }))

    await waitFor(() => {
      expect(screen.getByText('/leagues/1?tab=pronosticos&round=5')).toBeTruthy()
    })
  })

  it('keeps the specific login error and hides the expired-session message', async () => {
    apiPost.mockRejectedValue(new ApiError('Email o contraseña incorrectos.', 401))
    renderApp('/login')

    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'admin@example.com' } })
    fireEvent.change(screen.getByLabelText('Contraseña'), { target: { value: 'wrong' } })
    fireEvent.click(screen.getByRole('button', { name: 'Ingresar' }))

    expect(await screen.findByText('Email o contraseña incorrectos.')).toBeTruthy()
    expect(screen.queryByText(/Tu sesión venció/)).toBeNull()
  })

  it('does not redirect in a loop when the expiration event is emitted more than once', async () => {
    setToken('expired-token')
    renderApp('/rounds/5/edit')

    expect(await screen.findByText('Editor de fecha')).toBeTruthy()

    act(() => {
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
      window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT))
    })

    expect(await screen.findByRole('heading', { name: 'Iniciar sesión' })).toBeTruthy()
    expect(screen.getAllByRole('heading', { name: 'Iniciar sesión' })).toHaveLength(1)
  })
})
