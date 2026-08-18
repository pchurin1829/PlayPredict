@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo ============================================
echo PlayPredict - ACTUALIZAR BASE DE DATOS
echo ============================================
echo.

echo [1/4] Verificando contenedor de base de datos...
docker compose ps db | findstr "healthy" >nul 2>&1
if errorlevel 1 (
    echo [WARN] DB no esta corriendo. Levantando servicios...
    docker compose up -d db
    echo Esperando salud de DB...
    :waitdb
    ping -n 4 127.0.0.1 >nul
    docker compose ps db | findstr "healthy" >nul 2>&1
    if errorlevel 1 goto :waitdb
)
echo [OK] DB healthy.
echo.

echo [2/4] Restaurando dotnet-ef tools...
docker compose run --rm --no-deps backend bash -c "dotnet tool restore" 2>&1
if errorlevel 1 (
    echo [ERROR] No se pudieron restaurar las herramientas.
    goto :fin
)
echo [OK] Tools restauradas.
echo.

echo [3/4] Listando migraciones pendientes...
docker compose run --rm --no-deps backend bash -c "dotnet ef migrations list --project PlayPredict.Api.csproj --no-build" 2>&1
echo.

echo [4/4] Aplicando migraciones pendientes...
echo El backend aplica migraciones automaticamente al arrancar.
echo Si hay migraciones pendientes, se aplicaran ahora.
echo.

echo Reiniciando backend para aplicar migraciones...
docker compose restart backend 2>&1

echo Esperando salud del backend...
set BACKEND_READY=0
for /l %%i in (1,1,30) do (
    ping -n 4 127.0.0.1 >nul
    curl -sf http://localhost:8006/api/health >nul 2>&1
    if not errorlevel 1 (
        set BACKEND_READY=1
        goto :backend_ok
    )
    echo Esperando... intento %%i/30
)
goto :backend_fail

:backend_ok
echo [OK] Backend healthy.
echo.
echo Verificando migraciones aplicadas:
docker compose exec db psql -U playpredict_user -d playpredict_db -c "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";" 2>&1
echo.
echo ============================================
echo ACTUALIZACION COMPLETADA
echo ============================================
goto :fin

:backend_fail
echo [ERROR] Backend no responde. Ultimas lineas de log:
docker compose logs backend --tail 30
echo.
echo ============================================
echo ACTUALIZACION FALLIDA
echo ============================================

:fin
endlocal
