# CONFIGURACION_COMPETENCIAS_v1.0
## PlayPredict
Versión 1.0

---

# 1. Objetivo

Este documento define todo aquello que un Administrador podrá configurar para una Competencia.

La filosofía del sistema establece que una Competencia no posee reglas fijas.

Cada Competencia define completamente cómo será jugada.

---

# 2. Principio Fundamental

Una Competencia es una configuración.

No es solamente un conjunto de partidos.

Dos Competencias pueden utilizar exactamente los mismos partidos y generar experiencias completamente distintas.

---

# 3. Datos Generales

Cada Competencia define:

- Nombre
- Descripción
- Deporte
- Estado
- Imagen
- Color principal
- Color secundario
- Organizador
- Sponsor principal

---

# 4. Vigencia

Fecha de inicio.

Fecha de cierre.

Zona horaria.

Estado.

---

# 5. Tipos de Pronóstico habilitados

El Administrador podrá decidir cuáles estarán disponibles.

Ejemplo

✓ Resultado Exacto

✓ Ganador

✓ Goleador

✓ Campeón

✗ MVP

✗ Expulsado

---

# 6. Puntajes

Cada Tipo tendrá su configuración.

Ejemplo

Resultado Exacto

6 puntos.

Ganador

3 puntos.

Goleador

2 puntos por gol.

Campeón

100 puntos.

---

# 7. Prioridades

Cuando varias reglas coincidan.

Ejemplo

Resultado Exacto.

↓

Ganador.

↓

Empate.

Solo una podrá otorgar puntos.

---

# 8. Bonificaciones

Podrán existir reglas adicionales.

Ejemplos

Fecha doble.

Partido especial.

Multiplicador.

Comodín.

Racha.

Participación perfecta.

---

# 9. Calendario de Cierre

Cada Tipo define cuándo deja de aceptarse.

Ejemplos

Resultado.

↓

Comienza el partido.

---

Campeón.

↓

Comienza el torneo.

---

MVP.

↓

Finaliza la temporada regular.

---

# 10. Premios

La Competencia podrá definir múltiples premios.

Ejemplo

Primer puesto.

Segundo puesto.

Tercer puesto.

Ganador de la Fecha.

Ganador del Mes.

Ganador de la Primera Ronda.

Mayor cantidad de resultados exactos.

Mayor cantidad de participaciones.

Premio sorpresa.

---

# 11. Clasificaciones

La Competencia podrá generar distintos rankings.

General.

Mensual.

Semanal.

Fecha.

Grupo privado.

Empresa.

Histórico.

---

# 12. Desempates

Cada Competencia podrá definir su criterio.

Ejemplo

Más resultados exactos.

↓

Más ganadores acertados.

↓

Mayor cantidad de participaciones.

↓

Empate compartido.

---

# 13. Participación

Definir:

Participación libre.

Solo invitados.

Por código.

Por empresa.

Por grupo privado.

---

# 14. Grupos Privados

Una Competencia podrá permitir:

✓ Crear grupos.

✓ Unirse mediante código.

✓ Administrador del grupo.

✓ Ranking propio.

✓ Premios propios.

Todos utilizando exactamente los mismos partidos.

---

# 15. Visibilidad

Competencia pública.

Competencia privada.

Competencia oculta.

Competencia por invitación.

---

# 16. Notificaciones

Podrán configurarse:

Inicio de Fecha.

Cierre próximo.

Resultados oficiales.

Ranking actualizado.

Premios obtenidos.

Recordatorios.

---

# 17. Estadísticas

La Competencia podrá decidir mostrar:

Ranking.

Porcentaje de aciertos.

Cantidad de participantes.

Cantidad de resultados exactos.

Historial.

Rachas.

---

# 18. Patrocinadores

Cada Competencia podrá asociar:

Sponsor principal.

Sponsors secundarios.

Publicidad.

Premios patrocinados.

---

# 19. Personalización

Logo.

Colores.

Banner.

Nombre comercial.

Dominio.

Empresa organizadora.

---

# 20. Configuración Futura

Este modelo permitirá incorporar sin modificar la arquitectura:

- IA.
- Fantasy.
- Predicciones automáticas.
- Casas de apuestas.
- Cuotas.
- Tokens.
- Logros.
- Niveles.
- Misiones.
- Eventos especiales.

---

# 21. Filosofía

La Competencia no contiene lógica.

Contiene únicamente configuración.

Toda la inteligencia pertenece al Motor de Pronósticos y al Motor de Puntuación.

---

# 22. Decisión de Arquitectura

Una Competencia podrá transformarse completamente modificando únicamente su configuración.

No deberá requerirse desarrollar nuevas versiones del software para crear nuevas modalidades de juego.