# REGLAS DE NEGOCIO
## PlayPredict
Versión 1.0

---

# Objetivo

Este documento define las reglas funcionales del MVP.

No describe la implementación técnica.

Describe únicamente el comportamiento esperado del sistema.

---

# 1. Competencias Oficiales

Las Competencias Oficiales son creadas únicamente por los Administradores.

Una Competencia Oficial posee:

- Nombre
- Descripción
- Deporte
- Fixture
- Partidos
- Estado

Los usuarios no pueden crear Competencias Oficiales.

---

# 2. Partidos

Cada Partido pertenece a una única Competencia Oficial.

Todo Partido posee:

- Fecha
- Hora
- Participantes
- Resultado Oficial
- Estado

Estados posibles

- Programado
- En juego
- Finalizado
- Suspendido
- Cancelado

---

# 3. Pronósticos

Todo Usuario puede realizar un Pronóstico sobre un Partido.

El Pronóstico incluye:

- Ganador
- Empate (si corresponde)
- Marcador

El Usuario podrá modificar el Pronóstico únicamente mientras el Partido permanezca abierto.

Una vez iniciado el Partido el Pronóstico queda bloqueado.

---

# 4. Resultados Oficiales

Los Resultados Oficiales únicamente podrán ser cargados por los Administradores.

Una vez publicado un Resultado Oficial:

- se recalculan automáticamente los puntos
- se actualizan los Rankings
- se actualizan las posiciones de todas las Ligas

---

# 5. Ligas

Una Liga representa un grupo de Usuarios.

Puede ser:

- Oficial
- Privada

Las Ligas Privadas son creadas por cualquier Usuario.

Toda Liga utiliza una única Competencia Oficial.

Nunca posee Partidos propios.

---

# 6. Invitaciones

El creador de una Liga podrá invitar otros Usuarios.

Los invitados deberán aceptar la invitación para participar.

Un Usuario podrá pertenecer a múltiples Ligas simultáneamente.

---

# 7. Rankings

Cada Liga posee su propio Ranking.

Todos los Rankings utilizan los mismos Resultados Oficiales.

Los Rankings son independientes entre sí.

Un Usuario podrá ocupar posiciones distintas según la Liga.

---

# 8. Premios

Los Premios podrán asociarse a:

- una Competencia
- una Liga
- una Fecha
- un Período
- un Evento Especial

Ejemplos

Premio Fecha 3

Premio Agosto

Premio Clausura

Premio Anual

---

# 9. Puntuación

El sistema permitirá definir el método de puntuación.

Inicialmente existirá un único sistema oficial.

En futuras versiones podrán existir distintos sistemas de puntuación.

---

# 10. Empates

Si dos o más Usuarios obtienen el mismo puntaje:

el sistema utilizará los criterios de desempate configurados.

Los criterios definitivos se establecerán antes del desarrollo del Motor de Ranking.

---

# 11. Cambios de horario

Si un Partido modifica su fecha u horario antes de comenzar:

los Pronósticos continúan siendo válidos.

Si el Partido ya comenzó:

los Pronósticos permanecen bloqueados.

---

# 12. Partidos suspendidos

Los Partidos Suspendidos conservarán los Pronósticos realizados.

Cuando exista un Resultado Oficial:

los puntos serán recalculados automáticamente.

---

# 13. Seguridad

Cada Usuario únicamente podrá:

- modificar sus propios Pronósticos
- administrar sus propias Ligas

Los Administradores podrán administrar toda la plataforma.

---

# Evolución del Motor de Pronósticos

El MVP implementará un único tipo de pronóstico:

- Marcador Exacto.

Sin embargo, el motor será diseñado para soportar distintos tipos de preguntas en futuras versiones.

Ejemplos:

- Marcador Exacto.
- Ganador.
- Campeón del Torneo.
- Goleador.
- MVP.
- Podio.
- Cantidad de goles.
- Primer goleador.
- Cualquier otro evento pronosticable.

De esta manera el crecimiento futuro no requerirá rediseñar el modelo de datos principal.

---

# Reglas del MVP

✔ Una única forma de puntuación.

✔ Un único idioma.

✔ Sin notificaciones Push.

✔ Sin Chat.

✔ Sin Comentarios.

✔ Sin Estadísticas Avanzadas.

✔ Sin Multiempresa.

Todo lo anterior quedará para versiones posteriores.