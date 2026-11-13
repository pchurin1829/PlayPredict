// @vitest-environment jsdom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { api, ApiError, SESSION_EXPIRED_EVENT } from './client'
import { getToken, setToken } from '../auth/token'

function unauthorizedResponse(): Response {
  return {
    ok: false,
    status: 401,
    json: async () => ({}),
  } as Response
}

function listenForSessionExpired(): { listener: () => void; remove: () => void } {
  const listener = vi.fn()
  window.addEventListener(SESSION_EXPIRED_EVENT, listener)
  return {
    listener,
    remove: () => window.removeEventListener(SESSION_EXPIRED_EVENT, listener),
  }
}

describe('api session expiry handling', () => {
  afterEach(() => {
    localStorage.clear()
    vi.unstubAllGlobals()
  })

  it('clears the token and emits one event for a 401 on an authenticated request', async () => {
    setToken('expired-token')
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(unauthorizedResponse()))
    const { listener, remove } = listenForSessionExpired()

    try {
      await expect(api.put('/rounds/5', { name: 'Fecha 17', order: 17 })).rejects.toMatchObject({
        status: 401,
      })
      expect(getToken()).toBeNull()
      expect(listener).toHaveBeenCalledTimes(1)
    } finally {
      remove()
    }
  })

  it('clears the token and emits one event for concurrent authenticated 401 responses', async () => {
    setToken('expired-token')
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(unauthorizedResponse()))
    const { listener, remove } = listenForSessionExpired()

    try {
      await Promise.allSettled([
        api.get('/users/me'),
        api.put('/rounds/5', { name: 'Fecha 17', order: 17 }),
      ])
      expect(getToken()).toBeNull()
      expect(listener).toHaveBeenCalledTimes(1)
    } finally {
      remove()
    }
  })

  it('does not clear the token or emit an event for a login 401', async () => {
    setToken('stale-token')
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(unauthorizedResponse()))
    const { listener, remove } = listenForSessionExpired()

    try {
      await expect(api.post('/auth/login', { email: 'admin@example.com', password: 'wrong' }))
        .rejects.toBeInstanceOf(ApiError)
      expect(getToken()).toBe('stale-token')
      expect(listener).not.toHaveBeenCalled()
    } finally {
      remove()
    }
  })

  it('handles a 401 on an authenticated download', async () => {
    setToken('expired-token')
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(unauthorizedResponse()))
    const { listener, remove } = listenForSessionExpired()

    try {
      await expect(api.download('/editions/1/fixture.csv')).rejects.toMatchObject({
        status: 401,
      })
      expect(getToken()).toBeNull()
      expect(listener).toHaveBeenCalledTimes(1)
    } finally {
      remove()
    }
  })
})
