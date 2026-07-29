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

echo Carpeta actual:
cd
echo.

echo Rama actual:
git branch --show-current
echo.

echo Ultimo commit:
git log -1 --oneline
echo.

echo Cambios locales (git status --short):
git status --short
echo.

echo Remoto configurado:
git remote -v
echo.

set HAS_ORIGIN=0
for /f %%r in ('git remote') do (
    if "%%r"=="origin" set HAS_ORIGIN=1
)

if "!HAS_ORIGIN!"=="1" (
    echo Ejecutando git fetch origin...
    git fetch origin
    echo.
    echo Estado respecto de origin/main:
    git status -sb
) else (
    echo No hay remoto "origin" configurado. Se omite git fetch.
)
echo.

echo Estado de Docker (docker compose ps):
docker compose ps
echo.

echo URLs del entorno:
echo   Frontend:        http://localhost:5175
echo   Backend Swagger: http://localhost:8006/swagger
echo   Backend health:  http://localhost:8006/api/health
echo.

echo Si los servicios no estan corriendo, ejecutar:
echo   docker compose up -d
echo.

echo Revisar SESSION.md antes de comenzar.

:fin
endlocal
