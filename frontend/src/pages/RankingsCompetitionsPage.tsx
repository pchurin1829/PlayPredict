import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import type { Competition, Edition, RankingEntry } from '../api/types'
import StatusMessage from '../components/StatusMessage'
import { useAuth } from '../auth/AuthContext'
import './PlayerPages.css'

interface CompetitionWithEdition {
  competition: Competition
  activeEdition: Edition | null
}

export default function RankingsCompetitionsPage() {
  const [items, setItems] = useState<CompetitionWithEdition[] | null>(null)
  const [selectedCompId, setSelectedCompId] = useState<number | null>(null)
  const [ranking, setRanking] = useState<RankingEntry[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const { user } = useAuth()

  useEffect(() => {
    let cancelled = false
    setError(null)

    api
      .get<Competition[]>('/competitions')
      .then(async (competitions) => {
        const active = competitions.filter((c) => c.isActive)
        const enriched: CompetitionWithEdition[] = []

        for (const competition of active) {
          const editions = await api.get<Edition[]>(`/competitions/${competition.id}/editions`)
          const activeEd = editions.find((e) => e.status === 'Active') ?? null
          enriched.push({ competition, activeEdition: activeEd })
        }

        if (cancelled) return
        setItems(enriched)

        const first = enriched.find((e) => e.activeEdition)
        if (first) {
          setSelectedCompId(first.competition.id)
        }
      })
      .catch((err) => {
        if (!cancelled) setError(err.message ?? 'No se pudieron cargar las competencias.')
      })

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!selectedCompId || !items) return

    const item = items.find((i) => i.competition.id === selectedCompId)
    if (!item?.activeEdition) {
      setRanking(null)
      return
    }

    let cancelled = false
    api
      .get<RankingEntry[]>(`/rankings/editions/${item.activeEdition.id}`)
      .then((data) => {
        if (!cancelled) setRanking(data)
      })
      .catch(() => {
        if (!cancelled) setRanking([])
      })

    return () => {
      cancelled = true
    }
  }, [selectedCompId, items])

  if (error) {
    return <StatusMessage kind="error" message={error} />
  }

  if (!items) {
    return <StatusMessage kind="loading" message="Cargando rankings..." />
  }

  const selectedItem = items.find((i) => i.competition.id === selectedCompId)

  return (
    <div>
      <div className="pp-header">
        <h1>Ranking</h1>
      </div>

      {items.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">📊</span>
          <p className="pp-empty__text">No hay competencias activas todavía.</p>
        </div>
      )}

      {items.length > 0 && (
        <div className="pp-edition-select">
          <span className="pp-edition-select__label">Competencia:</span>
          <select
            className="pp-edition-select__select"
            value={selectedCompId ?? ''}
            onChange={(e) => setSelectedCompId(Number(e.target.value))}
          >
            {items.map((item) => (
              <option key={item.competition.id} value={item.competition.id}>
                {item.competition.name}
                {item.activeEdition ? ` — ${item.activeEdition.name}` : ' (sin edición activa)'}
              </option>
            ))}
          </select>
          {selectedItem?.activeEdition && (
            <Link
              to={`/rankings/editions/${selectedItem.activeEdition.id}/rounds`}
              className="pp-btn pp-btn--secondary"
              style={{ fontSize: '0.8rem' }}
            >
              Ranking por Fecha
            </Link>
          )}
        </div>
      )}

      {selectedItem && !selectedItem.activeEdition && (
        <div className="pp-empty">
          <span className="pp-empty__icon">📊</span>
          <p className="pp-empty__text">Esta competencia no tiene edición activa.</p>
        </div>
      )}

      {selectedItem?.activeEdition && ranking === null && (
        <StatusMessage kind="loading" message="Cargando ranking..." />
      )}

      {ranking && ranking.length === 0 && (
        <div className="pp-empty">
          <span className="pp-empty__icon">📊</span>
          <p className="pp-empty__text">Todavía no hay pronósticos evaluados en esta edición.</p>
        </div>
      )}

      {ranking && ranking.length > 0 && (
        <div className="pp-ranking">
          <div className="pp-ranking__header">
            <h2>{selectedItem?.activeEdition?.name ?? 'Ranking General'}</h2>
          </div>
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Jugador</th>
                <th>Puntos</th>
                <th>Exactos</th>
                <th>Correctos</th>
                <th>Evaluados</th>
              </tr>
            </thead>
            <tbody>
              {ranking.map((r) => {
                const isMe = user && r.userId === user.id
                return (
                  <tr key={r.userId} className={isMe ? 'pp-ranking__me' : ''}>
                    <td>
                      <span className={`pp-ranking__pos ${r.position <= 3 ? `pp-ranking__pos--${r.position}` : ''}`}>
                        {r.position}°
                      </span>
                    </td>
                    <td>
                      {r.firstName} {r.lastName}
                      {isMe && <span className="pp-ranking__me-badge">(Vos)</span>}
                    </td>
                    <td className="pp-ranking__points">{r.points}</td>
                    <td>{r.exactCount}</td>
                    <td>{r.correctCount}</td>
                    <td>{r.evaluatedCount}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
