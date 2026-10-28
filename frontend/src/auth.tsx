import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api, setUnauthorizedHandler, tokenStore } from './api'
import type { AuthResponse, User } from './types'

interface AuthState {
  user: User | null
  loading: boolean
  isAdmin: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string, name: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  const logout = useCallback(() => {
    tokenStore.clear()
    setUser(null)
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(() => setUser(null))
    if (!tokenStore.get()) {
      setLoading(false)
      return
    }
    api.me()
      .then(setUser)
      .catch(() => tokenStore.clear())
      .finally(() => setLoading(false))
  }, [])

  const accept = (res: AuthResponse) => {
    tokenStore.set(res.token)
    setUser(res.user)
  }

  const value = useMemo<AuthState>(() => ({
    user,
    loading,
    isAdmin: !!user?.roles.includes('Admin'),
    login: async (email, password) => accept(await api.login(email, password)),
    register: async (email, password, name) => accept(await api.register(email, password, name)),
    logout,
  }), [user, loading, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
