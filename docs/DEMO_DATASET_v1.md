# PlayPredict — Dataset Demo v1

## 1. Objetivo y contenido

`PlayPredict Demo v1` es el dataset versionado y reproducible para probar el circuito completo ADMIN + PLAYER sin depender de carga manual acumulada en una PC.

El seeder es aditivo e idempotente: identifica sus registros por nombres/emails estables, crea sólo lo faltante y puede ejecutarse repetidamente sin duplicar competencias, ediciones, ligas, usuarios, pronósticos, evaluaciones ni goleadores.

Modelo utilizado:

1. La **Competencia de referencia** es la fuente deportiva real del fixture y resultados.
2. La **Edición** representa una temporada de esa fuente.
3. Una **Competencia del cliente** tiene nombre comercial propio y reutiliza el fixture de la referencia; nunca duplica partidos. Internamente continúa modelada como `OfficialLeague`/`League`.
4. Una **Liga de amigos** reutiliza la misma fuente, pero mantiene participantes, pronósticos y ranking propios.

## 2. Credenciales

| Perfil | Email | Contraseña |
|---|---|---|
| ADMIN | `admin@playpredict.local` | `admin123` |
| PLAYER principal | `rafael.demo@playpredict.local` | `demo123` |
| PLAYER ranking | `ana.torres@playpredict.local` | `demo123` |
| PLAYER ranking | `juan.perez@playpredict.local` | `demo123` |
| PLAYER ranking | `maria.lopez@playpredict.local` | `demo123` |
| PLAYER ranking | `pedro.gomez@playpredict.local` | `demo123` |

Las contraseñas son exclusivamente de Development. El seeder de ADMIN tiene una guarda que impide ejecutarlo fuera de ese ambiente.

## 3. Empresa, Competencias de referencia y Ediciones

La empresa demo persistida es `EL NENE`, con nombre corto `EL NENE`. Se administra desde `/admin/settings`; la UI usa `PlayPredict` únicamente como fallback cuando no existe configuración.

- `Copa Libertadores` — Edición `2026`.
- `Copa Argentina` — Edición `2026` (referencia precargada, sin fixture v1).
- `Liga Profesional Argentina` — Edición `2026`.

Libertadores y Liga Profesional tienen cinco Fechas y tres partidos por Fecha. Estas son referencias deportivas; `COPA EL NENE` y `PRODE EMPRESA DEMO` son competencias propias del cliente.

## 4. Equipos canónicos

Catálogo argentino:

- Boca Juniors, River Plate, Racing Club, Independiente, San Lorenzo.
- Estudiantes de La Plata, Gimnasia y Esgrima La Plata.
- Argentinos Juniors, Vélez Sarsfield, Rosario Central, Newell's Old Boys.
- Huracán, Lanús, Banfield, Belgrano, Talleres, Defensa y Justicia y Barracas Central.

Catálogo internacional utilizado:

- Flamengo, Palmeiras, Atlético Nacional y Peñarol.

Los nombres del v1 son las identidades canónicas. Un Team se reutiliza en todos los fixtures; no se crean copias por Competencia o Liga.

## 5. Logos

Los Teams canónicos apuntan al asset local estable:

`/assets/teams/demo-club.svg`

Es un escudo genérico de demostración, sin dependencia de URLs externas ni de derechos sobre marcas de terceros. La UI PLAYER mantiene además su sistema de escudos cromáticos por identidad. El asset puede reemplazarse en el futuro sin cambiar el modelo `Team.LogoUrl`.

## 6. Planteles

Cada Team utilizado por los fixtures v1 recibe cuatro jugadores claramente demo:

- número 1: Arquero;
- número 2: Defensor;
- número 3: Mediocampista;
- número 4: Delantero.

No requieren fotos. La carga real de fotos continúa mediante archivo/drag & drop, se almacena fuera de la DB y la UI usa avatar fallback cuando no existe foto.

## 7. Fixture y estados

En ambas Ediciones:

- Fecha 1: finalizada;
- Fecha 2: finalizada;
- Fecha 3: finalizada;
- Fecha 4: abierta y pronosticable;
- Fecha 5: futura y pronosticable.

Las Fechas 1–3 tienen resultados oficiales. Las Fechas 4–5 no tienen resultado. Sus horarios se desplazan a futuro al iniciar Development para mantener el circuito pronosticable.

## 8. Competencias EL NENE

- `COPA EL NENE`, basada en Copa Libertadores 2026, Fecha 1 → Fecha 5.
- `PRODE EMPRESA DEMO`, basada en Liga Profesional Argentina 2026, Fecha 1 → Fecha 5.

Ambas tienen cinco participantes y reutilizan los 15 partidos de sus Ediciones.

## 9. Ligas privadas

- `Los Sabaditos`, sobre Copa Libertadores 2026.
- `Los del Trabajo`, sobre Liga Profesional Argentina 2026.

Tienen cinco participantes, pronósticos y rankings propios.

## 10. Scoring y Jugador Preferido

Ambas Ediciones v1 usan:

- marcador exacto: 6 puntos;
- resultado correcto: 3 puntos;
- incorrecto: 0 puntos;
- Jugador Preferido habilitado;
- 2 puntos por gol del Jugador Preferido.

La opción es por Edition y la controla ADMIN. Si está deshabilitada, PLAYER no ve el bloque. Si está habilitada, lo ve en cada partido abierto; sin jugadores, aparece un mensaje explícito.

Los partidos finalizados incluyen goleadores demo. Existe al menos un caso verificable de marcador exacto con un Jugador Preferido que hizo dos goles: 6 + 4 = 10 puntos.

## 11. Ranking

Las Fechas 1–3 contienen pronósticos determinísticos para los cinco usuarios en las cuatro Ligas. Hay casos de:

- marcador exacto;
- resultado correcto;
- resultado incorrecto;
- puntos especiales por Jugador Preferido.

Rafael Demo participa y aparece en todos los rankings v1. Los puntos se calculan mediante `PredictionEvaluationService`; no se escriben rankings falsos.

## 12. Reconstruir o completar la demo

En una PC con la DB existente, el seeder corre automáticamente al iniciar el backend en Development. Para volver a ejecutarlo:

```powershell
docker compose restart backend
docker compose ps
```

En una PC nueva:

```powershell
Copy-Item .env.example .env
docker compose up -d --build
docker compose ps
```

El primer arranque aplica migraciones y ejecuta el Dataset Demo v1. Un segundo reinicio no duplica datos.

### Reconstrucción totalmente limpia

Esta operación elimina el volumen local; hacer primero el dump indicado abajo. Después:

```powershell
docker compose down
docker volume rm playpredict-dev_playpredict_db_data
docker compose up -d --build
```

No ejecutar la eliminación del volumen si el dump no terminó correctamente.

## 13. Llevarlo a Notebook, PC Trabajo o PC Casa

La fuente principal es Git:

1. llevar el repositorio sin `backend/bin`, `backend/obj`, `frontend/node_modules` ni dumps;
2. crear `.env` desde `.env.example`;
3. ejecutar `docker compose up -d --build`;
4. comprobar `/api/health` y entrar con las credenciales demo.

No hace falta copiar una DB para obtener el Dataset Demo v1.

## 14. Dump/restore opcional

Para conservar datos manuales además del seed:

```powershell
docker exec playpredict_db pg_dump -U playpredict_user -d playpredict_db -Fc -f /tmp/playpredict.dump
docker cp playpredict_db:/tmp/playpredict.dump backups/playpredict.dump
```

Restaurar sobre una base vacía:

```powershell
docker cp backups/playpredict.dump playpredict_db:/tmp/playpredict.dump
docker exec playpredict_db pg_restore -U playpredict_user -d playpredict_db --clean --if-exists /tmp/playpredict.dump
```

`backups/*.dump` está ignorado por Git porque puede contener datos locales.

## 15. Flujo ADMIN completo

1. Entrar con `admin@playpredict.local` / `admin123`.
2. Abrir Fuentes deportivas → Competencias deportivas.
3. Crear `Copa Demo` y su Edición `2026`.
4. Crear/seleccionar Teams canónicos.
5. Generar Fechas y cargar partidos.
6. Crear la Liga Oficial `COPA CLIENTE DEMO`.
7. Elegir fuente, Edition y alcance.
8. Configurar scoring y habilitar Jugador Preferido.
9. Hacer participar a un PLAYER y cargar su pronóstico.
10. Desde Resultados cargar marcador y goleadores.
11. Verificar puntos y ranking.
12. Desde Fixture exportar CSV para control externo; toda corrección sigue siendo manual en Editar.

## 16. Flujo PLAYER completo

1. Entrar con `rafael.demo@playpredict.local` / `demo123`.
2. En Home verificar que Pendientes sólo contenga partidos abiertos sin pronóstico, agrupados por Competencia, Liga y Fecha.
3. Entrar a una Liga v1.
4. Cargar marcador y Jugador Preferido.
5. Guardar y refrescar.
6. Confirmar persistencia.
7. Cambiar marcador y jugador; guardar cambios.
8. Consultar Resultados y Ranking.

## 17. Inventario de datos viejos detectados

La DB auditada antes del v1 contenía y conserva:

- Competencias fuente renombradas comercialmente: `COPA EL NENE - Suc La Plata` y `Copa EL NENE`.
- Ligas `Liga General - Liga Profesional (demo)` y `Liga General - Copa Libertadores (demo)`.
- privadas manuales/E2E: `Liga Amigos del Trabajo`, `LIGA 2 sabados`, `liga sabadios`, `AMIGOS DEL TRABAJO` y dos `Los del Trabajo - E2E Temporal`.
- alias: `Argentinos Jrs`, `Estudiantes`, `Gimnasia`, `Vélez`, `Newell's`, `Belgrano (Córdoba)`.
- placeholders `Equipo G/H/I/J/K/L`.
- usuarios `demo1.e2e.*`, `prediction-delete-*`, `leave-rejoin-*`, controles y videos temporales.

No se eliminaron: varias filas tienen Matches, planteles, participantes o pronósticos relacionados y la regla conservadora impide borrarlas automáticamente. En una instalación limpia no aparecen; el v1 usa únicamente sus identidades canónicas.
