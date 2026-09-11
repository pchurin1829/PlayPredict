#!/usr/bin/env bash
# PlayPredict — backup productivo (P2.2).
# pg_dump -Fc de PostgreSQL + tar de uploads. No imprime passwords.
# Uso: ./scripts/backup.sh [destino]  (defecto: ./backups)
# Requiere: compose prod levantado, variables POSTGRES_* en entorno o .env.production
#   docker compose -f docker-compose.prod.yml --env-file .env.production exec ...
# El script no asume de dónde vienen: solo exige las variables.
set -euo pipefail

DEST="${1:-./backups}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-7}"
STAMP="$(date +%Y%m%d-%H%M)"
DB_CONTAINER="${DB_CONTAINER:-playpredict_prod_db}"
BACKEND_CONTAINER="${BACKEND_CONTAINER:-playpredict_prod_backend}"

: "${POSTGRES_DB:?POSTGRES_DB is required}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"

mkdir -p "$DEST"
DUMP="$DEST/playpredict_$STAMP.dump"
UPLOADS_TAR="$DEST/playpredict_uploads_$STAMP.tar.gz"

echo "[backup] pg_dump -> $DUMP"
docker exec "$DB_CONTAINER" pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc -f "/tmp/backup_$STAMP.dump"
docker cp "$DB_CONTAINER:/tmp/backup_$STAMP.dump" "$DUMP"
docker exec "$DB_CONTAINER" rm "/tmp/backup_$STAMP.dump"

echo "[backup] uploads -> $UPLOADS_TAR"
docker exec "$BACKEND_CONTAINER" tar -czf "/tmp/uploads_$STAMP.tar.gz" -C /data uploads
docker cp "$BACKEND_CONTAINER:/tmp/uploads_$STAMP.tar.gz" "$UPLOADS_TAR"
docker exec "$BACKEND_CONTAINER" rm "/tmp/uploads_$STAMP.tar.gz"

echo "[backup] verificación estructural (NO equivale a prueba de restore)"
docker run --rm -v "$PWD/$DEST:/b:ro" postgres:18-alpine \
  pg_restore --list "/b/$(basename "$DUMP")" > /dev/null

echo "[backup] retención: borra archivos de más de $RETENTION_DAYS días en $DEST"
find "$DEST" -maxdepth 1 -name 'playpredict_*.dump' -mtime +"$RETENTION_DAYS" -delete
find "$DEST" -maxdepth 1 -name 'playpredict_uploads_*.tar.gz' -mtime +"$RETENTION_DAYS" -delete

ls -la "$DUMP" "$UPLOADS_TAR"
echo "[backup] OK"
