import { useEffect, useMemo, useState } from 'react'
import { api, ApiError } from '../api/client'
import type { PreferredPlayerProfileTeam, UserTeamPreferredPlayer } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase()

export default function PreferredPlayersPage() {
  const [teams, setTeams] = useState<PreferredPlayerProfileTeam[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [savingTeamId, setSavingTeamId] = useState<number | null>(null)
  const [search, setSearch] = useState('')

  useEffect(() => {
    api.get<PreferredPlayerProfileTeam[]>('/users/me/team-preferred-players/options')
      .then(setTeams)
      .catch((reason) => setError(reason instanceof ApiError ? reason.message : 'No se pudieron cargar los equipos y jugadores.'))
  }, [])

  const visibleTeams = useMemo(() => {
    if (!teams) return []
    const filter = normalize(search)
    return teams.filter(team => !filter || normalize(`${team.teamName} ${team.teamShortName}`).includes(filter))
  }, [teams, search])

  async function changePreference(team: PreferredPlayerProfileTeam, value: string) {
    setSavingTeamId(team.teamId)
    setError(null)
    setMessage(null)
    try {
      if (!value) {
        await api.del<void>(`/users/me/team-preferred-players/${team.teamId}`)
        setTeams(current => current?.map(item => item.teamId === team.teamId ? { ...item, preference: null } : item) ?? current)
        setMessage(`Se quitó el jugador preferido de ${team.teamName}.`)
      } else {
        const preference = await api.put<UserTeamPreferredPlayer>(`/users/me/team-preferred-players/${team.teamId}`, { teamPlayerId: Number(value) })
        setTeams(current => current?.map(item => item.teamId === team.teamId ? { ...item, preference } : item) ?? current)
        setMessage(`Jugador preferido de ${team.teamName} actualizado.`)
      }
    } catch (reason) {
      setError(reason instanceof ApiError ? reason.message : 'No se pudo actualizar la preferencia.')
    } finally {
      setSavingTeamId(null)
    }
  }

  return (
    <div>
      <div className="pp-header">
        <h1>Mis jugadores preferidos</h1>
      </div>

      <section className="pp-profile__card pp-profile__card--preferred">
        <div className="pp-profile__section-heading">
          <p>Elegí un jugador predeterminado para los equipos de tus Ligas Oficiales. Se usará como sugerencia al pronosticar.</p>
        </div>

        {error && <StatusMessage kind="error" message={error} />}
        {message && <StatusMessage kind="success" message={message} />}
        {!teams && !error && <StatusMessage kind="loading" message="Cargando equipos y jugadores..." />}

        {teams && teams.length > 0 && (
          <div className="pp-preferred__filters">
            <input
              type="search"
              className="pp-form__input pp-preferred__search"
              placeholder="Buscar equipo..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        )}

        {teams?.length === 0 && <p className="pp-profile__preferred-empty">No participás de ninguna Liga Oficial con planteles activos disponibles.</p>}
        {teams && teams.length > 0 && visibleTeams.length === 0 && <p className="pp-profile__preferred-empty">No se encontraron equipos con ese filtro.</p>}

        {visibleTeams.length > 0 && (
          <div className="pp-profile__preferred-list">
            {visibleTeams.map(team => (
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
