import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import type { CompanySettings } from '../api/types'
import { useCompanySettings } from '../company/CompanySettingsContext'
import StatusMessage from '../components/StatusMessage'
import ImageUploadField from '../components/admin/ImageUploadField'

export default function AdminSettingsPage() {
  const { company, loading, updateCompany } = useCompanySettings()
  const [name, setName] = useState(company.name)
  const [shortName, setShortName] = useState(company.shortName)
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [removeLogo, setRemoveLogo] = useState(false)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  useEffect(() => {
    setName(company.name); setShortName(company.shortName)
  }, [company])

  async function handleSubmit(event: FormEvent) {
    event.preventDefault(); setSaving(true); setSaved(false); setError(null); setFieldErrors({})
    try {
      let updated = await api.put<CompanySettings>('/company-settings', { name, shortName, logoUrl: company.logoUrl })
      if (logoFile) { const form = new FormData(); form.append('file', logoFile); updated = await api.upload<CompanySettings>('/company-settings/logo', form) }
      else if (removeLogo) updated = await api.del<CompanySettings>('/company-settings/logo')
      updateCompany(updated); setLogoFile(null); setRemoveLogo(false); setSaved(true)
    } catch (reason) {
      if (reason instanceof ApiError) { setError(reason.message); setFieldErrors(reason.fieldErrors) }
      else setError('No se pudo guardar la configuración de empresa.')
    } finally { setSaving(false) }
  }

  if (loading) return <StatusMessage kind="loading" message="Cargando configuración..." />

  return (
    <div>
      <div className="admin-header"><div><h1>Configuración</h1><p className="admin-help">Identidad del cliente y reglas generales de PlayPredict.</p></div></div>
      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Configuración de empresa guardada." />}
      <form className="form-card" onSubmit={handleSubmit}>
        <div><span className="admin-eyebrow">EMPRESA</span><h2>Identidad del cliente</h2></div>
        <div className="form-field"><label htmlFor="companyName">Nombre de empresa</label><input id="companyName" value={name} onChange={(e) => setName(e.target.value)} required />{fieldErrors.name && <span className="form-field-error">{fieldErrors.name[0]}</span>}</div>
        <div className="form-field"><label htmlFor="companyShortName">Nombre corto</label><input id="companyShortName" value={shortName} onChange={(e) => setShortName(e.target.value)} required /><span className="form-field-hint">Se utiliza en títulos como “Competencias {shortName || 'PlayPredict'}”.</span>{fieldErrors.shortName && <span className="form-field-error">{fieldErrors.shortName[0]}</span>}</div>
        <ImageUploadField label="Logo de empresa (opcional)" currentUrl={company.logoUrl} fallback={shortName.slice(0, 2).toUpperCase() || 'PP'} onSelectionChange={(file, remove) => { setLogoFile(file); setRemoveLogo(remove) }} onError={setError} />
        <div className="form-actions"><button className="btn btn-primary" disabled={saving}>{saving ? 'Guardando...' : 'Guardar empresa'}</button><Link className="btn btn-secondary" to="/admin/scoring">Configurar scoring</Link></div>
      </form>
    </div>
  )
}
