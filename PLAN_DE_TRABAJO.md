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

## MVP
- [~] Competencias Oficiales — módulo base (Competencia/Edición/Fecha/Partido) funcionando; falta lo no incluido en el Sprint 2 (p. ej. estados avanzados de administración)
- [ ] Ligas Privadas
- [~] Partidos — alta, edición y Resultado Oficial funcionando vía panel administrativo
- [ ] Pronósticos
- [ ] Rankings
- [ ] Premios
- [~] Panel Administrador — fixture administrable (Competencias → Ediciones → Fechas → Partidos); faltan las secciones de Resultados/Premios/Usuarios/Ligas del panel completo
