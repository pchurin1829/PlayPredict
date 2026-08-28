import { useEffect, useId, useMemo, useRef, useState, type CSSProperties, type KeyboardEvent } from 'react'
import { createPortal } from 'react-dom'
import type { AvailablePlayer } from '../../api/types'
import './PreferredPlayerPicker.css'

interface Props {
  homeTeam: string
  awayTeam: string
  homePlayers: AvailablePlayer[]
  awayPlayers: AvailablePlayer[]
  value: string
  onChange: (value: string) => void
  onSelectionComplete?: () => void
  ariaLabel?: string
}

const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase()
const demoPositions = new Set(['Arquero', 'Defensor', 'Mediocampista', 'Delantero'])

/** Keeps real names intact while avoiding the team name embedded in generated demo rosters. */
export const preferredPlayerLabel = (player: AvailablePlayer) => {
  if (demoPositions.has(player.firstName)) {
    const demoNumber = player.lastName.match(/\d+\s*$/)?.[0]?.trim()
    return demoNumber ? `${player.firstName} ${demoNumber}` : player.firstName
  }

  return `${player.firstName} ${player.lastName}`.trim() || player.nickname?.trim() || `Jugador ${player.id}`
}

type MenuPosition = { style: CSSProperties; opensUp: boolean }

export default function PreferredPlayerPicker({ homeTeam, awayTeam, homePlayers, awayPlayers, value, onChange, onSelectionComplete, ariaLabel }: Props) {
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const [activeOption, setActiveOption] = useState(-1)
  const [menuPosition, setMenuPosition] = useState<MenuPosition | null>(null)
  const pickerRef = useRef<HTMLDivElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const menuId = useId()
  const all = useMemo(() => [...homePlayers, ...awayPlayers], [awayPlayers, homePlayers])
  const selected = all.find(player => String(player.id) === value)
  const filter = normalize(query)
  const groups = useMemo(() => [
    { name: homeTeam, players: homePlayers.filter(player => normalize(`${preferredPlayerLabel(player)} ${player.nickname ?? ''} ${player.position}`).includes(filter)) },
    { name: awayTeam, players: awayPlayers.filter(player => normalize(`${preferredPlayerLabel(player)} ${player.nickname ?? ''} ${player.position}`).includes(filter)) },
  ], [awayPlayers, awayTeam, filter, homePlayers, homeTeam])
  const visiblePlayers = useMemo(() => groups.flatMap(group => group.players), [groups])

  useEffect(() => {
    if (!open) {
      setMenuPosition(null)
      return
    }

    const positionMenu = () => {
      const input = inputRef.current
      if (!input) return
      const rect = input.getBoundingClientRect()
      const viewportPadding = 8
      const gap = 6
      const availableBelow = window.innerHeight - rect.bottom - viewportPadding - gap
      const availableAbove = rect.top - viewportPadding - gap
      const opensUp = availableBelow < 180 && availableAbove > availableBelow
      const available = opensUp ? availableAbove : availableBelow
      const maxHeight = Math.max(110, Math.min(320, available))
      const width = Math.min(rect.width, window.innerWidth - viewportPadding * 2)
      const left = Math.min(Math.max(viewportPadding, rect.left), window.innerWidth - viewportPadding - width)
      const style: CSSProperties = { left, width, maxHeight }
      if (opensUp) style.bottom = window.innerHeight - rect.top + gap
      else style.top = rect.bottom + gap
      setMenuPosition({ style, opensUp })
    }

    positionMenu()
    window.addEventListener('resize', positionMenu)
    window.addEventListener('scroll', positionMenu, true)
    return () => {
      window.removeEventListener('resize', positionMenu)
      window.removeEventListener('scroll', positionMenu, true)
    }
  }, [open])

  useEffect(() => {
    if (!open) return
    const closeOnOutsideClick = (event: PointerEvent) => {
      const target = event.target as Node
      if (!pickerRef.current?.contains(target) && !menuRef.current?.contains(target)) setOpen(false)
    }
    document.addEventListener('pointerdown', closeOnOutsideClick)
    return () => document.removeEventListener('pointerdown', closeOnOutsideClick)
  }, [open])

  if (!all.length) return <span className="preferred-picker__empty">No hay jugadores disponibles para las posiciones habilitadas.</span>

  const choose = (nextValue: string) => {
    onChange(nextValue)
    setQuery('')
    setOpen(false)
    setActiveOption(-1)
    onSelectionComplete?.()
  }

  const handleInputKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Escape') {
      setOpen(false)
      setActiveOption(-1)
      return
    }
    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault()
      setOpen(true)
      const optionCount = visiblePlayers.length + 1
      setActiveOption(current => {
        if (event.key === 'ArrowDown') return current < optionCount - 1 ? current + 1 : 0
        return current > 0 ? current - 1 : optionCount - 1
      })
      return
    }
    if (event.key !== 'Enter') return

    event.preventDefault()
    if (open && activeOption === 0) {
      choose('')
      return
    }
    const activePlayer = open && activeOption > 0 ? visiblePlayers[activeOption - 1] : null
    if (activePlayer) {
      choose(String(activePlayer.id))
      return
    }
    if (query.trim() === '' || visiblePlayers.length === 0) {
      setOpen(false)
      onSelectionComplete?.()
    }
  }

  const menu = open && menuPosition && createPortal(
    <div ref={menuRef} id={menuId} role="listbox" aria-label="Jugadores disponibles" className="preferred-picker__menu" style={menuPosition.style}>
      <button type="button" role="option" aria-selected={activeOption === 0 || (!value && activeOption < 0)} className="preferred-picker__none" onPointerDown={event => event.preventDefault()} onClick={() => choose('')}>Sin Jugador Preferido</button>
      {groups.map(group => group.players.length > 0 && (
        <section key={group.name} aria-label={group.name}>
          <strong>{group.name}</strong>
          {group.players.map(player => (
            <button type="button" role="option" aria-selected={activeOption === visiblePlayers.indexOf(player) + 1 || (activeOption < 0 && String(player.id) === value)} key={player.id} onPointerDown={event => event.preventDefault()} onClick={() => choose(String(player.id))}>
              <span>{preferredPlayerLabel(player)}</span>
              <small>{player.position}</small>
            </button>
          ))}
        </section>
      ))}
      {groups.every(group => !group.players.length) && <span className="preferred-picker__no-results">No se encontraron jugadores.</span>}
    </div>,
    document.body,
  )

  return (
    <div className="preferred-picker" ref={pickerRef}>
      {selected && (
        <div className="preferred-picker__selected">
          <span><small>Actual</small>{preferredPlayerLabel(selected)}</span>
          <button type="button" onClick={() => choose('')}>Quitar</button>
        </div>
      )}
      <input ref={inputRef} data-preferred-player-input type="search" value={query} placeholder={selected ? 'Cambiar jugador...' : 'Buscar jugador...'} aria-label={ariaLabel ?? 'Buscar Jugador Preferido'} aria-controls={open ? menuId : undefined} aria-expanded={open} aria-haspopup="listbox" autoComplete="off" onFocus={() => setOpen(true)} onBlur={event => { if (!menuRef.current?.contains(event.relatedTarget as Node | null)) setOpen(false) }} onChange={event => { setQuery(event.target.value); setOpen(true); setActiveOption(event.target.value.trim() ? 1 : -1) }} onKeyDown={handleInputKeyDown} />
      {menu}
    </div>
  )
}
