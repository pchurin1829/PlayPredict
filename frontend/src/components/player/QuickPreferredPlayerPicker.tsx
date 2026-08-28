import { useEffect, useId, useState } from 'react'
import type { AvailablePlayer } from '../../api/types'
import PreferredPlayerPicker, { preferredPlayerLabel } from './PreferredPlayerPicker'
import './QuickPreferredPlayerPicker.css'

interface Props {
  homeTeam: string
  awayTeam: string
  homePlayers: AvailablePlayer[]
  awayPlayers: AvailablePlayer[]
  quickPlayers: AvailablePlayer[]
  value: string
  onChange: (value: string) => void
  onSelectionComplete?: () => void
  ariaLabel?: string
}

export default function QuickPreferredPlayerPicker(props: Props) {
  const { homeTeam, awayTeam, homePlayers, awayPlayers, value, onChange, onSelectionComplete, ariaLabel } = props
  // El backend siempre envía un array (posiblemente vacío) para quickPreferredPlayers, pero este
  // componente es un primitivo reutilizable: si todavía no llegaron las preferencias (o llega un
  // valor sin esa colección) se lo trata igual que "0 preferencias" y se cae al selector largo.
  const quickPlayers = props.quickPlayers ?? []
  const quickIds = quickPlayers.map(player => String(player.id))
  const [showAll, setShowAll] = useState(Boolean(value && !quickIds.includes(value)))
  const groupName = useId()

  useEffect(() => {
    if (value && !quickIds.includes(value)) setShowAll(true)
  }, [quickIds, value])

  if (quickPlayers.length === 0) {
    return <PreferredPlayerPicker {...props} />
  }

  const teamName = (player: AvailablePlayer) => homePlayers.some(candidate => candidate.teamId === player.teamId) ? homeTeam : awayTeam

  return (
    <div className="quick-preferred-picker">
      <div className="quick-preferred-picker__options" role="radiogroup" aria-label={ariaLabel ?? 'Jugadores preferidos sugeridos'}>
        {quickPlayers.map(player => {
          const playerId = String(player.id)
          return (
            <label key={player.id} className="quick-preferred-picker__option">
              <input type="radio" name={groupName} checked={value === playerId} onChange={() => { onChange(playerId); setShowAll(false); onSelectionComplete?.() }} />
              <span><strong>{preferredPlayerLabel(player)}</strong><small>{teamName(player)}</small></span>
            </label>
          )
        })}
        <label className="quick-preferred-picker__option quick-preferred-picker__option--search">
          <input type="radio" name={groupName} checked={showAll} onChange={() => setShowAll(true)} />
          <span><strong>Buscar otros jugadores...</strong></span>
        </label>
      </div>
      {showAll && (
        <PreferredPlayerPicker
          homeTeam={homeTeam}
          awayTeam={awayTeam}
          homePlayers={homePlayers}
          awayPlayers={awayPlayers}
          value={value}
          onChange={onChange}
          onSelectionComplete={onSelectionComplete}
          ariaLabel={ariaLabel}
        />
      )}
    </div>
  )
}
