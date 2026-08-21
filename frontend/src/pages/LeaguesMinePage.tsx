import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api, ApiError } from '../api/client'
import { LEAGUE_SCOPE_LABELS, type LeagueSummary } from '../api/types'
import ConfirmModal from '../components/ConfirmModal'
import StatusMessage from '../components/StatusMessage'
import './PlayerPages.css'

interface ModalTarget {
  id: number
  name: string
  action: 'leave' | 'suspend' | 'reactivate'
}

export default function LeaguesMinePage() {
  const [myLeagues, setMyLeagues] = useState<LeagueSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [actingId, setActingId] = useState<number | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [modalTarget, setModalTarget] = useState<ModalTarget | null>(null)

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
  const modalConfig = modalTarget
    ? {
        leave: { title: 'Dejar de participar', msg: `¿Querés dejar de participar en "${modalTarget.name}"?`, confirm: 'Dejar liga' },
        suspend: { title: 'Suspender Liga', msg: `¿Querés suspender "${modalTarget.name}"? Los participantes y pronósticos se conservarán.`, confirm: 'Suspender' },
        reactivate: { title: 'Reactivar Liga', msg: `¿Querés reactivar "${modalTarget.name}"?`, confirm: 'Reactivar' },
      }[modalTarget.action]
    : null

  return (
    <div>
      <div className="pp-header">
        <h1>Mis Ligas</h1>
        <p className="pp-header__subtitle">Tus Ligas de amigos y las Ligas Oficiales de PlayPredict en las que participás</p>
        <div className="pp-header__actions">
          <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
            ✋ Unirme a una Liga de amigos
          </Link>
        </div>
      </div>

      {error && <StatusMessage kind="error" message={error} />}
      {loading && <StatusMessage kind="loading" message="Cargando Ligas..." />}
      {message && <StatusMessage kind={message.includes('error') || message.includes('No podés') || message.includes('incorrecto') ? 'error' : 'success'} message={message} />}

      {/* ── Mis Ligas ── */}
      {myLeagues && (
        <>
          <div className="pp-section-title">
            <h2>📋 Mis Ligas</h2>
            <p>Ligas en las que participás</p>
          </div>
          {myLeagues.length === 0 ? (
            <div className="pp-empty">
              <span className="pp-empty__icon">🏆</span>
              <p className="pp-empty__text">
                Aún no participás en ninguna Liga.
                <br />
                Explorá competencias para participar en una Liga Oficial de PlayPredict o creá tu propia Liga con amigos.
              </p>
              <div className="pp-empty__actions">
                <Link to="/competitions/explore" className="pp-btn pp-btn--primary">
                  Explorar Competencias Oficiales
                </Link>
                <Link to="/leagues/join" className="pp-btn pp-btn--secondary">
                  ✋ Unirme a una Liga de amigos
                </Link>
              </div>
            </div>
          ) : (
            <div className="pp-grid">
              {myLeagues.map((l) => {
                const isOfficial = l.leagueType === 'Official'
                const cardClass = isOfficial
                  ? 'pp-league-card pp-league-card--official'
                  : 'pp-league-card pp-league-card--mine'

                return (
                  <div key={l.id} className={cardClass}>
                    <div className="pp-league-card__header">
                      <h3 className="pp-league-card__name">{l.name}</h3>
                      {isOfficial ? (
                        <span className="pp-league-card__badge pp-league-card__badge--official">
                          🏆 OFICIAL
                        </span>
                      ) : l.isCreator ? (
                        <span className="pp-league-card__badge pp-league-card__badge--mine">
                          MI LIGA
                        </span>
                      ) : (
                        <span className="pp-league-card__badge pp-league-card__badge--private">
                          AMIGOS
                        </span>
                      )}
                    </div>
                    <span className="pp-league-card__comp">⚽ {l.competitionName}</span>
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
                      <span className={`pp-league-card__status ${l.isActive ? 'pp-league-card__status--active' : l.isCreator && !l.isActive ? 'pp-league-card__status--suspended' : 'pp-league-card__status--inactive'}`}>
                        {l.isActive ? 'Activa' : l.isCreator ? 'Suspendida' : 'Inactiva'}
                      </span>
                      <div className="pp-league-card__actions-row">
                        <Link to={`/leagues/${l.id}`} className="pp-league-card__action">
                          Entrar
                        </Link>

                        {/* Caso A: Liga Oficial — puede dejar */}
                        {isOfficial && (
                          <button
                            type="button"
                            className="pp-btn pp-btn--secondary pp-btn--sm"
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
                            className="pp-btn pp-btn--secondary pp-btn--sm"
                            disabled={actingId === l.id}
                            onClick={() => openModal(l.id, l.name, 'leave')}
                          >
                            {actingId === l.id ? 'Saliendo...' : 'Dejar de participar'}
                          </button>
                        )}

                        {/* Caso C: MI LIGA creada por el usuario — Suspender / Reactivar */}
                        {!isOfficial && l.isCreator && l.isActive && (
                          <button
                            type="button"
                            className="pp-btn pp-btn--secondary pp-btn--sm"
                            disabled={actingId === l.id}
                            onClick={() => openModal(l.id, l.name, 'suspend')}
                          >
                            {actingId === l.id ? 'Suspendiendo...' : 'Suspender Liga'}
                          </button>
                        )}
                        {!isOfficial && l.isCreator && !l.isActive && (
                          <button
                            type="button"
                            className="pp-btn pp-btn--primary pp-btn--sm"
                            disabled={actingId === l.id}
                            onClick={() => openModal(l.id, l.name, 'reactivate')}
                          >
                            {actingId === l.id ? 'Reactivando...' : 'Reactivar Liga'}
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </>
      )}

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
