#!/usr/bin/env bash
# PlayPredict — restore productivo (P2.2). OPERACIÓN DESTRUCTIVA.
# Exige archivo explícito + flag --i-know-what-i-am-doing. Nunca corre en silencio.
# Uso: ./scripts/restore.sh <archivo.dump> <uploads.tar.gz> --i-know-what-i-am-doing
# Procedimiento: detiene backend -> recrea DB -> pg_restore -> uploads ->
# levanta backend (aplica migraciones) -> readiness -> smoke manual.
set -euo pipefail

DUMP="${1:-}"
UPLOADS_TAR="${2:-}"
CONFIRM="${3:-}"
COMPOSE="docker compose -f docker-compose.prod.yml"
DB_CONTAINER="${DB_CONTAINER:-playpredict_prod_db}"
BACKEND_CONTAINER="${BACKEND_CONTAINER:-playpredict_prod_backend}"

: "${POSTGRES_DB:?POSTGRES_DB is required}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"

if [[ ! -f "$DUMP" ]]; then echo "[restore] ERROR: dump no encontrado: $DUMP" >&2; exit 1; fi
if [[ ! -f "$UPLOADS_TAR" ]]; then echo "[restore] ERROR: uploads no encontrados: $UPLOADS_TAR" >&2; exit 1; fi
if [[ "$CONFIRM" != "--i-know-what-i-am-doing" ]]; then
  echo "[restore] ABORTADO: falta flag --i-know-what-i-am-doing" >&2
  echo "  Esto BORRA la base $POSTGRES_DB y los uploads actuales." >&2
  exit 2
fi

echo "[restore] 1/6 deteniendo backend (corta escrituras)"
$COMPOSE stop backend || true

echo "[restore] 2/6 recreando base $POSTGRES_DB"
docker exec "$DB_CONTAINER" dropdb --if-exists -U "$POSTGRES_USER" "$POSTGRES_DB"
docker exec "$DB_CONTAINER" createdb -U "$POSTGRES_USER" -T template0 "$POSTGRES_DB"

echo "[restore] 3/6 pg_restore $DUMP"
docker cp "$DUMP" "$DB_CONTAINER:/tmp/restore.dump"
docker exec "$DB_CONTAINER" pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --no-owner --no-privileges /tmp/restore.dump
docker exec "$DB_CONTAINER" rm /tmp/restore.dump

echo "[restore] 4/6 uploads $UPLOADS_TAR"
docker cp "$UPLOADS_TAR" "$BACKEND_CONTAINER:/tmp/uploads_restore.tar.gz"
docker exec "$BACKEND_CONTAINER" tar -xzf /tmp/uploads_restore.tar.gz -C /data
docker exec "$BACKEND_CONTAINER" rm /tmp/uploads_restore.tar.gz

echo "[restore] 5/6 levantando backend (aplica migraciones pendientes)"
$COMPOSE up -d backend
echo "[restore] esperando readiness..."
for i in $(seq 1 30); do
  if docker exec "$BACKEND_CONTAINER" curl -fs http://localhost:8080/api/health/ready > /dev/null 2>&1; then
    echo "[restore] backend ready"
    break
  fi
  if [[ "$i" == "30" ]]; then echo "[restore] ERROR: backend no quedó ready" >&2; exit 1; fi
  sleep 5
done

echo "[restore] 6/6 verificar: conteos + logins + smoke (ver docs/BACKUP_RESTORE.md)"
echo "[restore] OK parcial: completar smoke manual antes de exponer tráfico"
