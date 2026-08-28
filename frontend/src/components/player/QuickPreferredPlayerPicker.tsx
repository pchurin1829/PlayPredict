import { useEffect, useRef, useState, type CSSProperties, type MouseEvent, type PointerEvent as ReactPointerEvent } from 'react'
import { createPortal } from 'react-dom'
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
  selectedPlayerName?: string | null
  onChange: (value: string) => void
  onSelectionComplete?: () => void
  ariaLabel?: string
}

export default function QuickPreferredPlayerPicker(props: Props) {
  const { homeTeam, awayTeam, homePlayers, awayPlayers, value, selectedPlayerName, onChange, onSelectionComplete, ariaLabel } = props
  const quickPlayers = props.quickPlayers ?? []
  const selected = quickPlayers.find(player => String(player.id) === value)
    ?? [...homePlayers, ...awayPlayers].find(player => String(player.id) === value)
  const [quickMenuOpen, setQuickMenuOpen] = useState(false)
  const [showAll, setShowAll] = useState(false)
  const pickerRef = useRef<HTMLDivElement>(null)
  const fieldRef = useRef<HTMLButtonElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)
  const [menuStyle, setMenuStyle] = useState<CSSProperties | null>(null)

  useEffect(() => {
    if (!quickMenuOpen) return

    const positionMenu = () => {
      const field = fieldRef.current
      if (!field) return
      const rect = field.getBoundingClientRect()
      const viewportPadding = 8
      const gap = 6
      const width = Math.min(rect.width, window.innerWidth - viewportPadding * 2)
      const left = Math.min(Math.max(viewportPadding, rect.left), window.innerWidth - viewportPadding - width)
      setMenuStyle({ top: rect.bottom + gap, left, width })
    }

    positionMenu()
    window.addEventListener('resize', positionMenu)
    window.addEventListener('scroll', positionMenu, true)
    return () => {
      window.removeEventListener('resize', positionMenu)
      window.removeEventListener('scroll', positionMenu, true)
    }
  }, [quickMenuOpen])

  useEffect(() => {
    if (!quickMenuOpen) return
    const closeOnOutsideClick = (event: PointerEvent) => {
      const target = event.target as Node
      if (!pickerRef.current?.contains(target) && !menuRef.current?.contains(target)) setQuickMenuOpen(false)
    }
    document.addEventListener('pointerdown', closeOnOutsideClick)
    return () => document.removeEventListener('pointerdown', closeOnOutsideClick)
  }, [quickMenuOpen])

  const openPicker = () => {
    if (quickPlayers.length === 0) setShowAll(true)
    else setQuickMenuOpen(current => !current)
  }

  const updateSelection = (playerId: string) => {
    onChange(playerId)
  }

  const chooseQuickPlayer = (playerId: string) => {
    updateSelection(playerId)
    setQuickMenuOpen(false)
    setShowAll(false)
    onSelectionComplete?.()
  }

  const keepMenuEventInsidePortal = (event: ReactPointerEvent<HTMLButtonElement>) => {
    event.preventDefault()
    event.stopPropagation()
  }

  const handleQuickPlayerClick = (event: MouseEvent<HTMLButtonElement>, playerId: string) => {
    event.preventDefault()
    event.stopPropagation()
    chooseQuickPlayer(playerId)
  }

  const removePlayer = () => {
    updateSelection('')
    setQuickMenuOpen(false)
    setShowAll(false)
    onSelectionComplete?.()
  }

  const quickMenu = quickMenuOpen && menuStyle && createPortal(
    <div ref={menuRef} className="quick-preferred-picker__menu" role="menu" aria-label="Jugadores preferidos sugeridos" style={menuStyle}>
      {quickPlayers.map(player => (
        <button type="button" role="menuitem" key={player.id} onPointerDown={keepMenuEventInsidePortal} onClick={event => handleQuickPlayerClick(event, String(player.id))}>
          {preferredPlayerLabel(player)}
        </button>
      ))}
      <button type="button" role="menuitem" className="quick-preferred-picker__search" onPointerDown={keepMenuEventInsidePortal} onClick={event => { event.preventDefault(); event.stopPropagation(); setQuickMenuOpen(false); setShowAll(true) }}>Buscar otro jugador...</button>
    </div>,
    document.body,
  )

  return (
    <div className="quick-preferred-picker" ref={pickerRef}>
      <div className="quick-preferred-picker__control">
        <button ref={fieldRef} type="button" className="quick-preferred-picker__field" data-preferred-player-input aria-label={ariaLabel ?? 'Seleccionar Jugador Preferido'} aria-haspopup="menu" aria-expanded={quickMenuOpen || showAll} onClick={openPicker}>
          {selected ? preferredPlayerLabel(selected) : value && selectedPlayerName ? selectedPlayerName : 'Buscar jugador...'}
        </button>
        {value && <button type="button" className="quick-preferred-picker__remove" onClick={removePlayer}>Quitar</button>}
      </div>

      {quickMenu}

      {showAll && (
        <PreferredPlayerPicker
          homeTeam={homeTeam}
          awayTeam={awayTeam}
          homePlayers={homePlayers}
          awayPlayers={awayPlayers}
          value={value}
          onChange={updateSelection}
          onSelectionComplete={() => { setShowAll(false); onSelectionComplete?.() }}
          ariaLabel={ariaLabel}
          initiallyOpen
          onDismiss={() => setShowAll(false)}
        />
      )}
    </div>
  )
}
