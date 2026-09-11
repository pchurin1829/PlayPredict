# PlayPredict — Backups y restore (P2.2)

Objetivos (no garantías): **RPO 24 h, RTO 1–2 h**, sin alta disponibilidad.

## Backup

```bash
./scripts/backup.sh [destino]   # defecto ./backups (gitignored)
```

- `pg_dump -Fc` con timestamp `playpredict_YYYYMMDD-HHMM.dump` + `tar.gz` de uploads.
- No imprime passwords (usa variables de entorno, nunca `-W` interactivo en logs).
- Retención configurable: `BACKUP_RETENTION_DAYS` (defecto 7).
- Verificación estructural `pg_restore --list` incluida. **Esto NO equivale a
  prueba de restore.**
- Programación sugerida (cron host, VPS): `0 3 * * * /srv/PlayPredict/scripts/backup.sh`
  + copia externa semanal (rsync/rclone). Ajustar rutas al VPS real.

## Restore

```bash
./scripts/restore.sh <dump> <uploads.tar.gz> --i-know-what-i-am-doing
```

Sin el flag destructivo aborta. Flujo: stop backend → recrea DB → `pg_restore`
(`--no-owner --no-privileges`) → uploads → `up backend` (migraciones auto) →
readiness → smoke manual (`docs/DEPLOY_PRODUCTION.md` Apéndice A).

## Prueba de restore (obligatoria pre-go-live y trimestral)

Restaurar un backup real en **DB temporal** (nunca sobre producción ni dev),
validar conteos (24 migraciones, 60 matches, 1045 players, 0 predictions),
logins canónicos y eliminar solo la temporal. Ensayo local §21 del plan P2.2
como referencia técnica; repetir en VPS.

## Qué se respalda

- PostgreSQL completo (esquema + `__EFMigrationsHistory` + datos).
- Volumen uploads (`/data/uploads`: appearance + welcome campaigns).
- Fuera de backup (reproducible desde Git): código, imágenes Docker, `.env.production`
  (este último por canal aparte cifrado, documentado fuera del repo).
