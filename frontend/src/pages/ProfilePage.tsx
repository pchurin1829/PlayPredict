import { useEffect, useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client'
import type { PreferredPlayerProfileTeam, User, UserTeamPreferredPlayer } from '../api/types'
import { useAuth } from '../auth/AuthContext'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

export default function ProfilePage() {
  const { user } = useAuth()

  const [firstName, setFirstName] = useState(user?.firstName ?? '')
  const [lastName, setLastName] = useState(user?.lastName ?? '')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [saved, setSaved] = useState(false)
  const [preferredTeams, setPreferredTeams] = useState<PreferredPlayerProfileTeam[] | null>(null)
  const [preferenceError, setPreferenceError] = useState<string | null>(null)
  const [preferenceMessage, setPreferenceMessage] = useState<string | null>(null)
  const [savingTeamId, setSavingTeamId] = useState<number | null>(null)

  useEffect(() => {
    api.get<PreferredPlayerProfileTeam[]>('/users/me/team-preferred-players/options')
      .then(setPreferredTeams)
      .catch((reason) => setPreferenceError(reason instanceof ApiError ? reason.message : 'No se pudieron cargar los jugadores preferidos.'))
  }, [])

  if (!user) {
    return null
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    setSaved(false)

    try {
      await api.put<User>('/users/me', { firstName, lastName })
      setSaved(true)
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
        setFieldErrors(err.fieldErrors)
      } else {
        setError('Ocurrió un error inesperado al guardar.')
      }
    } finally {
      setSaving(false)
    }
  }

  async function changePreference(team: PreferredPlayerProfileTeam, value: string) {
    setSavingTeamId(team.teamId)
    setPreferenceError(null)
    setPreferenceMessage(null)
    try {
      if (!value) {
        await api.del<void>(`/users/me/team-preferred-players/${team.teamId}`)
        setPreferredTeams(current => current?.map(item => item.teamId === team.teamId ? { ...item, preference: null } : item) ?? current)
        setPreferenceMessage(`Se quitó el jugador preferido de ${team.teamName}.`)
      } else {
        const preference = await api.put<UserTeamPreferredPlayer>(`/users/me/team-preferred-players/${team.teamId}`, { teamPlayerId: Number(value) })
        setPreferredTeams(current => current?.map(item => item.teamId === team.teamId ? { ...item, preference } : item) ?? current)
        setPreferenceMessage(`Jugador preferido de ${team.teamName} actualizado.`)
      }
    } catch (reason) {
      setPreferenceError(reason instanceof ApiError ? reason.message : 'No se pudo actualizar la preferencia.')
    } finally {
      setSavingTeamId(null)
    }
  }

  return (
    <div>
      <div className="pp-header">
        <h1>Mi Perfil</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {saved && <StatusMessage kind="success" message="Perfil actualizado correctamente." />}

      <div className="pp-profile__card">
        <div className="pp-profile__avatar-section">
          <div className="pp-profile__avatar">
            {user.firstName[0]}{user.lastName[0]}
          </div>
          <div>
            <div className="pp-profile__name">{user.firstName} {user.lastName}</div>
            <div className="pp-profile__email">{user.email}</div>
          </div>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="pp-form__row">
            <div className="pp-form__field">
              <label className="pp-form__label" htmlFor="firstName">Nombre</label>
              <input
                id="firstName"
                className="pp-form__input"
                type="text"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
              />
              {fieldErrors.firstName && (
                <span className="pp-form__error">{fieldErrors.firstName[0]}</span>
              )}
            </div>

            <div className="pp-form__field">
              <label className="pp-form__label" htmlFor="lastName">Apellido</label>
              <input
                id="lastName"
                className="pp-form__input"
                type="text"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
              />
              {fieldErrors.lastName && (
                <span className="pp-form__error">{fieldErrors.lastName[0]}</span>
              )}
            </div>
          </div>

          <div className="pp-form__field">
            <label className="pp-form__label">Email</label>
            <input className="pp-form__input" type="text" value={user.email} disabled style={{ opacity: 0.6 }} />
          </div>

          <div className="pp-form__actions">
            <button type="submit" className="pp-btn pp-btn--primary" disabled={saving}>
              {saving ? 'Guardando...' : 'Guardar'}
            </button>
          </div>
        </form>
      </div>

      <section className="pp-profile__card pp-profile__card--preferred">
        <div className="pp-profile__section-heading">
          <h2>Jugadores preferidos</h2>
          <p>Elegí un jugador predeterminado para los equipos que quieras. Se usará como sugerencia al pronosticar.</p>
        </div>

        {preferenceError && <StatusMessage kind="error" message={preferenceError} />}
        {preferenceMessage && <StatusMessage kind="success" message={preferenceMessage} />}
        {!preferredTeams && !preferenceError && <StatusMessage kind="loading" message="Cargando equipos y jugadores..." />}
        {preferredTeams?.length === 0 && <p className="pp-profile__preferred-empty">No hay planteles activos disponibles.</p>}
        {preferredTeams && preferredTeams.length > 0 && (
          <div className="pp-profile__preferred-list">
            {preferredTeams.map(team => (
              <label key={team.teamId} className="pp-profile__preferred-row">
                <span className="pp-profile__preferred-team"><strong>{team.teamName}</strong><small>{team.teamShortName}</small></span>
                <select
                  className="pp-form__input"
                  value={team.preference?.isValid ? String(team.preference.teamPlayerId) : ''}
                  disabled={savingTeamId === team.teamId}
                  onChange={event => void changePreference(team, event.target.value)}
                >
                  <option value="">Sin jugador preferido</option>
                  {team.players.map(player => <option key={player.id} value={player.id}>{player.name}</option>)}
                </select>
                {team.preference && !team.preference.isValid && (
                  <span className="pp-profile__preferred-warning">
                    La preferencia guardada ya no está activa.
                    <button type="button" className="pp-btn pp-btn--secondary pp-btn--sm" disabled={savingTeamId === team.teamId} onClick={() => void changePreference(team, '')}>Quitar preferencia guardada</button>
                  </span>
                )}
              </label>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
