interface TeamBadgeProps {
  name: string
  size?: number
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/)
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

function hashColor(name: string): string {
  let hash = 0
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash)
  }
  const hue = Math.abs(hash) % 360
  return `hsl(${hue}, 55%, 45%)`
}

export default function TeamBadge({ name, size = 40 }: TeamBadgeProps) {
  return (
    <div
      className="team-badge"
      style={{
        width: size,
        height: size,
        background: hashColor(name),
        fontSize: size * 0.36,
      }}
    >
      {getInitials(name)}
    </div>
  )
}
