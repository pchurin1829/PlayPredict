# SESSION

## Proyecto
PlayPredict

## Rama actual
main (sincronizada con origin/main)

## Último commit
274520b — docs: add session protocol and stage 1 summary

## Estado del entorno
- Git: rama `main`. Sprint 3 (Usuarios/Autenticación), Sprint 3.5 (limpieza funcional) y el fix de edición de Partidos Finalizados fueron aprobados explícitamente por el usuario y commiteados en un único commit. Detalle completo en PROJECT_STATUS.md.
- Docker (`docker compose ps`): los 3 servicios levantados y healthy tras `docker compose up -d --build`.
  - `playpredict_db` (PostgreSQL 18) — Up, healthy
  - `playpredict_backend` — Up, healthy
  - `playpredict_frontend` — Up
- URLs:
  - Frontend: http://localhost:5175
  - Backend Swagger: http://localhost:8006/swagger
  - Backend health: http://localhost:8006/api/health
- Usuario administrador de desarrollo: `admin@playpredict.local` / `admin123` (creado por el seed, solo en Development).

## Último trabajo completado
- **Sprint 3 — Usuarios y Autenticación** (encontrado ya implementado y funcionando al inicio de esta sesión, sin commitear ni documentar; se verificó, se dio por válido con aprobación explícita del usuario y se documenta ahora): entidades Company/Role/User/UserRole, JWT, endpoints de Auth/Users/AdminUsers, páginas de Login/Registro/Perfil/Administración de Usuarios, rutas protegidas.
- **Sprint 3.5 — Limpieza funcional pre-Pronósticos**: datos de demostración corregidos e idempotentes (Liga Profesional y Copa Libertadores), estados (Edición/Partido) traducidos al castellano en la interfaz, navegación y consistencia visual revisadas, autenticación verificada de punta a punta (login, logout, persistencia de sesión, rutas protegidas, acceso ADMIN, restricción USER). Sin nuevas funcionalidades, sin cambios al modelo de datos, sin nuevas migraciones.
- **Fix post-Sprint 3.5 — Edición de Partido rompía el Resultado Oficial**: detectado en la revisión visual del usuario. Editar un partido Finalizado (sin tocar el resultado) lo volvía a Programado y ocultaba el resultado en el listado (los goles seguían en la base, pero dejaban de mostrarse). Corregido en backend (`PUT /api/matches/{id}` nunca cambia el estado de un partido ya Finalizado) y en frontend (el formulario de Partido muestra "Finalizado" como estado no editable y no envía `status` en ese caso). Aprovechado para agregar navegación: botón "Volver a Partidos" en el formulario, y regreso automático a la lista con mensaje de confirmación tras guardar.

Detalle completo de ambos sprints en PROJECT_STATUS.md.

## Pendiente inmediato
- Decidir qué hacer con el cambio sin confirmar en `docs/Etapa_1_28-07-2026_PlayPredict.pdf` (no forma parte de este protocolo de sesión; sigue sin resolver de sesiones anteriores).
- No se ha iniciado Sprint 4 (Pronósticos).

## Próximo paso exacto
Iniciar Sprint 4 / ETAPA 3 de `PLAN_IMPLEMENTACION_MVP.md` (Pronósticos) — no iniciar sin aprobación explícita del usuario.

## Comandos para retomar
```bash
git status
git log -5 --oneline
docker compose ps
docker compose up -d        # si los servicios no están corriendo
docker compose logs -f backend
```
