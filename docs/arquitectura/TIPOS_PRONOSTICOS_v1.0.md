# TIPOS DE PRONÓSTICOS
## PlayPredict
Versión 1.0

---

# 1. Objetivo

Este documento define los tipos de pronósticos que PlayPredict podrá ofrecer.

No define todavía:

- tablas;
- endpoints;
- pantallas;
- puntos concretos;
- reglas de premios.

Su objetivo es clasificar qué puede predecir un usuario y qué clase de respuesta requiere cada caso.

PlayPredict deberá permitir habilitar o deshabilitar estos tipos por Edición.

---

# 2. Principio general

Un Tipo de Pronóstico define:

- qué se pregunta;
- sobre qué objeto se pregunta;
- qué clase de respuesta debe ingresar el usuario;
- cuándo se cierra;
- cómo podrá evaluarse después.

El valor de puntos no pertenece al Tipo de Pronóstico.

La puntuación será configurada por separado.

---

# 3. Ámbitos de un pronóstico

Todo pronóstico pertenece a uno de estos ámbitos.

## 3.1 Partido

Se responde para un partido específico.

Ejemplos:

- marcador exacto;
- ganador;
- primer goleador;
- jugador expulsado;
- cantidad de goles.

---

## 3.2 Fecha

Se responde para una fecha o jornada completa.

Ejemplos:

- goleador de la fecha;
- equipo con más goles;
- partido con más goles;
- cantidad total de goles de la fecha.

---

## 3.3 Fase

Se responde para una etapa de una Edición.

Ejemplos:

- ganador de Zona A;
- ganador de Zona B;
- clasificados a semifinales;
- ganador de la fase de grupos.

---

## 3.4 Edición

Se responde para el torneo completo.

Ejemplos:

- campeón;
- subcampeón;
- goleador del campeonato;
- MVP;
- equipos descendidos.

---

# 4. Clases de respuesta

## 4.1 Marcador

Dos valores numéricos.

Ejemplo:

```text
Local: 2
Visitante: 1