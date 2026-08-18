@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo ============================================
echo   PlayPredict - INICIO DE SESION
echo ============================================
echo.

if not exist ".git" (
    echo [ERROR] No se encontro carpeta .git en esta carpeta.
    goto :fin
)

echo [1/7] Informacion de Git
echo --------------------------------------------
echo Rama actual:
git branch --show-current
echo.
echo Ultimo commit:
git log -1 --oneline
echo.
echo Cambios locales:
git status --short
echo.

echo [2/7] Levantando servicios PlayPredict
echo --------------------------------------------
echo Levantando DB y Backend...
docker compose up -d db backend
echo.

echo [3/7] Esperando DB...
echo --------------------------------------------
set DB_READY=0
for /l %%i in (1,1,20) do (
    ping -n 4 127.0.0.1 >nul
    docker compose ps db 2>&1 | findstr "healthy" >nul 2>&1
    if not errorlevel 1 (
        set DB_READY=1
        goto :db_ok
    )
    echo Esperando DB... intento %%i/20
)
goto :db_fail

:db_ok
echo [OK] DB healthy.
echo.

echo [4/7] Esperando Backend (aplica migraciones automaticamente)...
echo --------------------------------------------
set BACKEND_READY=0
for /l %%i in (1,1,30) do (
    ping -n 4 127.0.0.1 >nul
    curl -sf http://localhost:8006/api/health >nul 2>&1
    if not errorlevel 1 (
        set BACKEND_READY=1
        goto :backend_ok
    )
    echo Esperando backend... intento %%i/30
)
goto :backend_fail

:backend_ok
echo [OK] Backend healthy - migraciones aplicadas.
echo.

echo [5/7] Levantando Frontend...
echo --------------------------------------------
docker compose up -d frontend
echo Esperando frontend...
for /l %%i in (1,1,15) do (
    ping -n 3 127.0.0.1 >nul
    curl -sf http://localhost:5175 >nul 2>&1
    if not errorlevel 1 goto :frontend_ok
    echo Esperando frontend... intento %%i/15
)
goto :frontend_fail

:frontend_ok
echo [OK] Frontend running.
echo.

echo [6/7] Verificacion de /api/health
echo --------------------------------------------
curl -sf http://localhost:8006/api/health
echo.
echo.

echo [7/7] Estado final
echo --------------------------------------------
docker compose ps
echo.

echo ============================================
echo   INICIO DE SESION COMPLETADO
echo ============================================
echo.
echo URLs:
echo   Frontend:          http://localhost:5175
echo   Backend Swagger:   http://localhost:8006/swagger
echo   Backend health:    http://localhost:8006/api/health
echo.
echo Scripts disponibles:
echo   ACTUALIZAR_BD.bat   - Forzar actualizacion de migraciones
echo   NUEVA_MIGRACION.bat - Crear nueva migracion EF
echo   FIN_SESION.bat      - Cerrar sesion de trabajo
echo.
goto :fin

:db_fail
echo [ERROR] DB no responde.
echo Ultimas lineas de log de DB:
docker compose logs db --tail 20
echo.
echo ============================================
echo   INICIO DE SESION FALLIDO - DB NO HEALTHY
echo ============================================
goto :fin

:backend_fail
echo [ERROR] Backend no responde despues de 30 intentos.
echo Ultimas lineas de log de backend:
docker compose logs backend --tail 50
echo.
echo ============================================
echo   INICIO DE SESION FALLIDO - BACKEND NO HEALTHY
echo ============================================
echo.
echo Posibles causas:
echo   - Migracion pendiente con error
echo   - Problema de conexion a DB
echo   - Error de compilacion
echo.
echo Revise el log arriba. Si hay migraciones pendientes:
echo   ACTUALIZAR_BD.bat
goto :fin

:frontend_fail
echo [WARN] Frontend no responde aun. Puede estar iniciando.
echo Estado de Docker:
docker compose ps
echo.
echo ============================================
echo   INICIO DE SESION PARCIAL - Frontend pendiente
echo ============================================
goto :fin

:fin
endlocal
