import './CurrentRoundCard.css'

interface CurrentRoundCardProps {
  roundName?: string | null
  competitionName?: string | null
  currentRound?: number | null
  totalRounds?: number | null
}

export default function CurrentRoundCard({
  roundName,
  competitionName,
  currentRound,
  totalRounds,
}: CurrentRoundCardProps) {
  const hasData = roundName || currentRound
  const progress =
    currentRound && totalRounds ? Math.round((currentRound / totalRounds) * 100) : 0

  return (
    <div className="crcard">
      <h3 className="crcard__title">Fecha actual</h3>
      {hasData ? (
        <>
          <p className="crcard__round">
            {roundName ?? `Fecha ${currentRound}`}
            {totalRounds && currentRound && (
              <span className="crcard__progress-text">
                {' '}de {totalRounds}
              </span>
            )}
          </p>
          {competitionName && (
            <p className="crcard__competition">{competitionName}</p>
          )}
          {totalRounds && currentRound && (
            <div className="crcard__bar-wrap">
              <div className="crcard__bar" style={{ width: `${progress}%` }} />
            </div>
          )}
        </>
      ) : (
        <p className="crcard__empty">Sin fecha activa</p>
      )}
    </div>
  )
}
