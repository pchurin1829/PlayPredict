import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { LEAGUE_SCOPE_LABELS, type LeagueSummary } from '../api/types'
import ConfirmModal from '../components/ConfirmModal'
import StatusMessage from '../components/StatusMessage'
import { useCompanySettings } from '../company/CompanySettingsContext'
import './PlayerPages.css'

interface ModalTarget {
  id: number
  name: string
  action: 'leave' | 'suspend' | 'reactivate' | 'delete'
}

export default function LeaguesMinePage() {
  const { company } = useCompanySettings()
  const companyName = company.shortName || 'PlayPredict'
  const [myLeagues, setMyLeagues] = useState<LeagueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [actingId, setActingId] = useState<number | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [modalTarget, setModalTarget] = useState<ModalTarget | null>(null)
  const [manageTargetId, setManageTargetId] = useState<number | null>(null)

  useEffect(() => {
    let cancelled = false
    setError(null)

    async function load() {
      try {
        const mine = await api.get<LeagueSummary[]>('/leagues/mine')
        if (!cancelled) setMyLeagues(mine)
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof ApiError ? err.message : 'No se pudieron cargar tus Ligas.')
        }
      }
    }

    load()
    return () => { cancelled = true }
  }, [])

  async function refresh() {
    const mine = await api.get<LeagueSummary[]>('/leagues/mine')
    setMyLeagues(mine)
  }

  function openModal(id: number, name: string, action: ModalTarget['action']) {
    setManageTargetId(null)
    setModalTarget({ id, name, action })
  }

  function cancelModal() {
    setModalTarget(null)
  }

  async function confirmModal() {
    if (!modalTarget) return
    const { id, action } = modalTarget
    setModalTarget(null)
    setActingId(id)
    setMessage(null)

    try {
      if (action === 'leave') {
        await api.del(`/leagues/${id}/leave`)
        setMessage('Dejaste la Liga correctamente.')
        await refresh()
      } else if (action === 'suspend') {
        const league = myLeagues?.find((l) => l.id === id)
        await api.put(`/leagues/${id}`, {
          name: league?.name ?? '',
          description: league?.description ?? null,
          isActive: false,
        })
        setMessage('Liga suspendida correctamente.')
        await refresh()
      } else if (action === 'reactivate') {
        const league = myLeagues?.find((l) => l.id === id)
        await api.put(`/leagues/${id}`, {
          name: league?.name ?? '',
          description: league?.description ?? null,
          isActive: true,
        })
        setMessage('Liga reactivada correctamente.')
        await refresh()
      } else if (action === 'delete') {
        await api.del(`/leagues/${id}`)
        setManageTargetId(null)
        setMessage('Liga eliminada correctamente.')
        await refresh()
      }
    } catch (err) {
      if (err instanceof ApiError && err.fieldErrors.league) {
        setMessage(err.fieldErrors.league[0])
      } else {
        setMessage(err instanceof ApiError ? err.message : 'Ocurrió un error.')
      }
    } finally {
      setActingId(null)
      setTimeout(() => setMessage(null), 5000)
    }
  }

  const loading = !myLeagues && !error
  const leagueGroups = myLeagues
    ? [
        {
          title: `Competencias ${companyName}`,
          subtitle: `Competencias propias de ${companyName} en las que participás`,
          leagues: myLeagues.filter((league) => league.leagueType === 'Official'),
        },
        {
          title: 'Mis Ligas de Amigos',
          subtitle: 'Ligas propias y de amigos en las que participás',
          leagues: myLeagues.filter((league) => league.leagueType === 'Private'),
        },
      ].filter((group) => group.leagues.length > 0)
    : []
  const modalConfig = modalTarget
    ? {
        leave: { title: 'Dejar de participar', msg: `¿Querés dejar de participar en "${modalTarget.name}"?`, confirm: 'Dejar liga' },
        suspend: { title: 'Suspender Liga', msg: `¿Querés suspender "${modalTarget.name}"? Los participantes y pronósticos se conservarán.`, confirm: 'Suspender' },
        reactivate: { title: 'Reactivar Liga', msg: `¿Querés reactivar "${modalTarget.name}"?`, confirm: 'Reactivar' },
        delete: { title: 'Eliminar Liga definitivamente', msg: `¿Querés eliminar "${modalTarget.name}"? Se borrarán la Liga, sus participantes, pronósticos, puntos e historial. Esta acción no se puede deshacer.`, confirm: 'Sí, eliminar Liga' },
      }[modalTarget.action]
    : null

  return (
    <div>
      <div className="pp-header">
        <h1>Mis Ligas</h1>
        <p className="pp-header__subtitle">Tus Ligas de amigos y las competencias {companyName} en las que participás</p>
        <div className="pp-header__actions">
          <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
            ✋ Unirme a Liga de Amigos con código
          </Link>
        </div>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {loading && <StatusMessage kind="loading" message="Cargando Ligas..." />}
      {message && <StatusMessage kind={message.includes('error') || message.includes('No podés') || message.includes('incorrecto') ? 'error' : 'success'} message={message} />}

      {/* ── Mis Ligas ── */}
      {myLeagues && (
        <>
          {myLeagues.length === 0 ? (
            <div className="pp-empty">
              <span className="pp-empty__icon">🏆</span>
              <p className="pp-empty__text">
                Aún no participás en ninguna Liga.
                <br />
                Explorá competencias para participar en una competencia {companyName} o creá tu propia Liga con amigos.
              </p>
              <div className="pp-empty__actions">
                <Link to="/competitions/explore" className="pp-btn pp-btn--primary">
                  Explorar Competencias
                </Link>
                <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
                  ✋ Unirme a Liga de Amigos con código
                </Link>
              </div>
            </div>
          ) : (
            <div className="pp-league-groups">
              {leagueGroups.map((group) => (
                <section key={group.title} className="pp-league-group">
                  <div className="pp-section-title pp-section-title--league-group">
                    <h2>{group.title}</h2>
                    <p>{group.subtitle}</p>
                  </div>
                  <div className="pp-grid">
              {group.leagues.map((l) => {
                const isOfficial = l.leagueType === 'Official'
                const cardClass = isOfficial
                  ? 'pp-league-card pp-league-card--official'
                  : 'pp-league-card pp-league-card--mine'

                return (
                  <div key={l.id} className={cardClass}>
                    <div className="pp-league-card__header">
                      <h3 className="pp-league-card__name">{l.name}</h3>
                      <div className="pp-league-card__badge-stack">
                        {isOfficial ? (
                          <span className="pp-league-card__badge pp-league-card__badge--official">🏆 OFICIAL</span>
                        ) : l.isCreator ? (
                          <span className="pp-league-card__badge pp-league-card__badge--mine">MI LIGA</span>
                        ) : (
                          <span className="pp-league-card__badge pp-league-card__badge--private">AMIGOS</span>
                        )}
                        <span className={`pp-league-card__status ${l.isActive ? 'pp-league-card__status--active' : 'pp-league-card__status--suspended'}`}>
                          {l.isActive ? 'Activa' : 'Suspendida'}
                        </span>
                      </div>
                    </div>
                    <span className="pp-league-card__comp">⚽ {l.competitionName} · {l.editionName}</span>
                    <div className="pp-league-card__meta">
                      <span>
                        📋 {LEAGUE_SCOPE_LABELS[l.scopeType]}
                        {l.scopeType === 'RoundRange' && l.roundFromName && l.roundToName && (
                          <> ({l.roundFromName} → {l.roundToName})</>
                        )}
                      </span>
                      <span>👥 {l.participantsCount} participante{l.participantsCount !== 1 ? 's' : ''}</span>
                    </div>
                    <div className="pp-league-card__footer">
                      <div className={`pp-league-card__actions-row pp-league-card__actions-row--${isOfficial ? 'official' : 'private'}`}>
                        <Link to={`/leagues/${l.id}`} className="pp-league-card__action">
                          Entrar
                        </Link>

                        {/* Caso A: Liga Oficial — puede dejar */}
                        {isOfficial && (
                          <button
                            type="button"
                            className="pp-btn pp-btn--card-secondary pp-btn--sm"
                            disabled={actingId === l.id}
                            onClick={() => openModal(l.id, l.name, 'leave')}
                          >
                            {actingId === l.id ? 'Saliendo...' : 'Dejar de participar'}
                          </button>
                        )}

                        {/* Caso B: Liga de Amigos ajena — puede dejar */}
                        {!isOfficial && !l.isCreator && (
                          <button
                            type="button"
                            className="pp-btn pp-btn--card-secondary pp-btn--sm"
                            disabled={actingId === l.id}
                            onClick={() => openModal(l.id, l.name, 'leave')}
                          >
                            {actingId === l.id ? 'Saliendo...' : 'Dejar de participar'}
                          </button>
                        )}

                        {/* Caso C: MI LIGA creada por el usuario — administración centralizada */}
                        {!isOfficial && l.isCreator && (
                          <button
                            type="button"
                            className="pp-btn pp-btn--card-secondary pp-btn--sm"
                            disabled={actingId === l.id}
                            onClick={() => setManageTargetId(l.id)}
                          >
                            Administrar
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                )
              })}
                  </div>
                </section>
              ))}
            </div>
          )}
        </>
      )}

      {manageTargetId !== null && (() => {
        const target = myLeagues?.find((league) => league.id === manageTargetId)
        if (!target) return null
        return (
          <div className="cmodal-overlay" onClick={() => setManageTargetId(null)}>
            <div className="cmodal pp-manage-modal" onClick={(event) => event.stopPropagation()}>
              <h3 className="cmodal__title">Administrar {target.name}</h3>
              <p className="cmodal__message">
                La suspensión conserva participantes, pronósticos, resultados y ranking, pero impide nuevas participaciones y pronósticos.
              </p>
              <div className="pp-manage-modal__actions">
                <button
                  type="button"
                  className="pp-btn pp-btn--secondary"
                  onClick={() => openModal(target.id, target.name, target.isActive ? 'suspend' : 'reactivate')}
                >
                  {target.isActive ? 'Suspender Liga' : 'Reactivar Liga'}
                </button>
                <button
                  type="button"
                  className="pp-btn pp-btn--danger"
                  onClick={() => openModal(target.id, target.name, 'delete')}
                >
                  Eliminar Liga
                </button>
              </div>
              <button type="button" className="cmodal__cancel" onClick={() => setManageTargetId(null)}>Cerrar</button>
            </div>
          </div>
        )
      })()}

      <ConfirmModal
        open={modalTarget !== null}
        title={modalConfig?.title ?? ''}
        message={modalConfig?.msg ?? ''}
        confirmLabel={modalConfig?.confirm ?? 'Confirmar'}
        cancelLabel="Cancelar"
        onConfirm={confirmModal}
        onCancel={cancelModal}
      />
    </div>
  )
}
