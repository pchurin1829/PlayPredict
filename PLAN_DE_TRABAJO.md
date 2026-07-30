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

## MVP
- [~] Competencias Oficiales — módulo base (Competencia/Edición/Fecha/Partido) funcionando; falta lo no incluido en el Sprint 2 (p. ej. estados avanzados de administración)
- [ ] Ligas Privadas
- [~] Partidos — alta, edición y Resultado Oficial funcionando vía panel administrativo
- [ ] Pronósticos
- [ ] Rankings
- [ ] Premios
- [~] Panel Administrador — fixture administrable (Competencias → Ediciones → Fechas → Partidos) + Usuarios; faltan las secciones de Resultados/Premios/Ligas del panel completo
- [x] Usuarios — Registro, Login, Perfil, administración básica (activar/desactivar) funcionando
