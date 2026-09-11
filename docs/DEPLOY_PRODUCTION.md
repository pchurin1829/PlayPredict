# PlayPredict — Despliegue en producción (P2.2)

Arquitectura: 3 contenedores (`proxy` Caddy con SPA estática + `/api`, `backend` Release,
`db` PostgreSQL 18 interno). Detalle de diseño: informe P2.2.

## 0. Requisitos previos (VPS)

- VPS con Docker + Compose plugin, UFW (`22,80,443`), SSH por keys, usuario no-root.
- DNS `A/AAAA` del dominio → IP del VPS (solo antes del go-live; el desarrollo
  de P2.2 usa `SITE_ADDRESS=:80` sin TLS).
- `.env.production` completo (ver `.env.production.example`). Nunca en Git.

## 1. Secrets

| Variable | Origen |
|---|---|
| `Jwt__Key` | `python -c "import secrets; print(secrets.token_hex(32))"` (≥32 bytes, obligatorio; sin ella Production aborta) |
| `POSTGRES_PASSWORD` | aleatorio largo |
| `Jwt__ExpiresMinutes` | 60 |

## 2. Primera instalación — CAMINO A (recomendado): SEED

```bash
git clone <repo> && cd PlayPredict
git checkout <tag-exacto>
cp .env.production.example .env.production  # completar secretos, NO commitear
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build db
docker compose -f docker-compose.prod.yml --env-file .env.production run --rm --no-deps backend \
  dotnet PlayPredict.Api.dll --seed-initial-v1
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build
curl -f http://localhost/api/health/ready
```

Valida conteos (24 migraciones, 1 comp, 2 ligas, 2 users, 30 teams, 1045 players,
60 matches, 0 predictions) y **rota credenciales demo** (§5) antes de exponer.

## 3. Primera instalación — CAMINO B (snapshot): DB0

DB0 (`docs/database/backups/DB0_PlayPredict_BaseInicial_v1_2026-09-01.sql`) está en
**23 migraciones**; el HEAD actual es **24 (`20260911145441_AddAccountSecurity`)**,
que se aplica sola al arrancar el backend. Procedimiento: `dropdb/createdb` sobre
base vacía → `psql -v ON_ERROR_STOP=1` → `up -d` (migra) → validar → rotar creds.
Detalle y conteos: `docs/database/backups/README.md`, `docs/BACKUP_RESTORE.md`.

## 4. Actualización (por TAG, nunca `latest`)

```bash
./scripts/backup.sh
git fetch --tags && git checkout <tag-nuevo>
docker compose -f docker-compose.prod.yml --env-file .env.production up -d --build
# el backend aplica migraciones pendientes con fail-fast antes de servir
curl -f https://<dominio>/api/health/ready
# smoke: Apéndice A
```

## 5. Credenciales go-live (obligatorio, sin passwords en docs)

1. Login ADMIN seed → `/admin/users` → resetear PLAYER demo (temporal + cambio forzoso).
2. `/profile` → cambiar password del ADMIN (mín. 10).
3. Completar el cambio forzoso del PLAYER.
4. Verificar: `admin`/`usuario` solos → 401; `Jwt__Key` es aleatoria externa;
   ningún default del compose en uso.

## 6. Rollback

- **A (solo código)**: `git checkout <tag-anterior>` + `up -d --build`.
- **B (migración aditiva/compatible)**: igual que A (columnas nuevas ignoradas).
- **C (destructiva/incompatible)**: `./scripts/restore.sh <dump> <uploads> --i-know-what-i-am-doing`
  (backup pre-deploy) + imagen anterior. Nunca `ef database update <anterior>` a ciegas.

## 7. Dominio / HTTPS / Firewall

DNS `A/AAAA` → VPS; Caddy (`SITE_ADDRESS=<dominio>`) obtiene/renueva Let's Encrypt
solo, redirige 80→443. UFW: 22/80/443; PG cerrado; SSH keys sin password.

## Apéndice A — Smoke go-live

HTTPS+redirect, `/api/health/ready`, login ADMIN/PLAYER, cambio password
(token viejo inválido), competencia/fixture, pronóstico+edición, resultado admin,
ranking, premio visible, upload imagen login, persistencia tras `restart backend`,
`admin`/`usuario` → 401.
