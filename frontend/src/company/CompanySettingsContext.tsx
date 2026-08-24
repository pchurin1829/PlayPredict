import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { api } from '../api/client'
import type { CompanySettings } from '../api/types'

const FALLBACK: CompanySettings = { name: 'PlayPredict', shortName: 'PlayPredict', logoUrl: null }

interface CompanySettingsContextValue {
  company: CompanySettings
  loading: boolean
  refreshCompany: () => Promise<void>
  updateCompany: (settings: CompanySettings) => void
}

const CompanySettingsContext = createContext<CompanySettingsContextValue | undefined>(undefined)

export function CompanySettingsProvider({ children }: { children: ReactNode }) {
  const [company, setCompany] = useState(FALLBACK)
  const [loading, setLoading] = useState(true)

  const refreshCompany = useCallback(async () => {
    try {
      setCompany(await api.get<CompanySettings>('/company-settings'))
    } catch {
      setCompany(FALLBACK)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => { void refreshCompany() }, [refreshCompany])

  return (
    <CompanySettingsContext.Provider value={{ company, loading, refreshCompany, updateCompany: setCompany }}>
      {children}
    </CompanySettingsContext.Provider>
  )
}

export function useCompanySettings() {
  const context = useContext(CompanySettingsContext)
  if (!context) throw new Error('useCompanySettings debe usarse dentro de CompanySettingsProvider')
  return context
}
