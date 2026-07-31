# MODELO DE DATOS
## PlayPredict
Versión 1.0

---

# Objetivo

Este documento define el modelo de datos funcional del MVP.

No describe tablas SQL ni tipos de datos.

Describe las entidades del negocio y cómo se relacionan.

Una vez aprobado, servirá como base para generar la Base de Datos PostgreSQL.

---

# 1. Usuario

Representa una persona registrada en la plataforma.

Puede:

- participar en Competencias Oficiales
- crear Ligas Privadas
- unirse a Ligas Privadas
- realizar Pronósticos

---

# 2. Competencia Oficial

Representa una competencia administrada por PlayPredict.

Ejemplos:

- Liga Profesional
- Libertadores
- Mundial
- NBA
- Fórmula 1

Una Competencia Oficial posee:

- Fixture
- Partidos
- Resultados oficiales
- Premios oficiales (opcional)

Los usuarios NO pueden crear Competencias Oficiales.

---

# 3. Partido

Representa un evento sobre el cual pueden realizarse pronósticos.

Ejemplos

Boca vs River

Argentina vs Brasil

Ferrari vs McLaren

Etapa Final de un Reality

Un Partido pertenece a una única Competencia Oficial.

---

# 4. Pronóstico

Representa la predicción realizada por un Usuario sobre un Partido.

Contiene:

- resultado elegido
- marcador
- fecha y hora del pronóstico

Una vez iniciado el Partido deja de poder modificarse.

---

# 5. Liga

Representa un grupo de personas que compiten entre sí.

Puede ser:

- General
- Oficial
- Privada

Ejemplos

Liga General

Los Amigos

Familia Pérez

Oficina Comercial

Cada Liga utiliza una única Competencia Oficial.

No posee partidos propios.

Utiliza siempre los partidos oficiales.

---

# 6. Miembros de Liga

Relaciona Usuarios con una Liga.

Un Usuario puede pertenecer a muchas Ligas.

Una Liga posee muchos Usuarios.

---

# 7. Ranking

Representa la posición de un Usuario dentro de una Liga.

Se calcula automáticamente.

No depende de otras Ligas.

---

# 8. Premio

Representa un premio asociado a una Competencia o Liga.

Ejemplos

Premio Anual

Premio Mensual

Premio Fecha

Premio Especial

Puede existir cualquier cantidad de premios.

---

# Relaciones

Competencia Oficial

↓

Partidos

↓

Pronósticos

↓

Usuario

----------------------------

Competencia Oficial

↓

Liga

↓

Usuarios

↓

Ranking

----------------------------

Competencia Oficial

↓

Premios

---

# Principios de diseño

## 1

Los Partidos existen una única vez.

Nunca se duplican.

---

## 2

Los Resultados Oficiales existen una única vez.

---

## 3

Las Ligas solamente agrupan Usuarios.

No crean Partidos.

---

## 4

Un Usuario puede participar simultáneamente en muchas Ligas.

---

## 5

Una Liga siempre utiliza una Competencia Oficial.

---

## 6

Los Rankings se calculan independientemente para cada Liga.

---

## Fuera del MVP

No forman parte de esta versión:

- Chat
- Comentarios
- Reacciones
- Estadísticas avanzadas
- Notificaciones Push
- Integraciones
- Multiempresa