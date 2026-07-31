# PlayPredict

Plataforma de Pronósticos Deportiva.

## Estado actual

Sprint 1 completado: esqueleto técnico funcional (backend, frontend, base de datos y Docker Compose para desarrollo local). Ver detalle en [PROJECT_STATUS.md](PROJECT_STATUS.md).

## Stack

- Backend: ASP.NET Core Web API (.NET 10)
- Frontend: React + Vite + TypeScript
- Base de datos: PostgreSQL 16
- ORM: Entity Framework Core (Npgsql)
- Infraestructura local: Docker Compose

## Puertos locales

| Servicio   | Puerto |
|------------|--------|
| Frontend   | 5175   |
| Backend    | 8006   |
| PostgreSQL | 5436   |

## Requisitos

- Docker Desktop (con Docker Compose)

## Configuración inicial

1. Copiar el archivo de variables de entorno de ejemplo:

   ```bash
   cp .env.example .env
   ```

   (En Windows PowerShell: `Copy-Item .env.example .env`)

2. No es necesario editar `.env` para levantar el entorno de desarrollo por defecto.

## Levantar el entorno

Desde la raíz del proyecto:

```bash
docker compose up -d --build
```

Esto construye y levanta tres servicios:

- `playpredict_db` (PostgreSQL)
- `playpredict_backend` (ASP.NET Core Web API)
- `playpredict_frontend` (React + Vite)

## Verificar que todo funciona

```bash
docker compose ps
```

Todos los servicios deben figurar como `running` (`healthy` cuando aplica).

- Backend - health check: http://localhost:8006/api/health
- Backend - info del sistema: http://localhost:8006/api/system/info
- Backend - Swagger UI: http://localhost:8006/swagger
- Frontend: http://localhost:5175

El frontend consulta en tiempo real `GET /api/system/info` del backend y muestra el estado y la versión recibidos.

## Detener el entorno

```bash
docker compose down
```

Para eliminar también los datos de PostgreSQL (volumen de base de datos):

```bash
docker compose down -v
```

## Ver logs

```bash
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f db
```

## Desarrollo sin Docker (opcional)

### Backend

```bash
cd backend
dotnet run
```

Requiere una instancia de PostgreSQL accesible en `localhost:5436` (ver `appsettings.json`).

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Documentación funcional

Ver [docs/README_DOCS.md](docs/README_DOCS.md) — índice de arquitectura (modelo conceptual, modelo de datos, reglas de negocio, Motor de Pronósticos/Puntuación), producto (pantallas, plan de implementación, roadmap) y propuestas comerciales.
