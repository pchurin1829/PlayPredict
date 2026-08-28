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
  const { homeTeam, awayTeam, homePlayers, awayPlayers, quickPlayers, value, onChange, onSelectionComplete, ariaLabel } = props
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
