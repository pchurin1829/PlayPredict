# Reset de datos de juego demo

El reset conserva empresas, experiencias, competencias, ediciones, fechas, partidos,
equipos, planteles, ligas y participantes. Sólo elimina pronósticos, evaluaciones y
goleadores de las experiencias `PlayPredict Demo*`, y vuelve sus partidos a estado
`Scheduled` sin resultado.

Desde `backend`:

```powershell
$env:ConnectionStrings__DefaultConnection='Host=localhost;Port=5436;Database=playpredict_db;Username=playpredict_user;Password=playpredict_password'
dotnet run --no-build -- --reset-demo-game-data
```

La operación usa una transacción y es idempotente. Antes de ejecutarla en otra base,
verificar que las ediciones a limpiar pertenezcan a una Experience cuyo nombre comience
con `PlayPredict Demo`.
