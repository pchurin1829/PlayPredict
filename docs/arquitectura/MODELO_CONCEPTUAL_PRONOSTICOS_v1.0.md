# MODELO_CONCEPTUAL_PRONOSTICOS_v1.0

**Proyecto:** PlayPredict  
**Versión:** 1.0  
**Estado:** Diseño Conceptual  
**Fecha:** Julio 2026

---

# 1. Objetivo

Este documento define el modelo conceptual del Motor de Pronósticos de PlayPredict.

No describe cómo será implementado técnicamente.

Describe **qué conceptos existen**, cómo se relacionan y cuáles son las reglas generales que deberá respetar cualquier implementación futura.

La filosofía principal es:

> **PlayPredict no implementa un Prode. Implementa un Motor de Pronósticos configurable.**

Las reglas nunca deben quedar codificadas ("hardcodeadas") dentro del software.

Cada competencia podrá definir completamente su propio sistema de pronósticos.

---

# 2. Filosofía del sistema

El objetivo no es solamente adivinar resultados.

El objetivo es generar participación continua durante toda una competencia.

El sistema debe permitir construir experiencias muy diferentes entre sí.

Ejemplos:

- Prode clásico.
- Fantasy League.
- Quiniela deportiva.
- Torneos de empresas.
- Campeonatos internos.
- Promociones comerciales.
- Juegos de sponsors.
- Concursos periodísticos.
- Eventos especiales.

Todo utilizando exactamente el mismo motor.

---

# 3. Principios de Diseño

## Configurable

Nada relacionado con puntajes deberá depender del código.

---

## Escalable

Agregar nuevos tipos de pronósticos no debe requerir modificar el motor existente.

---

## Extensible

El sistema debe admitir deportes completamente distintos.

Ejemplos:

- Fútbol
- Básquet
- Rugby
- Tenis
- Vóley
- Fórmula 1
- eSports

---

## Independiente del deporte

El motor calcula puntos.

Nunca interpreta reglas deportivas.

---

# 4. Conceptos principales

## Competencia

Agrupa una o más Ediciones.

Ejemplo:

Liga Profesional

---

## Edición

Instancia concreta de una Competencia.

Ejemplo:

Liga Profesional 2026

---

## Fecha

Agrupa partidos.

---

## Partido

Evento deportivo.

Es el principal generador de resultados oficiales.

---

## Usuario

Participante del juego.

---

## Pronóstico

Predicción realizada por un usuario.

Puede referirse a:

- un partido
- una fecha
- una edición
- un torneo completo

---

# 5. Tipos de Pronósticos

El sistema debe manejar múltiples tipos.

## Resultado del partido

Ejemplo

2-1

---

## Ganador

Local

Visitante

Empate

---

## Ambos convierten

Sí

No

---

## Primer goleador

Jugador

---

## Último goleador

Jugador

---

## Goleador del partido

Jugador

---

## Goleador de la fecha

Jugador

---

## Goleador del torneo

Jugador

---

## MVP

Jugador

---

## Expulsado

Jugador

---

## Campeón

Equipo

---

## Descendido

Equipo

---

## Clasificados

Lista de equipos.

---

## Personalizados

Cada competencia podrá crear nuevos tipos.

Ejemplos

- Primer saque directo.
- Mejor jugador.
- Cantidad de córners.
- Cantidad de tarjetas.
- Duración del partido.

El motor no debe imponer límites.

---

# 6. Configuración de una Competencia

Cada edición define:

Qué pronósticos estarán disponibles.

Ejemplo

Liga Profesional

✓ Resultado

✓ Campeón

✓ Goleador

✗ MVP

✗ Expulsados

---

Champions League

✓ Resultado

✓ Ambos convierten

✓ Primer goleador

✓ Campeón

---

Mundial

✓ Resultado

✓ Goleador

✓ Campeón

✓ Bota de Oro

✓ Balón de Oro

---

# 7. Reglas de Cierre

Cada tipo de pronóstico define cuándo deja de aceptarse.

Ejemplos

Resultado

Cierra al comenzar el partido.

---

Campeón

Cierra al comenzar el torneo.

---

Goleador

Puede cerrar:

- inicio del torneo
- cierre de primera fase
- cierre de semifinales

Configurable.

---

# 8. Reglas de Puntuación

Cada tipo define su propia escala.

Ejemplo

Resultado

Resultado exacto

6 puntos

Ganador correcto

3 puntos

Empate correcto

3 puntos

Sin aciertos

0 puntos

---

Otro torneo

Resultado exacto

10 puntos

Ganador

5 puntos

---

Otro

Resultado exacto

100 puntos

No existe premio por ganador.

---

El motor nunca conocerá estas reglas.

Solo las ejecutará.

---

# 9. Motor de Evaluación

El motor responde únicamente una pregunta:

> ¿Qué regla corresponde aplicar?

Luego:

- obtiene el resultado oficial
- obtiene el pronóstico
- ejecuta la regla
- devuelve puntos

No contiene reglas deportivas.

No contiene reglas comerciales.

No contiene reglas de negocio específicas.

---

# 10. Motor de Ranking

El ranking tampoco posee reglas fijas.

Podrá calcular:

Puntos totales

---

Puntos por fecha

---

Puntos por competencia

---

Puntos por temporada

---

Puntos históricos

---

Ranking mensual

---

Ranking anual

---

Ranking por empresa

---

Ranking por grupo privado

---

Ranking por país

---

Ranking mundial

---

# 11. Bonificaciones

El sistema deberá admitir reglas adicionales.

Ejemplos

Fecha doble.

---

Partido destacado.

---

Multiplicador x2.

---

Multiplicador x3.

---

Comodín.

---

Bonus sorpresa.

---

Premio por racha.

---

Premio por asistencia.

---

Premio por participación completa.

---

Todas configurables.

---

# 12. Premios

Los premios no forman parte del cálculo.

Son una consecuencia del ranking.

Ejemplos

Dinero.

---

Productos.

---

Puntos.

---

Insignias.

---

Créditos.

---

Beneficios.

---

# 13. Separación de Responsabilidades

## Motor de Pronósticos

Administra pronósticos.

---

## Motor de Resultados

Administra resultados oficiales.

---

## Motor de Evaluación

Calcula puntos.

---

## Motor de Ranking

Genera posiciones.

---

## Motor de Premios

Entrega recompensas.

---

Cada componente podrá evolucionar independientemente.

---

# 14. Escenarios futuros

El modelo deberá permitir incorporar, sin rediseñar la arquitectura:

- IA para sugerencias de pronósticos.
- Probabilidades implícitas.
- Cuotas.
- Integración con casas de apuestas (si la legislación lo permite).
- Fantasy Sports.
- Predicciones colectivas.
- Torneos privados.
- Ligas entre amigos.
- Empresas.
- Sponsors.
- Tokens.
- NFTs.
- Gamificación.
- Logros.
- Niveles.
- Misiones.
- Desafíos diarios.
- Desafíos semanales.
- Desafíos especiales.

---

# 15. Principio Fundamental

El verdadero activo de PlayPredict no será un conjunto de pantallas.

Será su **Motor de Pronósticos Configurable**.

Mientras un Prode tradicional resuelve un único problema, PlayPredict deberá poder adaptarse a cualquier modalidad de competencia presente o futura simplemente modificando configuraciones y reglas, sin necesidad de desarrollar nuevas versiones del software.