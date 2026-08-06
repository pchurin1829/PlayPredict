# ROADMAP_PRONOSTICOS_v1.0
## PlayPredict
Versión 1.0

---

# Objetivo

Este documento define el orden de implementación del Motor de Pronósticos.

No describe detalles técnicos.

Define únicamente qué funcionalidades se incorporarán en cada etapa del producto.

El objetivo es mantener un crecimiento ordenado, evitando implementar características avanzadas antes de consolidar las bases del sistema.

---

# Nota de vigencia (Sprint 8.5)

La numeración de Sprints de este documento es la propuesta **original** del roadmap y ya no coincide con la numeración real de ejecución (por ejemplo, el Sprint 8 realmente ejecutado fue "Gestión de Experiencias", no "Configuración de Competencias" como indica este documento más abajo).

A partir del Sprint 8.5 — Ligas y Experiencia de Usuario, el modelo conceptual vigente es `docs/arquitectura/PLAYPREDICT_MODELO_CONCEPTUAL_v2.0.md`. Ese documento incorpora **Liga** (creada libremente por cualquier Jugador sobre una Competencia Oficial, sin duplicar Fixture ni Resultados) como concepto principal nuevo, y simplifica los roles a `ADMIN`/`PLAYER`. El Sprint 11 de este roadmap ("Grupos Privados") queda absorbido conceptualmente por Liga; se mantiene el resto de los Sprints propuestos como referencia de largo plazo, sujetos a reordenamiento según prioridad real del negocio.

---

# Estado actual

## Infraestructura

✅ Docker

✅ PostgreSQL

✅ Backend .NET

✅ Frontend React

✅ Autenticación

---

## Administración

✅ Competencias

✅ Ediciones

✅ Fechas

✅ Partidos

✅ Resultados Oficiales

---

## Usuarios

✅ Registro

✅ Login

✅ Perfil

✅ Roles

---

## Pronósticos

✅ Pronóstico por Partido

✅ Edición

✅ Persistencia

✅ Restricciones de edición

---

# Sprint 5
## Motor de Puntuación

Objetivo

Transformar un pronóstico en puntos.

Implementar

- Motor de Evaluación.
- Reglas configurables.
- Resultado Exacto.
- Ganador Correcto.
- Auditoría del cálculo.

No implementar

- Rankings.
- Premios.
- Bonificaciones.

Resultado esperado

El sistema ya puede calcular automáticamente cuántos puntos obtuvo un usuario.

---

# Sprint 6
## Ranking General

Objetivo

Mostrar posiciones.

Implementar

- Ranking General.
- Ranking por Fecha.
- Ranking por Edición.
- Ranking histórico.

Empates.

Cantidad de resultados exactos.

No implementar

Premios.

---

# Sprint 7
## Premios

Objetivo

Permitir múltiples premios.

Ejemplos

Primer puesto.

Segundo puesto.

Tercer puesto.

Ganador mensual.

Ganador de la Fecha.

Premio sorpresa.

Mayor cantidad de resultados exactos.

No entregar todavía beneficios económicos.

Solo infraestructura.

---

# Sprint 8
## Configuración de Competencias

Objetivo

Que el administrador pueda configurar completamente una competencia.

Implementar

Tipos habilitados.

Puntajes.

Bonificaciones.

Premios.

Fechas de cierre.

Orden de visualización.

---

# Sprint 9
## Tipos de Pronósticos

Implementar

Ganador.

Ambos convierten.

Cantidad de goles.

Primer goleador.

Jugador expulsado.

Campeón.

Subcampeón.

Goleador del torneo.

MVP.

---

# Sprint 10
## Bonificaciones

Implementar

Multiplicadores.

Comodines.

Fecha doble.

Partido especial.

Bonus por participación.

Bonus por rachas.

---

# Sprint 11
## Grupos Privados

Implementar

Crear Grupo.

Código de invitación.

Ranking propio.

Administrador.

Configuración propia.

Todos utilizando los mismos partidos oficiales.

---

# Sprint 12
## Estadísticas

Implementar

Porcentaje de aciertos.

Resultados exactos.

Historial.

Rachas.

Gráficos.

Comparativas.

---

# Sprint 13
## Empresas

Implementar

Empresas.

Organizadores.

Sponsors.

Publicidad.

Campañas.

Personalización.

---

# Sprint 14
## Motor Comercial

Implementar

Campañas.

Premios.

Períodos.

Múltiples competencias simultáneas.

Competencias patrocinadas.

---

# Sprint 15
## Gamificación

Implementar

Logros.

Insignias.

Niveles.

Misiones.

Desafíos.

Experiencia.

---

# Sprint 16
## Inteligencia Artificial

Implementar

Predicciones sugeridas.

Probabilidades.

Estadísticas avanzadas.

Explicaciones.

Análisis histórico.

---

# Sprint 17
## Deportes

Expandir el motor para:

Básquet.

Vóley.

Rugby.

Tenis.

Fórmula 1.

eSports.

Sin modificar la arquitectura.

---

# Sprint 18
## Plataforma

Convertir PlayPredict en una plataforma completamente configurable.

Cada cliente podrá crear su propia experiencia de juego.

Sin desarrollo adicional.

Solo configuración.

---

# Criterios de Prioridad

Siempre implementar primero:

1. Arquitectura.
2. Motor.
3. Configuración.
4. Experiencia del usuario.
5. Automatización.
6. Funcionalidades avanzadas.

Nunca al revés.

---

# Principio del Producto

PlayPredict crecerá por capas.

Cada Sprint deberá dejar un producto funcionando y utilizable.

No se implementarán funcionalidades parcialmente terminadas.

Cada versión deberá poder ponerse en producción.

---

# Visión Final

PlayPredict no será únicamente un Prode.

Será una plataforma de creación de experiencias deportivas configurables.

Cada empresa podrá construir su propio juego, sus propias reglas, sus propios premios y su propia identidad utilizando el mismo Motor de Pronósticos.