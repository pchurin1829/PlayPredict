import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { api } from '../api/client'
import type { User } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { clearToken, getToken, setToken } from './token'

interface AuthContextValue {
  user: User | null
  loading: boolean
  login: (token: string, user: User) => void
  logout: () => void
  viewMode: 'admin' | 'player'
  setViewMode: (mode: 'admin' | 'player') => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [viewMode, setViewModeState] = useState<'admin' | 'player'>(() =>
    localStorage.getItem('playpredict_view_mode') === 'player' ? 'player' : 'admin',
  )

  useEffect(() => {
    if (!getToken()) {
      setLoading(false)
      return
    }

    api
      .get<User>('/users/me')
      .then(setUser)
      .catch(() => {
        clearToken()
        setUser(null)
      })
      .finally(() => setLoading(false))
  }, [])

  function login(token: string, loggedUser: User) {
    setToken(token)
    setUser(loggedUser)
    if (loggedUser.roles.includes('ADMIN')) setViewMode('admin')
  }

  function logout() {
    clearToken()
    setUser(null)
    localStorage.removeItem('playpredict_view_mode')
  }

  function setViewMode(mode: 'admin' | 'player') {
    localStorage.setItem('playpredict_view_mode', mode)
    setViewModeState(mode)
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, viewMode, setViewMode }}>
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
  const { user, loading } = useAuth()
  const location = useLocation()

  if (loading) {
    return <StatusMessage kind="loading" message="Verificando sesión..." />
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />
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
