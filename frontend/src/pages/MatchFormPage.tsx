import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { isoToLocalInput, localInputToIsoUtc } from '../api/dateUtils'
import { MATCH_STATUSES, MATCH_STATUS_LABELS, type Match, type MatchStatus, type Team } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { appendReturnTo, validAdminReturnTo } from '../utils/adminReturnTo'

function SelectedTeam({ team }: { team: Team | undefined }) {
  if (!team) return null
  return <div className="selected-team-preview">{team.logoUrl ? <img src={team.logoUrl} alt="" /> : <span aria-hidden="true">{team.shortName.slice(0, 2).toUpperCase()}</span>}<strong>{team.name}</strong></div>
}

export default function MatchFormPage() {
  const { roundId: roundIdParam, matchId } = useParams()
  const isEdit = Boolean(matchId)
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const returnTo = validAdminReturnTo(searchParams.get('returnTo'))

  const [roundId, setRoundId] = useState<string | undefined>(roundIdParam)
  const [homeTeamId, setHomeTeamId] = useState('')
  const [awayTeamId, setAwayTeamId] = useState('')
  const [teams, setTeams] = useState<Team[]>([])
  const [roundMatches, setRoundMatches] = useState<Match[]>([])
  const [originalTeamIds, setOriginalTeamIds] = useState<number[]>([])
  const [startsAtUtc, setStartsAtUtc] = useState('')
  const [status, setStatus] = useState<MatchStatus>('Scheduled')
  const [currentStatus, setCurrentStatus] = useState<string | null>(null)

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  useEffect(() => {
    async function load() {
      try {
        const current = isEdit ? await api.get<Match>(`/matches/${matchId}`) : null
        const targetRoundId = current?.roundId ?? Number(roundIdParam)
        const [teamItems, matches] = await Promise.all([
          api.get<Team[]>('/teams'),
          api.get<Match[]>(`/rounds/${targetRoundId}/matches`),
        ])
        setTeams(teamItems.filter((team) => team.active))
        setRoundMatches(matches.filter((match) => match.id !== current?.id))
        if (!current) return
        const m = current
        setRoundId(String(m.roundId))
        setHomeTeamId(String(m.homeTeamId))
        setAwayTeamId(String(m.awayTeamId))
        setOriginalTeamIds([m.homeTeamId, m.awayTeamId])
        setStartsAtUtc(isoToLocalInput(m.startsAtUtc))
        setCurrentStatus(m.status)
        if (m.status !== 'Finished') {
          setStatus(m.status)
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'No se pudo cargar el partido.')
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [matchId, isEdit, roundIdParam])

  const usedTeamIds = new Set(roundMatches.flatMap((match) => [match.homeTeamId, match.awayTeamId]).filter((teamId) => !originalTeamIds.includes(teamId)))

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})

    if (!startsAtUtc) {
      setFieldErrors({ startsAtUtc: ['La fecha y hora del partido son obligatorias.'] })
      setSaving(false)
      return
    }
    if (homeTeamId && homeTeamId === awayTeamId) {
      setFieldErrors({ awayTeamId: ['El equipo visitante debe ser distinto del local.'] })
      setSaving(false)
      return
    }

    try {
      if (isEdit) {
        await api.put(`/matches/${matchId}`, {
          homeTeamId: Number(homeTeamId),
          awayTeamId: Number(awayTeamId),
          startsAtUtc: localInputToIsoUtc(startsAtUtc),
          // Un partido Finalizado no debe enviar status: su resultado oficial
          // solo se modifica desde "Cargar resultado".
          ...(currentStatus === 'Finished' ? {} : { status }),
        })
      } else {
        await api.post<Match>(`/rounds/${roundIdParam}/matches`, {
          homeTeamId: Number(homeTeamId),
          awayTeamId: Number(awayTeamId),
          startsAtUtc: localInputToIsoUtc(startsAtUtc),
          status,
        })
      }
      navigate(appendReturnTo(`/rounds/${roundId}/matches`, returnTo), {
        replace: true,
        state: { savedMessage: 'Partido guardado correctamente.' },
      })
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

  if (loading) {
    return <StatusMessage kind="loading" message="Cargando partido..." />
  }

  return (
    <div>
      <div className="breadcrumb">
        <Link to={appendReturnTo(`/rounds/${roundId}/matches`, returnTo)}>← Volver a Partidos</Link>
      </div>
      <div className="admin-header">
        <h1>{isEdit ? 'Editar Partido' : 'Nuevo Partido'}</h1>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {currentStatus === 'Finished' && (
        <StatusMessage
          kind="loading"
          message="Este partido ya tiene Resultado Oficial cargado. El estado se administra desde la lista de Partidos."
        />
      )}

      <form className="form-card" onSubmit={handleSubmit}>
        <div className="form-row">
          <div className="form-field">
            <label htmlFor="homeTeamId">Equipo local</label>
            <select id="homeTeamId" value={homeTeamId} onChange={(e) => setHomeTeamId(e.target.value)} required>
              <option value="">Seleccionar equipo</option>
              {teams.map((team) => <option key={team.id} value={team.id} disabled={usedTeamIds.has(team.id)}>{team.name}{usedTeamIds.has(team.id) ? ' — ya participa en esta Fecha' : ''}</option>)}
            </select>
            <SelectedTeam team={teams.find((team) => team.id === Number(homeTeamId))} />
            {fieldErrors.homeTeamId && (
              <span className="form-field-error">{fieldErrors.homeTeamId[0]}</span>
            )}
          </div>

          <div className="form-field">
            <label htmlFor="awayTeamId">Equipo visitante</label>
            <select id="awayTeamId" value={awayTeamId} onChange={(e) => setAwayTeamId(e.target.value)} required>
              <option value="">Seleccionar equipo</option>
              {teams.map((team) => <option key={team.id} value={team.id} disabled={usedTeamIds.has(team.id)}>{team.name}{usedTeamIds.has(team.id) ? ' — ya participa en esta Fecha' : ''}</option>)}
            </select>
            <SelectedTeam team={teams.find((team) => team.id === Number(awayTeamId))} />
            {fieldErrors.awayTeamId && (
              <span className="form-field-error">{fieldErrors.awayTeamId[0]}</span>
            )}
          </div>
        </div>

        <div className="form-field">
          <label htmlFor="startsAtUtc">Fecha y hora de inicio</label>
          <input
            id="startsAtUtc"
            type="datetime-local"
            value={startsAtUtc}
            onChange={(e) => setStartsAtUtc(e.target.value)}
            required
          />
          {fieldErrors.startsAtUtc && <span className="form-field-error">{fieldErrors.startsAtUtc[0]}</span>}
        </div>

        <div className="form-field">
          <label htmlFor="status">Estado</label>
          {currentStatus === 'Finished' ? (
            <input id="status" type="text" value={MATCH_STATUS_LABELS.Finished} disabled />
          ) : (
            <select
              id="status"
              value={status}
              onChange={(e) => setStatus(e.target.value as MatchStatus)}
            >
              {MATCH_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {MATCH_STATUS_LABELS[s]}
                </option>
              ))}
            </select>
          )}
          {fieldErrors.status && <span className="form-field-error">{fieldErrors.status[0]}</span>}
        </div>

        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar'}
          </button>
          <Link to={appendReturnTo(`/rounds/${roundId}/matches`, returnTo)} className="btn btn-secondary">
            Cancelar
          </Link>
        </div>
      </form>
    </div>
  )
}
