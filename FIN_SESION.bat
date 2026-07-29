@echo off
setlocal

cd /d "%~dp0"

echo ============================================
echo   PlayPredict - FIN DE SESION
echo ============================================
echo.

echo Rama actual:
git branch --show-current
echo.

echo Cambios locales (git status --short):
git status --short
echo.

echo Ultimo commit:
git log -1 --oneline
echo.

echo Estado de Docker (docker compose ps):
docker compose ps
echo.

echo ============================================
echo   RECORDATORIOS (acciones manuales)
echo ============================================
echo   1. Actualizar SESSION.md con el estado real.
echo   2. Actualizar PROJECT_STATUS.md si cambio el estado del proyecto.
echo   3. Ejecutar las pruebas correspondientes.
echo   4. Revisar los cambios (git diff / git status).
echo   5. Hacer git add, git commit y git push manualmente.
echo.
echo Este script NO modifica Git ni detiene Docker.

endlocal
