# MOTOR_PUNTUACION_v1.0
## PlayPredict
Versión 1.0

---

# 1. Objetivo

Este documento define el funcionamiento conceptual del Motor de Puntuación de PlayPredict.

No define:

- tablas;
- endpoints;
- pantallas;
- implementación técnica.

Su objetivo es establecer cómo PlayPredict transforma un pronóstico en puntos.

---

# 2. Filosofía

El Motor de Puntuación nunca debe conocer reglas deportivas.

Tampoco debe conocer reglas comerciales.

Su única responsabilidad será:

> Comparar un pronóstico contra un resultado oficial utilizando una regla configurada y devolver una cantidad de puntos.

---

# 3. Principio Fundamental

El Motor nunca tendrá valores "hardcodeados".

Ejemplos incorrectos:

```text
Resultado exacto = 6

Ganador correcto = 3
```

Eso pertenece a la configuración de cada competencia.

---

# 4. Componentes

Todo cálculo necesita cinco elementos.

## 1. Tipo de Pronóstico

Ejemplo

Resultado Exacto

---

## 2. Pronóstico del Usuario

Ejemplo

```text
2 - 1
```

---

## 3. Resultado Oficial

Ejemplo

```text
2 - 1
```

---

## 4. Regla de Puntuación

Ejemplo

```text
Exacto

6 puntos
```

---

## 5. Resultado del Motor

Ejemplo

```text
6 puntos
```

---

# 5. Flujo del Motor

```text
Pronóstico

↓

Resultado Oficial

↓

Buscar Regla

↓

Evaluar

↓

Calcular

↓

Registrar Puntaje

↓

Actualizar Ranking
```

---

# 6. Tipos de Reglas

## 6.1 Exactitud

Ejemplo

```text
Pronóstico

2-1

Resultado

2-1

Puntos

6
```

---

## 6.2 Acierto parcial

Ejemplo

```text
Pronóstico

2-1

Resultado

3-2

Ganó Local

Puntos

3
```

---

## 6.3 Sin acierto

```text
0 puntos
```

---

## 6.4 Cantidad

Ejemplo

Cantidad de goles.

Puede premiarse:

- exacto;
- aproximación;
- rango.

---

## 6.5 Selección

Ejemplo

Primer goleador.

Respuesta:

Jugador.

---

## 6.6 Respuesta múltiple

Ejemplo

Clasificados.

Puede otorgar puntos por cada acierto.

---

## 6.7 Acumulativa

Ejemplo

Jugador elegido.

Cada gol vale:

```text
2 puntos
```

Si convierte tres goles:

```text
6 puntos
```

---

# 7. Configuración por Competencia

Cada Edición podrá definir sus reglas.

Ejemplo

Liga Profesional

```text
Resultado exacto

6

Ganador

3
```

---

Champions

```text
Resultado exacto

10

Ganador

5
```

---

Empresa XYZ

```text
Resultado exacto

100

Ganador

0
```

El motor no necesita modificarse.

---

# 8. Prioridad de Reglas

Puede ocurrir que varias reglas sean verdaderas.

Ejemplo

```text
Resultado Exacto

↓

Ganador Correcto

↓

Empate Correcto
```

Solo debe aplicarse la de mayor prioridad.

La prioridad será configurable.

---

# 9. Bonificaciones

Las bonificaciones no son reglas principales.

Se aplican después.

Ejemplos

```text
Fecha doble.

x2
```

---

```text
Partido destacado.

x3
```

---

```text
Comodín.

+5
```

---

```text
Participación perfecta.

+20
```

---

# 10. Multiplicadores

Los multiplicadores deberán poder configurarse.

Ejemplo

```text
Resultado

6

Multiplicador

x2

Total

12
```

---

# 11. Descuentos

También podrán existir penalizaciones.

Ejemplo

```text
Pronóstico inválido

-2
```

No será utilizado en el MVP.

---

# 12. Reglas futuras

El motor deberá permitir incorporar:

- porcentajes;
- probabilidades;
- cuotas;
- dificultad;
- peso del partido.

Sin rediseñar su arquitectura.

---

# 13. Evaluación de un Partido

Ejemplo

Resultado Oficial

```text
2 - 1
```

Usuario

```text
2 - 1
```

Evaluación

```text
Exacto

6 puntos
```

---

Otro Usuario

```text
3 - 2
```

Evaluación

```text
Ganador correcto

3 puntos
```

---

Otro Usuario

```text
1 - 2
```

Evaluación

```text
Incorrecto

0 puntos
```

---

# 14. Acumulación

Los puntos obtenidos podrán acumularse por:

- partido;
- fecha;
- fase;
- edición;
- temporada;
- histórico.

El motor no realiza rankings.

Solo entrega puntos.

---

# 15. Corrección de Resultados

Si un resultado oficial cambia:

Ejemplo

```text
2-1

↓

2-2
```

Todos los puntajes afectados deberán recalcularse automáticamente.

Nunca deberán editarse manualmente.

---

# 16. Auditoría

Cada cálculo deberá poder reconstruirse.

Ejemplo

```text
Usuario

Juan

Pronóstico

2-1

Resultado

2-1

Regla aplicada

Resultado Exacto

Valor

6

Bonificación

0

Total

6
```

Esto permitirá explicar cualquier reclamo.

---

# 17. Ranking

El Motor de Puntuación no genera posiciones.

Solo informa:

```text
Usuario

↓

Puntos obtenidos
```

El Ranking será responsabilidad de otro componente.

---

# 18. MVP Inicial

La primera implementación del motor utilizará únicamente dos reglas.

## Regla 1

Resultado Exacto

Valor configurable.

---

## Regla 2

Ganador Correcto

Valor configurable.

---

No habrá todavía:

- goleadores;
- expulsados;
- MVP;
- campeones;
- bonificaciones;
- multiplicadores.

Todo eso quedará preparado por arquitectura.

---

# 19. Evolución prevista

Versión 1

Resultado Exacto.

---

Versión 2

Resultado Correcto.

---

Versión 3

Goleadores.

---

Versión 4

Bonificaciones.

---

Versión 5

Multiplicadores.

---

Versión 6

Reglas personalizadas.

---

# 20. Principio de Arquitectura

El Motor de Puntuación nunca deberá contener reglas específicas de un torneo.

Toda decisión deberá provenir de la configuración de la competencia.

De esta manera PlayPredict podrá adaptarse a cualquier modalidad deportiva o comercial sin modificar el código fuente.

---

# 21. Decisión aprobada

PlayPredict separará definitivamente cinco conceptos independientes:

1. Tipo de Pronóstico.
2. Pronóstico realizado por el Usuario.
3. Resultado Oficial.
4. Regla de Puntuación.
5. Puntaje obtenido.

Aunque normalmente intervienen juntos, representan responsabilidades diferentes y deberán mantenerse desacoplados para permitir la evolución futura del sistema.