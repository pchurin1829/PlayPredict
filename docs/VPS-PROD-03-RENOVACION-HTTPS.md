# VPS-PROD-03B — renovación HTTPS exclusiva de PlayPredict

Estado: **HTTPS publicado y validado externamente; renovación automática
instalada, habilitada y activa**. Fecha del runbook: 2026-11-11.
Verificación remota de solo lectura: 2026-11-12.
BAMIRA conserva su Nginx y su `certbot.timer`; no se modifican.

## Estado verificado

- `https://prode.odusport.com` responde `200` en `/api/health` y `/api/health/ready`.
- HTTP redirige con `308` a HTTPS.
- Certificado servido: `CN=prode.odusport.com`, emisor `Let's Encrypt YE2`,
  válido `2026-11-11 23:28:35Z` → `2027-02-09 23:28:34Z`.
- Tienda dedicada: `/etc/letsencrypt-playpredict`.
- `playpredict-cert-renew.timer`: `enabled` y `active`; `playpredict-cert-renew.service`
  se ejecutó con `Result=success` y `ExecMainStatus=0`.

## Verificación remota 2026-11-12

Lectura con `playpredict-deploy` (sin root) sobre el VPS de producción:

| Archivo | SHA-256 remoto | Coincidencia local |
|---|---|---|
| `/etc/nginx/sites-available/prode.odusport.com` | `7c0a2f7207c68c0dd4a8a622f72dd74fc929d11a47abb067e0bdb1b84437a75c` | `docs/nginx/prode.odusport.com.https.conf` |
| `/etc/systemd/system/playpredict-cert-renew.service` | `623a3e7f7cbcacd639b1be35546e026fd697ba86a4de8f0d1cc4e8fa791db189` | `scripts/https-prod/playpredict-cert-renew.service` |
| `/etc/systemd/system/playpredict-cert-renew.timer` | `73317428dff9c662793a7236b68e89035a95b7d5fc3a4e0d2cd1dbb6c0d4c6ff` | `scripts/https-prod/playpredict-cert-renew.timer` |

`playpredict-tls-refresh` está instalado `root:root 0700`, 2150 bytes, y la
ejecución del servicio terminó en 0; su hash y el de `certbot.ini` no son legibles
para la cuenta de lectura. Los hashes locales están en `scripts/https-prod/SHA256SUMS`
y la confirmación root de esos dos archivos queda pendiente. El timer está activo y
`systemctl list-timers` muestra la próxima ejecución.


## Resultado de la revisión de los tres archivos

La copia local de `playpredict-cert-renew.service` **ya contiene** `[Service]`
(línea 7) y `Type=oneshot` (línea 8). No tiene directivas de servicio dentro de
`[Unit]`. El síntoma descrito solo podría explicarse por una copia distinta en el
VPS (por ejemplo, CRLF o una versión previa). La verificación remota del 2026-11-12
confirmó que el hash instalado coincide con la copia local y que el servicio
ejecuta correctamente.

Corrección local realizada: se reemplazó el comentario engañoso que afirmaba que
el refresh siempre se ejecuta y reintenta recargas fallidas. Con dos `ExecStart`
en `Type=oneshot`, systemd ejecuta el segundo **solo si el primero termina en 0**.
Por lo tanto, un fallo de `certbot renew` detiene la unidad y no ejecuta el
refresh; eso es intencional para no ocultar fallos de renovación.

### Estructura esperada del unit

```ini
[Unit]
Description=Renew only prode.odusport.com in its dedicated Certbot store
Wants=network-online.target
After=network-online.target nginx.service
ConditionPathExists=/etc/letsencrypt-playpredict/renewal/prode.odusport.com.conf

[Service]
Type=oneshot
User=root
Group=root
UMask=0077
Environment=PATH=/usr/sbin:/usr/bin:/sbin:/bin
ExecStart=/usr/bin/certbot renew --config /etc/playpredict-https/certbot.ini --config-dir /etc/letsencrypt-playpredict --work-dir /var/lib/letsencrypt-playpredict --logs-dir /var/log/letsencrypt-playpredict --cert-name prode.odusport.com --no-directory-hooks --quiet
# Runs only when certbot exits 0 (no renewal due or renewal succeeded).
# Compares served vs on-disk public certificate and reloads Nginx only if needed.
# A certbot failure stops the unit here, so renewal errors are not masked.
ExecStart=/usr/local/sbin/playpredict-tls-refresh
TimeoutStartSec=15min
PrivateTmp=true
NoNewPrivileges=true
```

`Type=oneshot` admite múltiples `ExecStart`; se ejecutan en orden y se detienen
en el primer fallo. No cambiar a `Type=simple` ni agregar `RemainAfterExit`.

## Auditoría funcional y de seguridad

- **Directorios exclusivos:** `--config-dir`, `--work-dir` y `--logs-dir` apuntan
  solo a PlayPredict. Los locks de Certbot quedan separados de BAMIRA.
- **Certificado exclusivo:** `--cert-name prode.odusport.com` limita la acción a
  esa lineage; no se recorre la tienda global `/etc/letsencrypt`.
- **Hooks:** `--no-directory-hooks` evita hooks de directorio. La unidad no define
  hooks propios.
- **Recarga de Nginx:** el refresh compara el certificado público servido en
  `127.0.0.1:443` con `cert.pem` de la tienda dedicada. Solo si difieren ejecuta
  `nginx -t` y luego `systemctl reload nginx`, y confirma el certificado servido.
  No recarga si coinciden.
- **Fallo de Nginx:** si `nginx -t` falla, no recarga; la unidad falla y alerta. Si
  la recarga falla o el certificado no cambia, también falla.
- **Certificado vencido:** `ssl.create_default_context()` valida cadena, nombre y
  vigencia. Un certificado vencido se trata como no coincidente; se intenta una
  recarga validada y, si no se corrige, la unidad falla. Certbot renueva antes del
  vencimiento (dos chequeos diarios).
- **Timer:** `Unit=playpredict-cert-renew.service` coincide exactamente con el
  servicio. `Persistent=true` recupera una ejecución perdida al arrancar.
- **Permisos:** el refresh es root-only; instalar `root:root 0700`. Units `0644`.
  `certbot.ini` `0600`. Webroot `/var/www/letsencrypt-playpredict` `root:root 0755`.
- **Webroot y UMask:** Certbot crea los archivos de challenge con umask temporal
  `0022` (código de su plugin webroot), por lo que `UMask=0077` de la unidad no
  impide que Nginx los lea.
- **BAMIRA:** no se toca su tienda, sitios, certificados, timer ni configuración.
  La recarga de Nginx es compartida pero solo ocurre tras `nginx -t` y un cambio
  real de certificado.

### Riesgo residual: `cli.ini` global de Certbot

Certbot sigue leyendo `/etc/letsencrypt/cli.ini` y
`~/.config/letsencrypt/cli.ini` aunque se pase `--config` (comportamiento
documentado y bug abierto de Certbot). Si BAMIRA tiene un `cli.ini` global con
`authenticator`, `webroot-path`, `server`, `pre-hook`, `post-hook` o
`deploy-hook`, esos valores podrían influir en esta renovación.

Antes de habilitar, revisar sin imprimir secretos:

```sh
for f in /etc/letsencrypt/cli.ini /root/.config/letsencrypt/cli.ini; do
  if [ -f "$f" ]; then
    echo "REVISAR: $f"
    grep -E '^[[:space:]]*(authenticator|webroot-path|preferred-challenges|key-type|elliptic-curve|server|pre-hook|post-hook|deploy-hook|config-dir|work-dir|logs-dir)[[:space:]]*=' "$f" || true
  fi
done
```

Si aparece cualquiera de esas directivas, no habilitar el timer hasta neutralizar
el conflicto. Opciones: quitar la directiva del `cli.ini` (decisión del
administrador de BAMIRA) o pasarla explícitamente en la línea de `ExecStart` para
que la línea de comandos prevalezca. No editar el `cli.ini` global desde este
procedimiento.

## Archivos a transferir

Desde el repositorio local:

- `scripts/https-prod/playpredict-cert-renew.service`
- `scripts/https-prod/playpredict-cert-renew.timer`
- `scripts/https-prod/playpredict-tls-refresh`
- `scripts/https-prod/certbot.ini`
- `scripts/https-prod/SHA256SUMS`

Hashes actuales:

```text
623a3e7f7cbcacd639b1be35546e026fd697ba86a4de8f0d1cc4e8fa791db189  playpredict-cert-renew.service
73317428dff9c662793a7236b68e89035a95b7d5fc3a4e0d2cd1dbb6c0d4c6ff  playpredict-cert-renew.timer
276103c797849c776d1d049a55d60941d09d2acb4ca9b9c234fe9678c12d48e7  playpredict-tls-refresh
70c82da64a6392f7d3772866802276f74d87402daa6597e8e7e74ca671cf03e0  certbot.ini
```

## Preflight en el VPS (solo lectura)

```sh
set -eu
echo '--- estado actual ---'
systemctl status playpredict-cert-renew.timer --no-pager || true
systemctl cat playpredict-cert-renew.service 2>/dev/null || true
echo '--- certstore dedicado ---'
test -f /etc/letsencrypt-playpredict/renewal/prode.odusport.com.conf
test -f /etc/letsencrypt-playpredict/live/prode.odusport.com/fullchain.pem
test -f /etc/playpredict-https/certbot.ini
echo '--- binarios ---'
command -v certbot; certbot --version
test -x /usr/bin/certbot
test -x /usr/bin/python3
test -x /usr/sbin/nginx
test -x /usr/bin/systemctl
echo '--- cert servido ---'
openssl x509 -in /etc/letsencrypt-playpredict/live/prode.odusport.com/fullchain.pem \
  -noout -subject -issuer -dates
echo '--- nginx ---'
nginx -t
echo '--- CLI.INI GLOBAL (ver riesgo residual) ---'
for f in /etc/letsencrypt/cli.ini /root/.config/letsencrypt/cli.ini; do
  if [ -f "$f" ]; then
    echo "REVISAR: $f"
    grep -E '^[[:space:]]*(authenticator|webroot-path|preferred-challenges|key-type|elliptic-curve|server|pre-hook|post-hook|deploy-hook|config-dir|work-dir|logs-dir)[[:space:]]*=' "$f" || true
  fi
done
```

No continuar si `nginx -t` falla, si falta la tienda dedicada o si el `cli.ini`
global contiene directivas en conflicto. Si `command -v certbot` no es
`/usr/bin/certbot`, ajustar la ruta del `ExecStart` en la unidad antes de instalar
(no ampliar permisos ni crear wrappers).

## Transferencia

> Los comandos de estación de trabajo están en sintaxis POSIX. En PowerShell 5.1
> usar backtick para continuar líneas, o ejecutarlos en Git Bash/WSL. El `scp` y
> `ssh` de Windows OpenSSH aceptan las mismas opciones.

### Opción A — sesión root

```sh
# Ejecutar desde la estación de trabajo.
ssh -p 5978 ADMIN_USER@201.32.129.24 \
  'install -d -o root -g root -m 0700 /root/playpredict-https-review'
scp -P 5978 \
  scripts/https-prod/playpredict-cert-renew.service \
  scripts/https-prod/playpredict-cert-renew.timer \
  scripts/https-prod/playpredict-tls-refresh \
  scripts/https-prod/certbot.ini \
  scripts/https-prod/SHA256SUMS \
  ADMIN_USER@201.32.129.24:/root/playpredict-https-review/
```

### Opción B — usuario deploy a /tmp, luego root

```sh
scp -P 5978 \
  scripts/https-prod/playpredict-cert-renew.service \
  scripts/https-prod/playpredict-cert-renew.timer \
  scripts/https-prod/playpredict-tls-refresh \
  scripts/https-prod/certbot.ini \
  scripts/https-prod/SHA256SUMS \
  playpredict-deploy@201.32.129.24:/tmp/playpredict-https-renew/
# En sesión root:
install -d -o root -g root -m 0700 /root/playpredict-https-review
install -o root -g root -m 0600 /tmp/playpredict-https-renew/* /root/playpredict-https-review/
rm -f /tmp/playpredict-https-renew/*
```

Verificar hashes y formato en el VPS antes de instalar:

```sh
cd /root/playpredict-https-review
sha256sum -c SHA256SUMS
for f in playpredict-cert-renew.service playpredict-cert-renew.timer playpredict-tls-refresh certbot.ini; do
  printf '%s ' "$f"
  if grep -q $'\r' "$f"; then echo 'CRLF'; else echo 'LF'; fi
  head -c 3 "$f" | od -An -t x1
done
test "$(head -c 2 playpredict-tls-refresh)" = '#!'
```

Los cuatro archivos de código deben dar LF y no empezar con BOM (`ef bb bf`);
`SHA256SUMS` también debe ser LF. Si el `.service` del VPS actual no coincide con
el hash, se reemplaza; no se edita a mano.

## Instalación / actualización

```sh
set -euo pipefail
umask 077
REVIEW=/root/playpredict-https-review
test "$(id -u)" = 0

# Configuración de Certbot (sin secretos).
install -d -o root -g root -m 0700 /etc/playpredict-https
install -o root -g root -m 0600 "$REVIEW/certbot.ini" /etc/playpredict-https/certbot.ini

# Script root-only y unidades.
install -o root -g root -m 0700 "$REVIEW/playpredict-tls-refresh" /usr/local/sbin/playpredict-tls-refresh
install -o root -g root -m 0644 "$REVIEW/playpredict-cert-renew.service" /etc/systemd/system/playpredict-cert-renew.service
install -o root -g root -m 0644 "$REVIEW/playpredict-cert-renew.timer" /etc/systemd/system/playpredict-cert-renew.timer

systemd-analyze verify \
  /etc/systemd/system/playpredict-cert-renew.service \
  /etc/systemd/system/playpredict-cert-renew.timer
systemctl daemon-reload
```

`systemd-analyze verify` no debe reportar errores de sección, directiva o
ejecutable. Un warning de orden sin `Wants` no es bloqueante; investigar cualquier
otro.

## Prueba controlada (sin forzar renovación)

```sh
systemctl start playpredict-cert-renew.service
systemctl status playpredict-cert-renew.service --no-pager
journalctl -u playpredict-cert-renew.service -n 100 --no-pager
```

Resultado esperado:

- `certbot renew` termina en 0 (normalmente “no renewals were attempted”).
- El refresh imprime `PASS: certificate already served; no reload`.
- No se recarga Nginx ni se modifica el certificado.
- La unidad queda `inactive (dead)` con `Result=success`.

No usar `--force-renewal`. La prueba controlada no solicita un certificado nuevo.

### Validación opcional del desafío HTTP-01 (staging)

Solo con autorización explícita. `--dry-run` usa el entorno de staging, no guarda
certificado de producción y no modifica el certificado servido. Sirve para validar
webroot, DNS y la ruta de renovación completa:

```sh
certbot renew --config /etc/playpredict-https/certbot.ini \
  --config-dir /etc/letsencrypt-playpredict \
  --work-dir /var/lib/letsencrypt-playpredict \
  --logs-dir /var/log/letsencrypt-playpredict \
  --cert-name prode.odusport.com \
  --no-directory-hooks --dry-run
```

Debe terminar con “The dry run was successful”. Si falla el desafío, revisar
webroot y permisos antes de habilitar el timer. No ejecutar `--force-renewal`.

## Habilitar y verificar el timer

```sh
systemctl enable --now playpredict-cert-renew.timer
systemctl list-timers --all 'playpredict-cert-renew*'
systemctl status playpredict-cert-renew.timer --no-pager
```

La próxima ejecución debe ser a las `00:20` o `12:20` UTC, más hasta 30 minutos de
jitter. Confirmar con `systemctl list-timers`. No cambiar la hora para “probar”:
la prueba controlada ya ejecutó el servicio una vez.

## Rollback

Solo revierte la renovación; no toca HTTPS publicado, certificados ni Nginx.
Si `/usr/local/sbin/playpredict-tls-refresh` ya existía antes de este
procedimiento, no borrarlo: restaurar la copia previa. En esta instalación no
existía.

```sh
set -euo pipefail
systemctl disable --now playpredict-cert-renew.timer 2>/dev/null || true
rm -f /etc/systemd/system/playpredict-cert-renew.timer
rm -f /etc/systemd/system/playpredict-cert-renew.service
rm -f /usr/local/sbin/playpredict-tls-refresh
systemctl daemon-reload
```

Conservar `/etc/letsencrypt-playpredict`, `/etc/playpredict-https` y los logs.
No borrar certificados ni la tienda ACME. No modificar Nginx ni BAMIRA. Verificar
que el sitio sigue respondiendo:

```sh
curl -fsS https://prode.odusport.com/api/health
curl -fsS https://prode.odusport.com/api/health/ready
```

## Riesgos pendientes

- Contenido real de `/etc/letsencrypt/cli.ini` (BAMIRA): no verificado sin root.
- Hash root de `playpredict-tls-refresh` y `certbot.ini`: no legibles con la cuenta
  de lectura; confirmar en una sesión root si se requiere evidencia criptográfica.
- `systemd-analyze verify` real: pendiente; el servicio y el timer ya se ejecutaron
  con éxito en producción.
- Que no exista otra automatización ACME para este dominio: verificar cron/timers.
- La recarga de Nginx es compartida; un error global de configuración de BAMIRA
  puede impedir la recarga. El `nginx -t` previo evita recargar una configuración
  inválida.
- Si Certbot cambia de versión y altera la ruta de binarios o el formato de
  `cli.ini`, revisar la unidad antes de actualizar.
