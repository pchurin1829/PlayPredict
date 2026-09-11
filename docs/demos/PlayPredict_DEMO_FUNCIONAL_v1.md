# PlayPredict — GUIÓN DEMO FUNCIONAL v1

Rama: `prueba-glm-ui` @ `61fa902`. Base Inicial v1.0 instalada.
Modelo vigente: UN pronóstico por `UserId + MatchId`
(`docs/arquitectura/DECISION_PRONOSTICO_GLOBAL_v1.0.md`).

Credenciales demo (Base Inicial v1.0, `InitialDatasetV1Seeder.cs`):
ADMIN `admin@playpredict.local` / `admin123` — PLAYER `usuario@playpredict.local` / `usuario` (login por email).

> Solo pasos con funcionalidad existente. Sin Fixture global, sin notificaciones,
> sin tab Premios por Liga (los premios se ven en `/prizes`).

## ACTO 1 — ADMIN verifica la competencia

1. Login como ADMIN → `/admin`.
2. Competencias → verificar `Torneo Clausura AFA 2026` (referencia) y
   `COPA EL NENE` (oficial jugable, `SourceLeagueId` a la fuente).
3. Equipos (`/admin/teams`) y plantel de un equipo (30 equipos / 1045 jugadores).
4. Fixture: `/admin/fixture` → Competencia → Edición → Fechas 8–11 (60 partidos).

## ACTO 2 — PLAYER participa y pronostica

5. Registro de `demo1@ejemplo.com` (rol siempre PLAYER) → Home `/`.
6. Competencias Oficiales → `COPA EL NENE` → Participar → Mis Ligas.
7. Entrar a la liga → tab Pronósticos → elegir Fecha → cargar 2-1 en un partido
   `Scheduled` futuro (`POST /api/predictions`). Modificar a 3-1
   (`PUT /api/predictions/{id}`) para mostrar edición.
8. Mis jugadores preferidos (`/preferred-players`): elegir delantero de uno de
   los equipos del partido; volver a la liga y asociarlo al pronóstico.

## ACTO 3 — ADMIN carga el resultado

9. Como ADMIN: `/admin/results` → Fecha → partido → Cargar resultado 2-1 con
   goleadores del plantel (`PUT /api/matches/{id}/result`).
   Si el caso lo pide, marcar un autogol (toggle "Es autogol", sin jugador,
   con equipo beneficiado).
10. Intentar editar el partido como fixture → 409 esperado (solo vía resultado).

## ACTO 4 — PLAYER comprueba puntaje y ranking

11. Como demo1: tab Resultados → 2-1 oficial vs pronóstico 3-1:
    "Resultado correcto", puntos según scoring (6/3/0, preferido 2/gol).
12. Tab Ranking → posición y puntos; `Mi posición` en dashboard.
13. Perfil (`/profile`): cambiar apellido → Guardar → el header/avatar se
    actualiza SIN relogin (P0-A).
14. Premios (`/prizes`): solo Published/Cerrados visibles (P0-B).

## ACTO 5 — Liga de Amigos y pronóstico compartido

15. Como demo1: Crear Liga de Amigos sobre `COPA EL NENE` (alcance completo).
    Copiar el código de invitación (visible solo al creador, Resumen).
16. Registro de `demo2@ejemplo.com` → Unirme con código → entra a la liga.
17. Como demo2, abrir el MISMO partido: el pronóstico de demo1 NO aparece
    (es por usuario); demo2 carga el suyo.
18. Como demo1, abrir la Liga de Amigos: SU pronóstico 3-1 ya está ahí —
    el MISMO registro (`UserId+MatchId`) reutilizado, sin duplicar.
    Borrarlo muestra el aviso "Se borrará en todas las Ligas".
19. Ranking de la Liga de Amigos: independiente del de `COPA EL NENE`.
20. Mis Ligas → Dejar de participar (demo2) → Reingresar: pronósticos
    conservados. El creador NO puede abandonar su propia liga (regla).

## CIERRE

21. `GET /api/rankings/me/league-positions` resume todas las posiciones.
22. Fin: sin commit, sin cambios de datos estructurales (solo demo1/demo2 y
    pronósticos de prueba, revertibles por SQL si se desea base limpia).
