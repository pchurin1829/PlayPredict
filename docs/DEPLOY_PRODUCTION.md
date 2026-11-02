# PlayPredict ? producci?n en VPS compartido (PROD-VPS-01)

Procedimiento futuro: no ejecutar hasta aprobar el despliegue. Ubuntu 24.04,
4 vCPU, 8 GB RAM, 55 GB de disco. BAMIRA conserva sus contenedores, vol?menes,
Nginx y WordPress en `127.0.0.1:8088`. PlayPredict reside en `/opt/playpredict`.
No usar `docker system prune`, `down -v` ni modificar redes o sitios de BAMIRA.

## Arquitectura y confianza

Internet HTTPS ? Nginx del host (80/443) ? `127.0.0.1:8089` ? Caddy HTTP :80
? backend :8080 ? PostgreSQL :5432. Solo `proxy` publica un puerto Docker.
Caddy no obtiene certificados ni publica 443. Los cuatro vol?menes nombrados
existentes se conservan, incluidos los dos de Caddy aunque ya no gestione TLS.
La red bridge es exclusiva de PlayPredict; no se conecta a redes de BAMIRA.
No es `internal: true`: la aplicaci?n puede necesitar conexiones salientes.

Nginx sobrescribe X-Forwarded-For con `$remote_addr` y X-Forwarded-Proto con
`$scheme`. Caddy conf?a ?nicamente en `PROD_GATEWAY`, la direcci?n de origen
esperada del tr?fico del host por el bridge Docker; env?a una sola IP validada
con `{client_ip}` y conserva el protocolo recibido del proxy confiable.
ASP.NET conf?a ?nicamente en `PROD_PROXY_IP` (adem?s del loopback por defecto),
con ForwardLimit=1, antes de autenticaci?n y rate limiting. No confiar en todas
las redes privadas. No a?adir contenedores ajenos a esta red.
Los procesos locales del host son parte del per?metro de confianza.

Confirmar la direcci?n efectiva del host en los logs de Caddy antes del go-live;
Docker rootless, otro NAT o un CDN requieren revisar esta cadena, no ampliar
confianza indiscriminadamente. El ejemplo supone Nginx directamente en Internet,
sin CDN ni reglas globales `real_ip` que conf?en en clientes arbitrarios.
Referencia: [cabeceras de Caddy](https://caddyserver.com/docs/caddyfile/directives/reverse_proxy)
y [proxies confiables](https://caddyserver.com/docs/caddyfile/options).

CORS vac?o permite el uso same-origin HTTPS sin habilitar or?genes cruzados.
El rate limiter actual limita login a 5/minuto/IP y registro a 10/hora/IP;
no limita toda la API, y sus contadores son locales al proceso.

## 1. Revisi?n previa del VPS (solo lectura, ejecuci?n futura)

```bash
sudo ss -lntp
ip -4 route show table all
docker network ls
docker network inspect $(docker network ls -q) --format '{{.Name}} {{json .IPAM.Config}}'
docker ps --format 'table {{.Names}}\t{{.Ports}}'
df -h /opt /var/lib/docker
docker system df
sudo nginx -t
```

8089 debe estar libre y 8088 seguir reservado a BAMIRA. Elegir una subred que
no se superponga con Docker, rutas LAN/VPN ni rutas planificadas. Compose no
impone una subred predeterminada: exige `PROD_SUBNET`, `PROD_GATEWAY` y
`PROD_PROXY_IP`. Gateway y proxy deben ser IP distintas y utilizables dentro
de la subred. Si ya existe `playpredict_prod_network`, inspeccionarla antes de
reutilizarla; nunca borrar una red compartida para resolver un conflicto.
No se puede comprobar la disponibilidad real sin revisar el VPS.

## 2. Instalaci?n inicial

Docker Engine y Compose plugin deben estar instalados sin sustituir la instalaci?n
que usa BAMIRA. En los comandos, reemplazar REPO_URL y RELEASE por el repositorio
y tag/commit aprobado que incluya estos cambios (no basta el HEAD anterior).

```bash
sudo install -d -o "$USER" -g "$(id -gn)" -m 750 /opt/playpredict
git clone REPO_URL /opt/playpredict
cd /opt/playpredict
git checkout RELEASE
umask 077
cp .env.production.example .env.production
chmod 600 .env.production
# Editar .env.production en el VPS con valores reales, sin registrarlos en Git.
# Generar independientemente cada secreto con: openssl rand -hex 32
```

Variables obligatorias: `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`,
`Jwt__Key` (m?nimo 32 bytes; recomendado 32 bytes aleatorios en hexadecimal),
`PROD_SUBNET`, `PROD_GATEWAY`, `PROD_PROXY_IP`. La plantilla deja secretos y red
vac?os para que Compose rechace una instalaci?n incompleta.
Opcionales: `PROXY_BIND_IP=127.0.0.1`, `PROXY_HTTP_PORT=8089`, `Jwt__Issuer`,
`Jwt__Audience`, `Jwt__ExpiresMinutes=60`, `Cors__Origins=`.
Mantener loopback; cambiarlo a 0.0.0.0 expone un acceso que evita Nginx.
Si cambia el puerto, ajustar tambi?n el ejemplo Nginx y las comprobaciones.
`SITE_ADDRESS`, `PROXY_HTTPS_PORT` y `Forwarded__KnownNetworks` ya no se usan.

```bash
set -euo pipefail
cd /opt/playpredict
pp() { docker compose --env-file .env.production -f docker-compose.prod.yml "$@"; }
pp config --quiet
pp build --pull
pp up -d --wait db
# Este comando MODIFICA la base: ejecutar solo en el despliegue aprobado.
pp run --rm --no-deps backend dotnet PlayPredict.Api.dll --migrate-only
```

El repositorio contiene **25 migraciones**, desde `20260729011605_InitialFixtureSchema`
hasta `20261027141751_AddRegistrationLegalAndAdultSettings`. `Program.cs` aplica
pendientes antes de atender peticiones y aborta si fallan. `--migrate-only`
termina antes de ejecutar seeders. Arrancar el backend tambi?n puede modificar
la base aunque no se invoque expl?citamente una migraci?n.

Para una instalaci?n nueva con el dataset inicial aprobado, montar los Excel
porque la imagen backend no incluye `docs/`. El seeder inicial no debe ejecutarse
sobre una base existente: sustituye datos y crea usuarios con claves conocidas.

```bash
pp run --rm --no-deps -v /opt/playpredict/docs/datos-iniciales:/seed:ro backend \
  dotnet PlayPredict.Api.dll --seed-initial-v1 \
  --initial-base=/seed/PlayPredict_Base_Inicial_v1.0_2026-09-01.xlsx \
  --initial-rosters=/seed/PlayPredict_Planteles_Clausura_AFA_2026_v2.xlsx
pp up -d --wait --wait-timeout 180
pp ps
curl --fail http://127.0.0.1:8089/api/health/ready
```

El backend tiene HEALTHCHECK en su Dockerfile: curl instalado consulta
`/api/health/ready` y comprueba conectividad con PostgreSQL. `proxy` espera
`backend: service_healthy`; backend espera DB healthy. El healthcheck de Caddy
comprueba la SPA. Un estado unhealthy posterior no provoca reinicio autom?tico.

El snapshot DB0 antiguo no debe considerarse esquema actual: revisar su historial,
restaurar primero en un entorno aislado y aplicar las migraciones de esta release.
No restaurar DB0 por encima de una instalaci?n existente.

## 3. HTTPS del host y acceso inicial

Antes de publicar el sitio, usar un t?nel SSH desde una estaci?n autorizada:
`ssh -L 18089:127.0.0.1:8089 USUARIO@VPS`, abrir `http://localhost:18089`, cambiar
las claves de TODOS los usuarios iniciales y validar roles/cambio forzoso.
No publicar con credenciales demo. No incluir claves ni tokens en reportes.

Preparar DNS A/AAAA y certificado del dominio con el mecanismo ya administrado
en el host; no ejecutar herramientas que reescriban autom?ticamente sitios
existentes. El ejemplo `docs/nginx/playpredict.conf.example` requiere certificados
ya emitidos. La renovaci?n ACME debe quedar configurada y probada por el operador
(por ejemplo DNS-01); el ejemplo no configura el challenge HTTP-01.

Solo despu?s de aprobaci?n, copiar el ejemplo a un sitio NUEVO, sustituir todas
las apariciones de `jugar.example.com`, revisar certificados y puerto:

```bash
sudo cp docs/nginx/playpredict.conf.example /etc/nginx/sites-available/playpredict
sudoedit /etc/nginx/sites-available/playpredict
sudo ln -s /etc/nginx/sites-available/playpredict /etc/nginx/sites-enabled/playpredict
sudo nginx -t && sudo systemctl reload nginx
curl --fail https://DOMINIO/api/health/ready
```

No sobrescribir archivos/symlinks preexistentes. No cambiar el sitio BAMIRA,
`default_server`, firewall global ni configuraci?n global de Nginx. Comprobar BAMIRA
antes y despu?s de la recarga. El reload afecta al Nginx compartido: si `nginx -t`
falla, no recargar. Nginx limita solicitudes a 20 MB; la aplicaci?n conserva sus
propios l?mites de archivos.

## 4. Backup y actualizaci?n

Programar backups fuera del VPS con acceso restringido y probar restauraci?n.
55 GB se comparten: controlar im?genes, logs (rotados por Compose), dumps y uploads.
Para un backup coherente, poner ?nicamente el sitio PlayPredict en mantenimiento,
detener proxy y backend y mantener DB. Comandos desde `/opt/playpredict`, con `pp`
definida como arriba. No usar los scripts hist?ricos de restore sin corregirlos:
no cargan `.env.production` y hacen `docker exec` sobre el backend detenido.

```bash
set -euo pipefail
umask 077
STAMP=$(date +%Y%m%d-%H%M%S)
BACKUP_DIR="/opt/playpredict/backups/$STAMP"
mkdir -p "$BACKUP_DIR"
pp stop proxy backend
pp exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > "$BACKUP_DIR/database.dump"
pp run --rm --no-deps --entrypoint tar backend -czf - -C /data uploads > "$BACKUP_DIR/uploads.tar.gz"
pp exec -T db pg_restore --list < "$BACKUP_DIR/database.dump" > /dev/null
tar -tzf "$BACKUP_DIR/uploads.tar.gz" > /dev/null
git rev-parse HEAD > "$BACKUP_DIR/release.txt"
pp images > "$BACKUP_DIR/images.txt"
# Conservar tambi?n las im?genes exactas (o digests en un registry privado).
docker image save playpredict-prod-backend playpredict-prod-proxy | gzip > "$BACKUP_DIR/images.tar.gz"
# Copiar backups fuera del VPS y comprobar espacio antes de continuar.
git fetch --tags
git checkout NUEVA_RELEASE
pp config --quiet
pp build --pull
pp run --rm --no-deps backend dotnet PlayPredict.Api.dll --migrate-only
pp up -d --wait --wait-timeout 180
curl --fail http://127.0.0.1:8089/api/health/ready
```

Ante cualquier error, detener la secuencia y mantener PlayPredict en mantenimiento.
No ejecutar el bloque de actualizaci?n como un script que ignore c?digos de error.
Guardar `.env.production` cifrado por separado; nunca incluirlo en Git ni imprimir
`pp config` completo en logs. Fijar digests de im?genes base para reproducibilidad
estricta: las etiquetas actuales son m?viles. Presupuestar el espacio para im?genes
y copias y preferir compilar fuera del VPS si perjudica a BAMIRA.

## 5. Rollback

Solo c?digo: si la compatibilidad con el esquema nuevo est? comprobada, volver al
commit guardado, cargar im?genes guardadas y arrancar con `--no-build`.
Si la migraci?n es incompatible, restaurar DB y uploads del MISMO backup, perdiendo
los cambios posteriores. Estos comandos son destructivos y requieren autorizaci?n
espec?fica en el despliegue futuro; no ejecutar como prueba:

```bash
set -euo pipefail
pp stop proxy backend
# BACKUP_DIR debe apuntar al backup verificado elegido.
git checkout "$(cat "$BACKUP_DIR/release.txt")"
gzip -dc "$BACKUP_DIR/images.tar.gz" | docker image load
pp exec -T db sh -c 'dropdb --if-exists -U "$POSTGRES_USER" "$POSTGRES_DB" && createdb -U "$POSTGRES_USER" -T template0 "$POSTGRES_DB"'
pp exec -T db sh -c 'pg_restore --exit-on-error --no-owner --no-privileges -U "$POSTGRES_USER" -d "$POSTGRES_DB"' < "$BACKUP_DIR/database.dump"
# Vaciar solo uploads de PlayPredict para no conservar archivos posteriores al backup.
pp run --rm --no-deps --entrypoint sh backend -c 'find /data/uploads -mindepth 1 -maxdepth 1 -exec rm -rf -- {} +'
pp run --rm --no-deps -T --entrypoint tar backend -xzf - -C /data < "$BACKUP_DIR/uploads.tar.gz"
pp up -d --no-build --wait --wait-timeout 180
curl --fail http://127.0.0.1:8089/api/health/ready
```

El checkout debe conservar esta configuraci?n de VPS compartido; una release
anterior que publique 80/443 requiere portar primero esta configuraci?n. Nunca
arrancar c?digo nuevo contra una DB restaurada vieja durante el rollback.
No ejecutar downgrade EF a ciegas. Validar datos, uploads y login antes de reabrir.

## 6. Checklist go-live

- `pp config --quiet`, `pp ps` y puertos: solo `127.0.0.1:8089->80`; nada en
  0.0.0.0, [::], 5432, 8080, 80 o 443 publicado por PlayPredict.
- Nombres/vol?menes preservados; BAMIRA responde y sigue usando 8088.
- TLS v?lido, redirecci?n HTTP ? HTTPS, renovaci?n probada, DNS IPv4/IPv6 correcto.
- SPA con ruta profunda, `/api/health/ready`, `/swagger` y `/swagger/index.html` 404.
- Login, roles admin/player, fixture, pron?stico, ranking, uploads y persistencia.
- JWT aleatorio, credenciales iniciales rotadas, backups protegidos y restauraci?n probada.
- CORS no autoriza un origen ajeno. Verificar IP y esquema HTTPS de extremo a extremo
  en un entorno controlado. Enviar X-Forwarded-For falso desde fuera no debe cambiar
  la IP del cliente ni permitir eludir el l?mite de login; una segunda IP real debe
  tener contador independiente. No habilitar endpoints p?blicos de diagn?stico.
- Revisar CPU/RAM/disco durante build y carga; este cambio no garantiza capacidad
  para una carga espec?fica en el VPS compartido.

## 7. Validaci?n sin despliegue

`docker compose --env-file ARCHIVO_FICTICIO -f docker-compose.prod.yml config --quiet`
valida YAML e interpolaci?n sin arrancar servicios. Usar valores ficticios y una
red ilustrativa solo para esta validaci?n; no usar `.env` local.
Para Caddy: ejecutar `caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile`
en `caddy:2-alpine`, montando ?nicamente el Caddyfile de solo lectura y pasando
`NGINX_TRUSTED_PROXY` ficticio. No arrancar backend para validar configuraci?n:
su arranque aplica migraciones. Las pruebas de readiness usan SQLite en memoria;
ninguna validaci?n debe conectarse a una base de desarrollo o producci?n.

### Resultado local PROD-VPS-01 (2026-11-01)

- PyYAML y Docker Compose con variables ficticias: OK. Se comprob? que solo se
  publique `127.0.0.1:8089:80`, sin puertos de backend/DB ni 80/443 del host.
- Compose rechaza la ausencia de contrase?a, JWT, subnet, gateway o IP del proxy.
- `caddy validate` en `caddy:2-alpine`: OK; HTTPS autom?tico deshabilitado.
- Smoke en contenedor sin red ni puertos publicados, backend simulado: OK para
  fallback SPA, ruta uploads, IP de cliente, HTTPS, Host, Swagger 404 y wget del
  healthcheck. No equivale a validar NAT ni certificados del VPS.
- `git diff --check`: OK.
- Tests .NET de ProductionReadinessTests/AccountSecurityTests: no ejecutados con
  resultado verificable; la restauraci?n offline fall? por paquetes NuGet faltantes
  (NU1101). El SDK local es 9 y el proyecto requiere 10. No se arranc? la aplicaci?n.
- No se valid? el ejemplo con `nginx -t`: requiere el entorno/certificados reales.
  Ejecutarlo antes de cualquier recarga futura.
- No hubo conexi?n al VPS, despliegue, migraci?n, modificaci?n de bases existentes,
  cambios de secretos locales, commit ni push.
