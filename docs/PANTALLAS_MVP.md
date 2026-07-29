# PANTALLAS MVP
## PlayPredict
Versión 1.0

---

# Objetivo

Este documento define todas las pantallas necesarias para el MVP.

No describe diseño gráfico.

No describe implementación.

Únicamente define la navegación y la funcionalidad de cada pantalla.

---

# 1. Inicio

Es la pantalla principal.

Desde aquí el usuario podrá:

- Ver Competencias Oficiales activas.
- Ver sus Ligas.
- Ver próximas fechas.
- Ver noticias o promociones.
- Acceder a su Perfil.

Botones

- Competencias
- Mis Ligas
- Premios
- Perfil

---

# 2. Registro

Permite crear una nueva cuenta.

Datos mínimos

- Nombre
- Email
- Contraseña

---

# 3. Login

Permite ingresar al sistema.

Funciones

- Iniciar sesión
- Recuperar contraseña

---

# 4. Competencias Oficiales

Lista todas las Competencias disponibles.

Ejemplos

Liga Profesional

Libertadores

Mundial

NBA

Fórmula 1

Al ingresar se muestran:

- próximas fechas
- partidos
- premios
- rankings

---

# 5. Detalle de Competencia

Muestra:

- Fixture
- Próximos Partidos
- Partidos Finalizados
- Ranking General
- Premios

Desde aquí el usuario podrá realizar Pronósticos.

---

# 6. Mis Pronósticos

Lista todos los Pronósticos realizados.

Debe permitir:

- consultar
- modificar (si el Partido aún no comenzó)

---

# 7. Mis Ligas

Lista todas las Ligas donde participa el Usuario.

Ejemplo

Liga General

Los Amigos

La Oficina

Familia

Cada Liga muestra:

- posición
- puntos
- cantidad de participantes

---

# 8. Crear Liga

Permite crear una Liga Privada.

Datos

Nombre

Descripción

Competencia Oficial asociada

Luego permitirá invitar participantes.

---

# 9. Liga

Pantalla principal de una Liga.

Muestra

- integrantes
- ranking
- premios
- próximas fechas

---

# 10. Invitar Participantes

Permite incorporar nuevos Usuarios a una Liga.

Inicialmente mediante Email.

En futuras versiones podrá utilizar enlaces o códigos.

---

# 11. Premios

Muestra los premios disponibles.

Ejemplos

Premio Fecha

Premio Mensual

Premio Anual

Campaña Especial

Debe indicar:

- descripción
- condiciones
- vigencia

---

# 12. Perfil

Permite administrar:

- datos personales
- contraseña
- notificaciones (futuro)

---

# 13. Panel Administrador

Pantalla principal del Administrador.

Acceso únicamente para Administradores.

Opciones

Competencias

Partidos

Resultados

Premios

Usuarios

Ligas

---

# 14. Administrar Competencias

Permite:

- crear
- modificar
- eliminar

Competencias Oficiales.

---

# 15. Administrar Partidos

Permite administrar:

Fixture

Fechas

Horarios

Participantes

Estado

---

# 16. Administrar Resultados

Permite ingresar el Resultado Oficial.

Al confirmar:

- recalcula Pronósticos
- recalcula Rankings
- actualiza posiciones

---

# 17. Administrar Premios

Permite crear:

Premios Anuales

Premios Mensuales

Premios por Fecha

Premios Especiales

---

# Flujo principal

Inicio

↓

Competencia

↓

Pronóstico

↓

Esperar resultado

↓

Ranking

↓

Premios

---

# Flujo Liga Privada

Crear Liga

↓

Invitar Amigos

↓

Todos Pronostican

↓

Ranking de la Liga

↓

Premios

---

# Fuera del MVP

No forman parte de esta versión:

- Chat

- Mensajes

- Push Notifications

- Estadísticas avanzadas

- Historial completo

- Multiempresa

- Personalización visual

- Integraciones externas