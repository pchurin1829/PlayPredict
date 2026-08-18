@echo off
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo ============================================
echo PlayPredict - NUEVA MIGRACION
echo ============================================
echo.

if "%~1"=="" (
    echo [ERROR] Uso: NUEVA_MIGRACION.bat ^<NombreMigracion^>
    echo.
    echo Ejemplo:
    echo   NUEVA_MIGRACION.bat AddPlayerStats
    echo.
    echo Reglas:
    echo   - El nombre debe ser descriptivo en PascalCase.
    echo   - Se generaran .cs, .Designer.cs y se actualizara el ModelSnapshot.
    echo   - Los tres archivos DEBEN ir commiteados juntos.
    echo   - NUNCA crear el .cs manualmente sin EF tooling.
    goto :fin
)

set MIGRATION_NAME=%~1

echo Nombre de migracion: %MIGRATION_NAME%
echo.

echo [1/4] Restaurando dotnet-ef tools...
docker compose run --rm --no-deps backend bash -c "dotnet tool restore" 2>&1
if errorlevel 1 (
    echo [ERROR] No se pudieron restaurar las herramientas.
    goto :fin
)
echo [OK] Tools restauradas.
echo.

echo [2/4] Generando migracion con EF Core...
docker compose run --rm --no-deps backend bash -c "dotnet ef migrations add %MIGRATION_NAME% --output-dir Migrations --project PlayPredict.Api.csproj" 2>&1
if errorlevel 1 (
    echo [ERROR] Fallo la generacion de la migracion.
    goto :fin
)
echo [OK] Migracion generada.
echo.

echo [3/4] Verificando artefactos generados...
echo Buscando archivos de migracion...

set FOUND_CS=0
set FOUND_DESIGNER=0
set FOUND_SNAPSHOT=0

for /f "delims=" %%f in ('dir /b /s "backend\Migrations\*_%MIGRATION_NAME%.cs" 2^>nul') do (
    set FOUND_CS=1
    echo [OK] .cs: %%f
)
for /f "delims=" %%f in ('dir /b /s "backend\Migrations\*_%MIGRATION_NAME%.Designer.cs" 2^>nul') do (
    set FOUND_DESIGNER=1
    echo [OK] .Designer.cs: %%f
)
if exist "backend\Migrations\PlayPredictDbContextModelSnapshot.cs" (
    set FOUND_SNAPSHOT=1
    echo [OK] ModelSnapshot: backend\Migrations\PlayPredictDbContextModelSnapshot.cs
)

echo.

if "!FOUND_CS!"=="0" (
    echo [ERROR] No se encontro el archivo .cs de la migracion.
    goto :fail
)
if "!FOUND_DESIGNER!"=="0" (
    echo [ERROR] No se encontro el archivo .Designer.cs de la migracion.
    echo Esto indica un problema con EF tooling. NO continuar.
    goto :fail
)
if "!FOUND_SNAPSHOT!"=="0" (
    echo [ERROR] No se encontro el ModelSnapshot actualizado.
    goto :fail
)

echo [4/4] Todos los artefactos verificados.
echo.
echo ============================================
echo MIGRACION CREADA: %MIGRATION_NAME%
echo ============================================
echo.
echo PROXIMOS PASOS:
echo   1. Revisar el archivo .cs generado en backend\Migrations\
echo   2. Si la migracion incluye datos seed, agregarlas en el Up()
echo   3. Commitear los 3 archivos juntos:
echo      git add backend\Migrations\
echo      git commit -m "migration: %MIGRATION_NAME%"
echo   4. En otras PCs: git pull ^&^& INICIO_SESION.bat
echo      (las migraciones se aplican automaticamente al arrancar)
echo.
echo PARA APLICAR AHORA EN ESTA PC:
echo   ACTUALIZAR_BD.bat
echo.
goto :fin

:fail
echo.
echo ============================================
echo ERROR: ARTEFACTOS INCOMPLETOS
echo ============================================
echo Se encontraron .cs=!FOUND_CS! .Designer=!FOUND_DESIGNER! Snapshot=!FOUND_SNAPSHOT!
echo Elimine cualquier archivo parcial y reintente.

:fin
endlocal
