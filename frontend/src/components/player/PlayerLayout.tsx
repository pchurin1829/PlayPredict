import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import PlayerHeader from './PlayerHeader'
import PlayerSidebar from './PlayerSidebar'
import './PlayerLayout.css'

export default function PlayerLayout() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  return (
    <div className="playout">
      <PlayerHeader />
      <div className="playout__body">
        <PlayerSidebar
          collapsed={sidebarCollapsed}
          onToggle={() => setSidebarCollapsed((v) => !v)}
        />
        <main className="playout__content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
