import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { api, ApiError, SESSION_EXPIRED_EVENT } from '../api/client'
import type { User } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { clearToken, getToken, setToken } from './token'
import { clearWelcomeCampaignSeenForSession } from '../utils/welcomeCampaignSeen'

interface AuthContextValue {
  user: User | null
  loading: boolean
  sessionExpired: boolean
  login: (token: string, user: User) => void
  logout: () => void
  updateUser: (user: User, token?: string) => void
  viewMode: 'admin' | 'player'
  setViewMode: (mode: 'admin' | 'player') => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [sessionExpired, setSessionExpired] = useState(false)
  const [viewMode, setViewModeState] = useState<'admin' | 'player'>(() =>
    localStorage.getItem('playpredict_view_mode') === 'player' ? 'player' : 'admin',
  )

  useEffect(() => {
    function handleSessionExpired() {
      clearToken()
      setUser(null)
      setSessionExpired(true)
      setLoading(false)
    }

    window.addEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired)
    return () => window.removeEventListener(SESSION_EXPIRED_EVENT, handleSessionExpired)
  }, [])

  useEffect(() => {
    if (!getToken()) {
      setLoading(false)
      return
    }

    api
      .get<User>('/users/me')
      .then(setUser)
      .catch((error) => {
        clearToken()
        setUser(null)
        if (error instanceof ApiError && error.status === 401) {
          setSessionExpired(true)
        }
      })
      .finally(() => setLoading(false))
  }, [])

  function login(token: string, loggedUser: User) {
    setToken(token)
    setUser(loggedUser)
    setSessionExpired(false)
    if (loggedUser.roles.includes('ADMIN')) setViewMode('admin')
  }

  function logout() {
    clearToken()
    setUser(null)
    setSessionExpired(false)
    localStorage.removeItem('playpredict_view_mode')
    clearWelcomeCampaignSeenForSession()
  }

  function updateUser(updated: User, token?: string) {
    if (token) setToken(token)
    setUser(updated)
  }

  function setViewMode(mode: 'admin' | 'player') {
    localStorage.setItem('playpredict_view_mode', mode)
    setViewModeState(mode)
  }

  return (
    <AuthContext.Provider value={{ user, loading, sessionExpired, login, logout, updateUser, viewMode, setViewMode }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth debe usarse dentro de AuthProvider')
  }
  return ctx
}

export function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading, sessionExpired } = useAuth()
  const location = useLocation()

  if (loading) {
    return <StatusMessage kind="loading" message="Verificando sesión..." />
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location, sessionExpired }} />
  }

  if (user.mustChangePassword && location.pathname !== '/profile') {
    return <Navigate to="/profile" replace />
  }

  return <>{children}</>
}

export function RequireAdmin({ children }: { children: ReactNode }) {
  const { user } = useAuth()

  if (!user?.roles.includes('ADMIN')) {
    return <Navigate to="/leagues" replace />
  }

  return <>{children}</>
}
