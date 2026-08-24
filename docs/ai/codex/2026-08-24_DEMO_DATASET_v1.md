# Cierre MVP — Dataset Demo v1

Se incorporó `DemoDatasetV1Seeder` como fuente reproducible del circuito ADMIN + PLAYER. El detalle operativo, credenciales, catálogo, fixtures, ligas, scoring, traslado y dump/restore está en [`docs/DEMO_DATASET_v1.md`](../../DEMO_DATASET_v1.md).

Decisiones relevantes:

- Competencia/Edition son fuente deportiva; Liga Oficial es producto comercial y reutiliza fixture.
- Dataset aditivo e idempotente, sin limpieza automática de datos manuales o E2E existentes.
- Dos fuentes canónicas, cinco Fechas y quince partidos por Edition.
- Dos Ligas Oficiales y dos privadas con cinco participantes, 180 pronósticos/evaluaciones totales.
- Jugador Preferido habilitado a 2 puntos por gol, con planteles y goleadores demo.
- Logos locales genéricos de demo, reemplazables mediante `Team.LogoUrl`.
- Eliminación de Team protegida por dependencias y exportación CSV autenticada del fixture.
- No se agregó migración: el modelo relacional existente ya cubría todo el alcance.

La DB de trabajo previa fue respaldada antes de ejecutar el seed. Los registros viejos fueron inventariados y preservados.
