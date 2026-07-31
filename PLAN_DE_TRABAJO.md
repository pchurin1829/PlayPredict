# PLAN DE TRABAJO

## Sprint 1 - ETAPA 0: Preparación (completado)
- [x] Crear Backend (ASP.NET Core Web API + Swagger + CORS + /api/health + /api/system/info)
- [x] Configurar Entity Framework Core para PostgreSQL (Npgsql) con DbContext inicial, sin entidades de negocio
- [x] Crear Frontend (React + Vite + TypeScript) consultando el backend real
- [x] Configurar PostgreSQL vía Docker Compose
- [x] Configurar Docker (Dockerfile.dev backend/frontend + docker-compose.yml)
- [x] Ejecutar proyecto vacío (entorno completo levanta y responde)
- [x] Crear repositorio Git (realizado en el ajuste posterior al Sprint 1)

## Sprint 2 - ETAPA 1: Motor del sistema (módulo base del fixture) (completado)
- [x] Entidades Competition, Edition, Round, Match + configuraciones EF Core (IEntityTypeConfiguration)
- [x] Migración inicial `InitialFixtureSchema` creada y aplicada a PostgreSQL
- [x] Endpoints REST de Competencias, Ediciones, Fechas y Partidos (CRUD + validaciones)
- [x] Endpoint de Resultado Oficial (`PUT /api/matches/{id}/result`) con punto de extensión para recálculo futuro
- [x] Seed de datos de demostración idempotente (Liga Profesional / Clausura 2026 / Fecha 1 / 3 partidos)
- [x] Panel administrativo básico en el frontend (listas y formularios para las 4 entidades + carga de resultado)
- [ ] Recálculo de Pronósticos y Rankings a partir del Resultado Oficial (queda para un sprint posterior)

## Sprint 3 - ETAPA 2: Usuarios (completado)
- [x] Entidades Company, Role, User, UserRole + configuraciones EF Core
- [x] Migración `AddUsersAndAuthentication` aplicada
- [x] Autenticación JWT (registro, login) y endpoints de perfil propio
- [x] Administración de usuarios (listar, activar/desactivar) restringida a rol ADMIN
- [x] Páginas de Registro, Login, Perfil y Administración de Usuarios en el frontend
- [x] Rutas protegidas (`RequireAuth`, `RequireAdmin`) y persistencia de sesión

Nota: este sprint se implementó en la sesión anterior (cortada por error 529) sin documentarse ni commitearse; se verificó y se dio por finalizado con aprobación explícita al inicio de esta sesión.

## Sprint 3.5 - Limpieza funcional pre-Pronósticos (completado)
- [x] Datos de demostración corregidos e idempotentes (Liga Profesional + Copa Libertadores, sin registros técnicos de prueba)
- [x] Textos visibles revisados; estados de Edición/Partido traducidos al castellano en la interfaz
- [x] Navegación Competencias → Ediciones → Fechas → Partidos revisada (ya cumplía)
- [x] Consistencia visual mínima revisada (ya cumplía, salvo estados)
- [x] Autenticación verificada de punta a punta (login, logout, persistencia, rutas protegidas, ADMIN/USER) sin modificar JWT ni roles

## Sprint 4 - ETAPA 3: Sistema de Pronósticos (infraestructura) (completado, sin commitear)
- [x] Entidad Prediction (MatchId, UserId, PredictedHomeScore, PredictedAwayScore, CreatedAtUtc, UpdatedAtUtc) + configuración EF Core (único por UserId+MatchId)
- [x] Migración `AddPredictions` creada y aplicada
- [x] Endpoints REST: GET partidos con pronóstico por Fecha, GET mis pronósticos, POST crear, PUT modificar
- [x] Reglas: un pronóstico por usuario y partido; solo editable en Programado/En juego/Suspendido; bloqueado en Finalizado y Cancelado; no se puede modificar el pronóstico de otro usuario
- [x] Pantalla "Pronósticos" en el frontend con navegación Competencia → Edición → Fecha → Partidos y carga partido por partido desde una única pantalla
- [ ] Cálculo de puntos, comparación con resultado oficial, rankings, posiciones y premios (explícitamente fuera de este sprint, queda para Sprint 5 en adelante)

## Sprint 5 - Motor de Puntuación Configurable Básico (completado, sin commitear)
- [x] Entidad EditionScoringConfiguration (1 a 1 con Edition, única por EditionId) + configuración EF Core
- [x] Entidad PredictionEvaluation (única por PredictionId, sin historial) + enum EvaluationType
- [x] Migración `AddScoringEngine` creada y aplicada
- [x] Servicio de evaluación (`PredictionEvaluationService`) separado del endpoint, responsabilidad única
- [x] `PUT /api/matches/{id}/result` dispara la evaluación automática de todos los pronósticos del partido, en la misma transacción implícita
- [x] Endpoints `GET/PUT /api/editions/{editionId}/scoring-configuration` exclusivos ADMIN, con validación de enteros ≥ 0
- [x] Configuración inicial automática (6/3/0) para Ediciones existentes (backfill del seed) y nuevas (al crearlas)
- [x] Reglas: marcador exacto > resultado correcto > incorrecto; recálculo al corregir un resultado oficial sin duplicar evaluaciones; sin evaluación para partidos sin resultado, Cancelados o Suspendidos
- [x] Pantalla "Configurar puntuación" en el panel administrativo (solo ADMIN)
- [x] Pantalla de Pronósticos del usuario muestra puntos/motivo cuando el partido está Finalizado, o "Sin pronóstico" si no pronosticó
- [ ] Rankings, posiciones, premios, bonificaciones, multiplicadores, historial de evaluaciones (explícitamente fuera de este sprint)

## Sprint 6 - Motor de Rankings (completado, sin commitear)
- [x] `RankingService`: Ranking General por Edición y Ranking por Fecha, calculados dinámicamente (sin tabla, sin migración)
- [x] Orden: puntos → exactos → correctos → incorrectos (asc) → apellido/nombre (solo desempate visual)
- [x] Posición compartida estilo ranking deportivo (1-2-2-4)
- [x] Solo participan usuarios con al menos un pronóstico evaluado
- [x] Endpoints `GET /api/rankings/editions/{editionId}` y `GET /api/rankings/rounds/{roundId}`, autenticados
- [x] Pantallas "Rankings" en el frontend: Competencia → Edición → Ranking General / Fechas → Ranking por Fecha
- [x] Datos de demostración idempotentes (4 usuarios, Fecha 1 de Clausura 2026 con nombres y resultados reales) — Ranking verificado exacto contra el esperado
- [x] Recálculo automático confirmado al agregar y al corregir un resultado oficial
- [ ] Ranking mensual, histórico, por empresa, por grupo privado, premios, bonificaciones (explícitamente fuera de este sprint)

## MVP
- [~] Competencias Oficiales — módulo base (Competencia/Edición/Fecha/Partido) funcionando; falta lo no incluido en el Sprint 2 (p. ej. estados avanzados de administración)
- [ ] Ligas Privadas
- [~] Partidos — alta, edición y Resultado Oficial funcionando vía panel administrativo
- [~] Pronósticos — infraestructura completa (carga, edición y cálculo automático de puntos) funcionando
- [~] Rankings — Ranking General y por Fecha funcionando; falta ranking histórico/mensual y por grupo privado
- [ ] Premios
- [~] Panel Administrador — fixture administrable (Competencias → Ediciones → Fechas → Partidos) + Usuarios + Configuración de puntuación por Edición; faltan las secciones de Resultados/Premios/Ligas del panel completo
- [x] Usuarios — Registro, Login, Perfil, administración básica (activar/desactivar) funcionando
