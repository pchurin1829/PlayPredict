import { useId } from 'react'
import { CLUB_BADGES } from '../../data/clubBadges'

interface TeamBadgeProps {
  name: string
  size?: number
}

const SHIELD_PATH =
  'M50 3 L91 16 V53 C91 84 72 101 50 113 C28 101 9 84 9 53 V16 Z'

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
  return `hsl(${hue}, 55%, 40%)`
}

export default function TeamBadge({ name, size = 40 }: TeamBadgeProps) {
  const clipId = useId()
  const club = CLUB_BADGES[name.trim()]

  const fallback = {
    abbr: getInitials(name),
    pattern: 'solid' as const,
    primary: hashColor(name),
    secondary: '#ffffff',
    textColor: '#ffffff',
  }

  const { abbr, pattern, primary, secondary, textColor } = club ?? fallback

  return (
    <svg
      width={size}
      height={size * 1.16}
      viewBox="0 0 100 116"
      className="team-badge"
      style={{ flexShrink: 0, display: 'block' }}
      aria-label={name}
      role="img"
    >
      <defs>
        <clipPath id={clipId}>
          <path d={SHIELD_PATH} />
        </clipPath>
      </defs>

      <g clipPath={`url(#${clipId})`}>
        <rect x="0" y="0" width="100" height="116" fill={primary} />

        {pattern === 'halves' && (
          <rect x="50" y="0" width="50" height="116" fill={secondary} />
        )}

        {pattern === 'sash' && (
          <polygon points="0,25 30,0 100,75 100,116 70,116" fill={secondary} />
        )}

        {pattern === 'stripesV' &&
          [1, 3].map((i) => (
            <rect key={i} x={i * 20} y="0" width="20" height="116" fill={secondary} />
          ))}

        {pattern === 'stripesH' &&
          [1, 3].map((i) => (
            <rect key={i} x="0" y={i * 20} width="100" height="20" fill={secondary} />
          ))}
      </g>

      <path
        d={SHIELD_PATH}
        fill="none"
        stroke="rgba(0,0,0,0.25)"
        strokeWidth="2"
      />

      <text
        x="50"
        y="66"
        textAnchor="middle"
        fontSize="26"
        fontWeight="800"
        fontFamily="inherit"
        fill={textColor}
        stroke="rgba(0,0,0,0.18)"
        strokeWidth="0.5"
      >
        {abbr.slice(0, 3)}
      </text>
    </svg>
  )
}
