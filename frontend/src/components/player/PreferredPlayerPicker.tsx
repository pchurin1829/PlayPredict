import { useMemo, useState } from 'react'
import type { AvailablePlayer } from '../../api/types'
import './PreferredPlayerPicker.css'

interface Props { homeTeam:string; awayTeam:string; homePlayers:AvailablePlayer[]; awayPlayers:AvailablePlayer[]; value:string; onChange:(value:string)=>void; ariaLabel?:string }
const normalize = (value:string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase()
export const preferredPlayerLabel = (player:AvailablePlayer) => `${player.firstName} ${player.lastName}${player.nickname ? ` · “${player.nickname}”` : ''}${player.shirtNumber == null ? '' : ` · #${player.shirtNumber}`}`.trim()

export default function PreferredPlayerPicker({homeTeam,awayTeam,homePlayers,awayPlayers,value,onChange,ariaLabel}:Props) {
  const [query,setQuery]=useState(''); const [open,setOpen]=useState(false)
  const all=[...homePlayers,...awayPlayers]; const selected=all.find(player=>String(player.id)===value); const filter=normalize(query)
  const groups=useMemo(()=>[{name:homeTeam,players:homePlayers.filter(p=>normalize(preferredPlayerLabel(p)).includes(filter))},{name:awayTeam,players:awayPlayers.filter(p=>normalize(preferredPlayerLabel(p)).includes(filter))}],[awayPlayers,awayTeam,filter,homePlayers,homeTeam])
  if(!all.length) return <span className="preferred-picker__empty">No hay jugadores disponibles para las posiciones habilitadas.</span>
  return <div className="preferred-picker">
    {selected&&<div className="preferred-picker__selected"><span>{preferredPlayerLabel(selected)}</span><button type="button" onClick={()=>{onChange('');setQuery('')}}>Quitar</button></div>}
    <input type="search" value={query} placeholder={selected?'Cambiar jugador...':'Buscar jugador...'} aria-label={ariaLabel??'Buscar Jugador Preferido'} autoComplete="off" onFocus={()=>setOpen(true)} onChange={e=>{setQuery(e.target.value);setOpen(true)}} onBlur={()=>window.setTimeout(()=>setOpen(false),150)}/>
    {open&&<div className="preferred-picker__menu">
      {!selected&&<button type="button" className="preferred-picker__none" onMouseDown={e=>e.preventDefault()} onClick={()=>{onChange('');setQuery('');setOpen(false)}}>Sin Jugador Preferido</button>}
      {groups.map(group=>group.players.length>0&&<section key={group.name}><strong>{group.name}</strong>{group.players.map(player=><button type="button" key={player.id} onMouseDown={e=>e.preventDefault()} onClick={()=>{onChange(String(player.id));setQuery('');setOpen(false)}}><span>{preferredPlayerLabel(player)}</span><small>{player.position}</small></button>)}</section>)}
      {groups.every(group=>!group.players.length)&&<span className="preferred-picker__no-results">No se encontraron jugadores.</span>}
    </div>}
  </div>
}
