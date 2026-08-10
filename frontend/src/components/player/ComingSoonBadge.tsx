interface ComingSoonBadgeProps {
  label?: string
}

export default function ComingSoonBadge({ label = 'Próximamente' }: ComingSoonBadgeProps) {
  return (
    <span className="coming-soon-badge">{label}</span>
  )
}
