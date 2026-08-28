export const TIE_BREAK_POLICY = {
  title: 'POLÍTICA DE DESEMPATE',
  rankingExplanation: 'El Ranking utiliza posiciones compartidas. Los jugadores con el mismo puntaje ocupan la misma posición.',
  viewExplanation: [
    'Ranking: muestra una posición compartida determinada únicamente por los puntos.',
    'Posiciones para premios: construye un orden individual aplicando los criterios automáticos de desempate.',
  ],
  example: ['100 puntos → 1°', '100 puntos → 1°', '50 puntos → 2°'],
  prizeOrderExplanation: 'Cuando sea necesario determinar un orden individual para la asignación de premios, se aplicarán criterios de desempate.',
  rules: [
    'Mayor cantidad de resultados exactos.',
    'Mayor cantidad de resultados correctos.',
    'Menor error acumulado en los marcadores pronosticados.',
    'Mayor cantidad de puntos obtenidos por Jugador Preferido, cuando esta modalidad se encuentre habilitada.',
    'Desafío de desempate, si la competencia lo establece.',
    'Sorteo entre los participantes que continúen empatados, como criterio final cuando corresponda.',
  ],
  clarification: 'Los criterios de desempate para premios no modifican la posición obtenida en el Ranking.',
  clarificationExample: 'Dos jugadores pueden continuar figurando ambos como 1° en el Ranking aunque uno quede por delante del otro en el futuro Orden de Premios.',
} as const
