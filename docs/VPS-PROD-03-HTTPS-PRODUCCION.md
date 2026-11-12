# VPS-PROD-03 — publicación HTTPS de prode.odusport.com

> Actualización 2026-11-11: HTTPS ya está publicado y validado. Este documento
> conserva el procedimiento inicial de publicación. La instalación y verificación
> de la renovación automática está en `docs/VPS-PROD-03-RENOVACION-HTTPS.md`.

Estado histórico de esta guía: **preparación local, sin instalación en producción
al momento de redactarla.**
Fecha: 2026-11-11. Rama `prueba-glm-ui`, HEAD `8012752`.
Dominio objetivo: `https://prode.odusport.com` (solo IPv4, sin AAAA).
Proxy interno: `127.0.0.1:8089`. Nginx público: compartido con BAMIRA.

Este documento es el runbook de instalación. No declara HTTPS funcionando: al
cierre de esta revisión el dominio no está publicado (HTTP responde desde otro
sitio y el handshake TLS no valida para ese nombre).

## Artefactos revisados

| Archivo | Función |
|---|---|
| `docs/nginx/prode.odusport.com.bootstrap.conf` | Sitio temporal: solo ACME HTTP-01, resto 503 |
| `docs/nginx/prode.odusport.com.https.conf` | Sitio final: redirección 308 + proxy TLS a 8089 |
| `scripts/https-prod/certbot.ini` | Certbot dedicado (webroot, ECDSA P-256, sin hooks) |
| `scripts/https-prod/playpredict-cert-renew.service` | Renovación + refresh TLS |
| `scripts/https-prod/playpredict-cert-renew.timer` | Dos chequeos diarios |
| `scripts/https-prod/playpredict-tls-refresh` | Compara certificado servido vs. disco y recarga Nginx solo si hace falta |

Decisiones de diseño que deben conservarse:

- Tienda ACME dedicada `/etc/letsencrypt-playpredict`; no se toca la tienda global
  ni los certificados de BAMIRA.
- Challenge HTTP-01 por webroot; nunca `certbot --nginx` ni `standalone`.
- Sin HSTS, sin `includeSubDomains`, sin `preload` en esta primera etapa.
- Sin IPv6: no hay AAAA y forzar `listen [::]` puede fallar si el kernel no tiene IPv6.
- Nginx recibe el certificado; Caddy interno sigue en HTTP `:80`, `auto_https off`.

## Arquitectura de confianza

```
Internet :443 -> Nginx host (TLS) -> 127.0.0.1:8089 -> Caddy :80
             -> backend:8080 -> PostgreSQL:5432
```

- Nginx sobrescribe `X-Forwarded-For` con `$remote_addr`, `X-Forwarded-Proto` con
  `$scheme`, borra `Forwarded`, `Upgrade`, `Connection` y `Cookie`.
- Caddy confía en `PROD_GATEWAY`; reenvía una única IP de cliente al backend.
- ASP.NET confía en `PROD_PROXY_IP` con `ForwardLimit=1` antes del rate limiting.
- Autenticación por JWT Bearer (`Authorization`), sin cookies de sesión.
- CORS productivo vacío: mismo origen, sin orígenes cruzados.
- Rutas relativas `/api/...`: no hay URL absoluta ni puerto interno en el frontend.

## Riesgos identificados

1. **Nginx compartido (BAMIRA).** Si ningún bloque existente declara
   `default_server` para 80/443, el primer `server` cargado pasa a ser el default.
   Agregar un sitio nuevo puede cambiar qué responde ante Host/SNI desconocidos o
   IP directa. Preflight obligatorio: `nginx -T` y verificación explícita de
   `default_server` antes y después. Si BAMIRA no tiene default explícito, no
   continuar sin decisión del administrador.
2. **Recarga de Nginx.** `systemctl reload nginx` afecta a BAMIRA. Siempre ejecutar
   `nginx -t` antes; si falla, no recargar y revertir.
3. **Nombre de sitio duplicado.** El bootstrap y el HTTPS declaran el mismo
   `server_name`. Nunca habilitar ambos a la vez: se reemplaza el archivo, no se
   agrega un segundo sitio.
4. **Certbot global.** Usar solo `--config-dir/--work-dir/--logs-dir` dedicados,
   `--cert-name` y `--no-directory-hooks`. No ejecutar `certbot -d` sin esas rutas.
5. **Webroot.** `/var/www/letsencrypt-playpredict` debe ser `root:root 0755` y los
   archivos de challenge `0644` para que el worker de Nginx los lea. Un webroot
   `0700` rompe HTTP-01.
6. **Permisos del refresh.** `playpredict-tls-refresh` se instala `root:root 0700`
   (script root-only). El checkout de Windows no conserva el bit ejecutable; usar
   `install -m 0700`.
7. **Logs.** Certbot escribe en `/var/log/letsencrypt-playpredict` (rotación propia
   de Certbot). Nginx escribe `playpredict-access/error.log` (cubiertos por el
   logrotate estándar de `/var/log/nginx/*.log`). No crear logrotate que toque
   archivos de BAMIRA.
8. **Renovación no probada localmente.** `systemd-analyze verify`, `certbot renew
   --dry-run` y el kernel Linux no se ejecutaron en esta revisión.

## Preflight (solo lectura, sesión root)

Ejecutar y archivar la salida en `/root/playpredict-https-review/` (0700). No
imprimir secretos.

```sh
install -d -o root -g root -m 0700 /root/playpredict-https-review
umask 077
{
  date -u
  hostname
  nginx -v
  nginx -t
  nginx -T > /root/playpredict-https-review/nginx-before.txt 2>&1
  echo '--- default_server ---'
  nginx -T 2>/dev/null | grep -n 'default_server' || true
  echo '--- server_name ---'
  nginx -T 2>/dev/null | grep -n 'server_name' || true
  echo '--- include sites-enabled ---'
  grep -n 'sites-enabled' /etc/nginx/nginx.conf || true
  echo '--- puertos ---'
  ss -lntp '( sport = :80 or sport = :443 or sport = :8089 or sport = :8088 )'
  echo '--- certbot ---'
  command -v certbot || true
  certbot --version || true
  ls -l /usr/bin/certbot /snap/bin/certbot 2>/dev/null || true
  echo '--- restos del dominio ---'
  grep -RIn 'prode\.odusport\.com' /etc/nginx/sites-available /etc/nginx/sites-enabled /etc/nginx/conf.d 2>/dev/null || true
  echo '--- recursos existentes ---'
  ls -ld /etc/letsencrypt-playpredict /etc/playpredict-https /var/www/letsencrypt-playpredict /usr/local/sbin/playpredict-tls-refresh 2>/dev/null || true
} > /root/playpredict-https-review/baseline.txt
chmod 600 /root/playpredict-https-review/baseline.txt
```

Comprobar manualmente:

- `certbot` real en `/usr/bin/certbot` (o el que devuelva `command -v`). Si es snap,
  confirmar que `--config-dir/--work-dir/--logs-dir` siguen permitidos y ajustar
  la unidad; no ampliar permisos.
- Rutas fijas del service/refresh ejecutables: `/usr/bin/python3`, `/usr/sbin/nginx`
  y `/usr/bin/systemctl`. Si alguna difiere, corregir la unidad antes de instalar.
- `nginx -t` actual en 0. Si ya falla, no continuar.
- BAMIRA responde antes del cambio:

```sh
curl -sS -o /dev/null -w 'bamira_local=%{http_code}\n' http://127.0.0.1:8088/ || true
# Repetir con el dominio público administrado de BAMIRA y guardar el resultado.
```

- No existe otro cliente ACME/cron gestionando `prode.odusport.com`.
- `PROXY_BIND_IP=127.0.0.1:8089` responde (`/api/health/ready`).
- DNS: `prode.odusport.com A 201.32.129.24` (ya confirmado 2026-11-11).

Transferir a `/root/playpredict-https-review/` los seis artefactos revisados con
propiedad `root:root`, sin escritura de grupo/otros, y verificar hashes por un
canal independiente. No ejecutar el checked-out como root.

## Variables del procedimiento

- `REVIEW=/root/playpredict-https-review`
- `EMAIL_ACME=...` (el administrador lo provee; no se registra en el informe)
- `SITE=/etc/nginx/sites-available/prode.odusport.com`
- `LINK=/etc/nginx/sites-enabled/prode.odusport.com`

## Fase A — directorios y configuración dedicada de Certbot

```sh
set -euo pipefail
umask 077
test "$(id -u)" = 0
REVIEW=/root/playpredict-https-review

install -d -o root -g root -m 0755 /var/www/letsencrypt-playpredict
install -d -o root -g root -m 0700 /etc/playpredict-https
install -d -o root -g root -m 0700 /var/lib/letsencrypt-playpredict
install -d -o root -g root -m 0700 /var/log/letsencrypt-playpredict
install -o root -g root -m 0600 "$REVIEW/certbot.ini" /etc/playpredict-https/certbot.ini

test ! -e /etc/letsencrypt-playpredict
test ! -e "$SITE"
test ! -e "$LINK"
```

## Fase B — bootstrap HTTP-01 y emisión inicial

```sh
set -euo pipefail
umask 022
REVIEW=/root/playpredict-https-review
SITE=/etc/nginx/sites-available/prode.odusport.com
LINK=/etc/nginx/sites-enabled/prode.odusport.com

install -o root -g root -m 0644 "$REVIEW/prode.odusport.com.bootstrap.conf" "$SITE"
ln -s "$SITE" "$LINK"
nginx -t
systemctl reload nginx
```

Verificar que el bootstrap solo responde al desafío y 503 al resto:

```sh
curl -sS -o /dev/null -w 'bootstrap_root=%{http_code}\n' \
  --resolve prode.odusport.com:80:127.0.0.1 http://prode.odusport.com/
curl -sS -o /dev/null -w 'bootstrap_acme_missing=%{http_code}\n' \
  --resolve prode.odusport.com:80:127.0.0.1 \
  http://prode.odusport.com/.well-known/acme-challenge/no-existe
```

Esperado: `503` y `404`. Repetir la comprobación de BAMIRA. Si algo falla:
revertir Fase B (ver Rollback).

Emitir el certificado con la tienda dedicada. No usar `--nginx` ni `--standalone`.
No imprimir claves.

```sh
set -euo pipefail
umask 022
REVIEW=/root/playpredict-https-review

certbot certonly \
  --config /etc/playpredict-https/certbot.ini \
  --config-dir /etc/letsencrypt-playpredict \
  --work-dir /var/lib/letsencrypt-playpredict \
  --logs-dir /var/log/letsencrypt-playpredict \
  --cert-name prode.odusport.com \
  -d prode.odusport.com \
  --email "$EMAIL_ACME" \
  --non-interactive --agree-tos \
  --no-directory-hooks
```

Verificar sin revelar la clave:

```sh
test -f /etc/letsencrypt-playpredict/live/prode.odusport.com/fullchain.pem
test -f /etc/letsencrypt-playpredict/live/prode.odusport.com/privkey.pem
test -f /etc/letsencrypt-playpredict/renewal/prode.odusport.com.conf
openssl x509 -in /etc/letsencrypt-playpredict/live/prode.odusport.com/fullchain.pem \
  -noout -subject -issuer -dates -ext subjectAltName
ls -l /etc/letsencrypt-playpredict/live/prode.odusport.com/
```

## Fase C — reemplazo por el sitio HTTPS

Se reemplaza el archivo; no se agrega un segundo sitio.

```sh
set -euo pipefail
umask 022
REVIEW=/root/playpredict-https-review
SITE=/etc/nginx/sites-available/prode.odusport.com

cp -a "$SITE" "$REVIEW/prode.bootstrap.installed.conf"
install -o root -g root -m 0644 "$REVIEW/prode.odusport.com.https.conf" "$SITE"
nginx -t
systemctl reload nginx
```

Verificar TLS y rutas a través del Nginx real, sin depender del DNS externo:

```sh
curl -sS -o /dev/null -w 'http_root=%{http_code} -> %{redirect_url}\n' \
  --resolve prode.odusport.com:80:127.0.0.1 http://prode.odusport.com/
curl --fail --silent --show-error \
  --resolve prode.odusport.com:443:127.0.0.1 https://prode.odusport.com/api/health
curl --fail --silent --show-error \
  --resolve prode.odusport.com:443:127.0.0.1 https://prode.odusport.com/api/health/ready
openssl s_client -connect 127.0.0.1:443 -servername prode.odusport.com \
  -verify_return_error </dev/null 2>/dev/null | openssl x509 -noout -subject -issuer -dates
```

Esperado: `308` hacia `https://prode.odusport.com/`, health `200`, readiness
`200`, certificado válido para el nombre. Comprobar de nuevo BAMIRA. Si falla
`nginx -t`, no recargar: restaurar el bootstrap y reintentar.

## Fase D — refresh TLS y renovación automática

```sh
set -euo pipefail
REVIEW=/root/playpredict-https-review

install -o root -g root -m 0700 "$REVIEW/playpredict-tls-refresh" /usr/local/sbin/playpredict-tls-refresh
install -o root -g root -m 0644 "$REVIEW/playpredict-cert-renew.service" /etc/systemd/system/playpredict-cert-renew.service
install -o root -g root -m 0644 "$REVIEW/playpredict-cert-renew.timer" /etc/systemd/system/playpredict-cert-renew.timer
systemd-analyze verify /etc/systemd/system/playpredict-cert-renew.service /etc/systemd/system/playpredict-cert-renew.timer
systemctl daemon-reload
/usr/local/sbin/playpredict-tls-refresh
```

Esperado en el refresh: `PASS: certificate already served; no reload`.

Probar renovación sin alterar el certificado productivo (usa staging y HTTP-01):

```sh
certbot renew --dry-run \
  --config /etc/playpredict-https/certbot.ini \
  --config-dir /etc/letsencrypt-playpredict \
  --work-dir /var/lib/letsencrypt-playpredict \
  --logs-dir /var/log/letsencrypt-playpredict \
  --cert-name prode.odusport.com \
  --no-directory-hooks
```

Solo si el dry-run pasa, habilitar el timer:

```sh
systemctl enable --now playpredict-cert-renew.timer
systemctl start playpredict-cert-renew.service
systemctl status playpredict-cert-renew.service --no-pager
systemctl list-timers playpredict-cert-renew.timer --all
journalctl -u playpredict-cert-renew.service -n 50 --no-pager
```

## Verificaciones posteriores (obligatorias)

- `nginx -t` en 0; BAMIRA responde igual que en la línea base.
- `curl -fsS https://prode.odusport.com/api/health` y `/ready` desde una red
  externa (no solo loopback), luego de propagación DNS.
- Redirección 308 de HTTP a HTTPS; `/.well-known/acme-challenge/` sigue operativo.
- `/swagger` y `/swagger/index.html` devuelven 404.
- Ruta profunda de SPA (por ejemplo `/ligas`) devuelve el `index.html`.
- Login real y llamada autenticada con Bearer; sin `Set-Cookie` inesperado.
- `/api/uploads/...` sirve un archivo de prueba existente.
- Un `X-Forwarded-For` falso enviado desde fuera no altera la IP vista por el
  backend ni el límite de login; una segunda IP real tiene contador propio.
- Dominio de BAMIRA y `127.0.0.1:8088` sin cambios.
- `openssl s_client` valida cadena completa y nombre.
- No hay puertos nuevos en `0.0.0.0`; PlayPredict sigue solo en `127.0.0.1:8089`.
- El certificado emitido es de la tienda dedicada (no uno de BAMIRA).

## Rollback

### Rollback suave (volver al bootstrap 503)

Útil si el bloque HTTPS no valida pero el certificado ya se emitió:

```sh
set -euo pipefail
REVIEW=/root/playpredict-https-review
SITE=/etc/nginx/sites-available/prode.odusport.com
install -o root -g root -m 0644 "$REVIEW/prode.bootstrap.installed.conf" "$SITE"
nginx -t
systemctl reload nginx
```

### Rollback completo (despublicar el sitio)

```sh
set -euo pipefail
systemctl disable --now playpredict-cert-renew.timer 2>/dev/null || true
rm -f /etc/nginx/sites-enabled/prode.odusport.com
if [ -f /etc/nginx/sites-available/prode.odusport.com ]; then
  mv /etc/nginx/sites-available/prode.odusport.com \
     /root/playpredict-https-review/prode.uninstalled.conf
fi
nginx -t
systemctl reload nginx

# Retirar solo lo instalado por este procedimiento.
rm -f /etc/systemd/system/playpredict-cert-renew.service
rm -f /etc/systemd/system/playpredict-cert-renew.timer
rm -f /usr/local/sbin/playpredict-tls-refresh
systemctl daemon-reload
```

Después del rollback: verificar BAMIRA, `http://prode.odusport.com` deja de ser
servido por este sitio y PlayPredict interno sigue en `127.0.0.1:8089`.

**No borrar** `/etc/letsencrypt-playpredict`, `/etc/playpredict-https` ni los logs:
conservan evidencia y evitan reemisiones innecesarias. Un borrado de certificados
requiere autorización separada. No usar `rm -rf` sobre rutas de Nginx ni la tienda
ACME. No tocar `/etc/letsencrypt` global ni archivos de BAMIRA.

## Requisitos que no pueden comprobarse sin root/servidor

- `nginx -t`/`nginx -T` reales, versión de Nginx y orden de carga de sitios.
- Presencia y ubicación de `default_server` (riesgo BAMIRA).
- `systemd-analyze verify`, ejecución real del service/timer y próxima ejecución.
- `certbot --version`, plugins, cuenta ACME y `renew --dry-run` real.
- Permisos/ACL efectivos de `/etc/nginx`, `sites-enabled`, webroot y logs.
- Que no exista otro cliente ACME/cron para este dominio.
- Cadena TLS completa y propagación DNS desde múltiples redes.
- Respuesta de BAMIRA antes/después y su dominio público exacto.
- Que `PROD_GATEWAY`/`PROD_PROXY_IP` del VPS coincidan con la cadena de proxy.

## Validaciones locales realizadas (2026-11-11, sin VPS)

- DNS A `prode.odusport.com -> 201.32.129.24`: correcto; sin AAAA ni CAA.
- HTTP público actual: 404 (no publicado por PlayPredict).
- HTTPS público actual: el certificado presentado no valida para el nombre
  (`SEC_E_WRONG_PRINCIPAL`); no se declara HTTPS operativo.
- Sintaxis Python de `playpredict-tls-refresh` (`py_compile`): OK.
- LF sin BOM en los seis artefactos: OK.
- `.gitattributes` corregido para forzar LF en `scripts/https-prod/*` y
  `docs/nginx/*.conf` (evita romper shebang y unidades en checkout Windows).
- No se pudo ejecutar `nginx -t` ni `systemd-analyze verify` localmente: el daemon
  Docker no estaba disponible y no hay Nginx/Systemd en Windows.
- No se conectó al VPS, no se modificó producción, DNS, BAMIRA ni contenedores.
