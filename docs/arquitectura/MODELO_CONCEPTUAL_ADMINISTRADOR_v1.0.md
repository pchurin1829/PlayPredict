# MODELO_CONCEPTUAL_ADMINISTRADOR_v1.0

## Modelo Conceptual del Administrador

Versión: 1.0

---

# 1. Propósito

Este documento define el rol del Administrador dentro de PlayPredict.

No describe pantallas.

No describe permisos técnicos.

No describe casos de uso de implementación.

Describe el modelo conceptual del actor responsable de diseñar, publicar y operar una Experiencia Configurable.

---

# 2. Filosofía

El Administrador no administra tablas.

No administra registros.

No administra entidades aisladas.

Su función consiste en construir una Experiencia Deportiva completa que posteriormente será utilizada por miles de participantes.

Toda la interfaz administrativa deberá estar orientada a este objetivo.

---

# 3. Objetivos

El Administrador debe poder:

- crear experiencias;
- configurarlas;
- publicarlas;
- operarlas;
- analizarlas;
- evolucionarlas.

Todo ello sin requerir modificaciones de código.

---

# 4. El Administrador como Diseñador de Experiencias

Conceptualmente el Administrador actúa como un diseñador de productos.

No configura únicamente una Competencia.

Diseña una experiencia completa.

Esa experiencia define:

- identidad;
- reglas;
- participantes;
- competencias;
- premios;
- funcionamiento general.

---

# 5. Responsabilidades

Las responsabilidades del Administrador se agrupan en ocho grandes áreas.

## Diseño

Crear una nueva Experiencia.

Definir su propósito.

Asignar nombre.

Definir identidad.

---

## Configuración

Determinar cómo funcionará la experiencia.

Ejemplos:

- tipos de pronóstico;
- puntuación;
- rankings;
- premios;
- reglas generales.

---

## Organización

Crear Competencias.

Crear Ediciones.

Definir Fechas.

Administrar Partidos.

---

## Operación

Publicar la experiencia.

Abrir y cerrar Ediciones.

Registrar resultados.

Controlar incidencias.

---

## Seguimiento

Consultar estadísticas.

Consultar rankings.

Consultar participación.

Consultar actividad.

---

## Evolución

Modificar configuraciones.

Agregar nuevas competencias.

Incorporar nuevas campañas.

Actualizar branding.

---

## Comercial

Administrar sponsors.

Configurar premios.

Gestionar campañas promocionales.

---

## Auditoría

Consultar historial.

Revisar cambios.

Controlar publicaciones.

---

# 6. Componentes que administra

Conceptualmente el Administrador administra los siguientes componentes.

Experiencia

↓

Branding

↓

Sponsors

↓

Participantes

↓

Competencias

↓

Ediciones

↓

Fechas

↓

Partidos

↓

Configuración

↓

Premios

↓

Publicación

---

# 7. Flujo Conceptual

Toda Experiencia atraviesa el siguiente proceso.

Crear Experiencia

↓

Definir Branding

↓

Configurar reglas

↓

Crear Competencias

↓

Crear Ediciones

↓

Configurar Ediciones

↓

Cargar Fechas

↓

Cargar Partidos

↓

Publicar

↓

Operar

↓

Analizar

↓

Evolucionar

Este flujo representa el ciclo natural de administración.

---

# 8. Configuración de la Experiencia

El Administrador podrá definir configuraciones generales.

Entre ellas:

- identidad visual;
- sponsors;
- reglas por defecto;
- tipos de pronóstico;
- puntuación;
- ranking;
- premios;
- participantes.

Estas configuraciones constituyen el comportamiento base de la Experiencia.

---

# 9. Configuración de una Edición

Cada Edición podrá:

- heredar la configuración general;
- utilizarla sin cambios;
- sobrescribir únicamente los parámetros necesarios.

Este mecanismo evita duplicaciones y facilita la reutilización.

---

# 10. Publicación

Una Experiencia no se considera operativa hasta ser publicada.

La publicación representa el momento en que queda disponible para los participantes.

Una Experiencia puede encontrarse, conceptualmente, en alguno de estos estados:

- Diseño
- Configuración
- Publicada
- Operativa
- Finalizada
- Archivada

---

# 11. Principios

El Administrador nunca deberá configurar una misma información dos veces.

Siempre deberá privilegiarse:

- herencia;
- reutilización;
- configuración;
- simplicidad.

---

# 12. Independencia de los Motores

El Administrador configura motores.

No los ejecuta.

Cada motor mantiene su responsabilidad.

Motor de Pronósticos

↓

Motor de Puntuación

↓

Motor de Rankings

↓

Motor de Premios

El Administrador únicamente define cómo deben comportarse.

---

# 13. Dashboard Conceptual

El trabajo diario del Administrador gira alrededor de cinco grandes preguntas.

¿Qué experiencias existen?

¿Qué está publicado?

¿Qué está ocurriendo ahora?

¿Qué requiere atención?

¿Cómo está participando la comunidad?

Todo panel administrativo deberá responder estas preguntas antes que mostrar tablas o formularios.

---

# 14. Principios de Diseño

La experiencia administrativa deberá seguir los siguientes principios.

Pensar en procesos antes que en ABM.

Pensar en experiencias antes que en competencias.

Pensar en configuración antes que en programación.

Pensar en reutilización antes que en duplicación.

Pensar en evolución antes que en soluciones puntuales.

---

# 15. Definición Final

El Administrador representa el diseñador y operador de una Experiencia Configurable.

Su función no consiste en administrar datos aislados.

Su función consiste en construir, publicar y mantener productos digitales capaces de generar participación alrededor del deporte.

Toda futura funcionalidad administrativa deberá contribuir a simplificar esa tarea.