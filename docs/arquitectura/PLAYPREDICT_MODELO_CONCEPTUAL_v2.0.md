# PLAYPREDICT_MODELO_CONCEPTUAL_v2.0

## Modelo Conceptual Oficial del MVP — Ligas y Experiencia de Usuario

Versión: 2.0

Sustituye, para las decisiones aquí descriptas, a `MODELO_CONCEPTUAL_v1.0.md`, `MODELO_CONCEPTUAL_ADMINISTRADOR_v1.0.md` y `MODELO_CONCEPTUAL_JUGADOR_v1.0.md`.

No se eliminan ni se reescriben esos documentos: se conservan como historial del proyecto (Sprints 1 a 8). Este documento es, a partir de aquí, la referencia vigente para toda nueva funcionalidad.

---

# 0. Origen de este documento

Este documento traduce a modelo conceptual un conjunto de decisiones de producto ya aprobadas por el dueño del producto para el Sprint 8.5 — *Ligas y Experiencia de Usuario*. Las decisiones no se discuten ni se replantean aquí: se documentan y se ordenan en un modelo funcional coherente.

Decisiones aprobadas (resumen, ver detalle desarrollado en las secciones siguientes):

1. Solo existen dos roles: `ADMIN` y `PLAYER`.
2. La instalación inicial contiene 3 usuarios `ADMIN` de ejemplo. Todo registro público crea siempre un `PLAYER`.
3. Existe un único Login. El Dashboard se determina por rol, sin selección manual.
4. Las Competencias Oficiales solo pueden crearse por un `ADMIN`.
5. Se incorpora **Liga** como nuevo concepto principal: la crea cualquier `PLAYER`, pertenece a una Competencia Oficial, nunca duplica Fixture ni Resultados.
6. Una Liga almacena únicamente Participantes, Pronósticos (de sus Participantes), Ranking y Premios (opcionales). **Precisión aprobada (corrige la interpretación inicial de este documento):** el Pronóstico **no** es global por Usuario+Partido. Cada Pronóstico pertenece a una Liga concreta — identidad lógica `LeagueId + UserId + MatchId` —, de modo que un mismo Jugador puede pronosticar resultados distintos para el mismo Partido en Ligas distintas. Los Partidos y Resultados Oficiales sí se comparten entre Ligas y nunca se duplican; los Pronósticos, en cambio, nunca se comparten entre Ligas. Ver Sección 9.
7. Un `PLAYER` puede crear múltiples Ligas sobre la misma Competencia Oficial, sin restricción.
8. Para el MVP, una Liga puede usar toda la Competencia o un rango de Fechas (desde–hasta). No se implementa todavía selección por fases.
9. Los jugadores se registran libremente y luego exploran Competencias Oficiales, crean una Liga o se unen a una existente mediante código.
10. El `ADMIN` administra Competencias Oficiales, Experiences, Ediciones, Fixture, Resultados, Configuración y usuarios `ADMIN`. No administra jugadores.
11. El `PLAYER` administra únicamente sus propias Ligas.

---

# 1. Objetivo del producto

PlayPredict es una plataforma configurable para crear experiencias deportivas basadas en pronósticos (ver `docs/business/PLAYPREDICT_PRODUCTO_v1.0.md`).

Hasta el Sprint 8, el producto resolvía la mitad del problema: un `ADMIN` diseñaba una Experiencia y administraba toda la competencia deportiva de punta a punta, incluidos los participantes.

A partir del Sprint 8.5, el objetivo se completa: **el propio Jugador se convierte en creador de su propia comunidad de juego** (una Liga) alrededor de una Competencia Oficial administrada centralmente, sin ninguna intervención del `ADMIN`. Esto es lo que permite que un mismo Mundial, administrado una sola vez, alimente simultáneamente miles de grupos de amigos, oficinas, familias o clubes — el escenario que `MODELO_NEGOCIO_PLAYPREDICT_v1.0.md` describe como el verdadero producto vendible: una plataforma de engagement, no un Prode aislado.

---

# 2. Conceptos principales

| Concepto | Quién lo crea | Qué contiene |
|---|---|---|
| **Experience** | ADMIN | Identidad, branding, configuración por defecto, Competencias Oficiales |
| **Competencia Oficial** | ADMIN | Ediciones, Fechas, Partidos, Resultados Oficiales |
| **Edición** | ADMIN | Fechas, configuración de puntuación |
| **Fecha (Round)** | ADMIN | Partidos |
| **Partido** | ADMIN | Resultado Oficial |
| **Pronóstico** | PLAYER | Predicción de un Usuario sobre un Partido (único por Usuario+Partido, global) |
| **Liga** ⭐ nuevo | PLAYER | Participantes, Ranking propio, Premios propios (opcionales) |
| **Usuario** | ADMIN (semilla) o auto-registro | Rol único: `ADMIN` o `PLAYER` |

La única entidad nueva de este Sprint es la **Liga**. Todo lo demás (Experience, Competencia, Edición, Fecha, Partido, Pronóstico, Resultado, Evaluación, Ranking, Premio) ya existe desde Sprints anteriores y se reutiliza sin duplicar.

---

# 3. Modelo conceptual completo

El modelo se divide ahora en dos grandes capas, claramente separadas por responsabilidad:

```
┌──────────────────────────────────────────────────────────┐
│                    CAPA DEPORTIVA OFICIAL                 │
│                     (una sola verdad, ADMIN)               │
│                                                              │
│   Experience → Competencia → Edición → Fecha → Partido     │
│                                            ↓                │
│                                   Resultado Oficial          │
└──────────────────────────────────────────────────────────┘
                              │
                              │ se referencia, nunca se duplica
                              ▼
┌──────────────────────────────────────────────────────────┐
│                 CAPA SOCIAL / COMPETITIVA                  │
│                  (múltiples verdades, PLAYER)               │
│                                                              │
│         Liga 1        Liga 2        Liga 3     ...          │
│      (Amigos)       (Oficina)      (Familia)                │
│           │              │              │                   │
│    Participantes   Participantes  Participantes             │
│    Pronósticos      Pronósticos    Pronósticos              │
│    (propios)         (propios)      (propios)               │
│      Ranking          Ranking        Ranking                │
│    Premios (op.)    Premios (op.)  Premios (op.)            │
└──────────────────────────────────────────────────────────┘
```

La Capa Deportiva Oficial es única y compartida: un solo Mundial, una sola Champions, un solo conjunto de resultados.

La Capa Social es infinitamente replicable: cualquier cantidad de Ligas puede apoyarse sobre la misma Competencia Oficial sin costo administrativo y sin que el `ADMIN` se entere de que existen.

El Fixture y los Resultados Oficiales viajan de la Capa Deportiva hacia la Capa Social por referencia (nunca se copian). El Pronóstico, en cambio, **nace y vive dentro de la Capa Social**: pertenece siempre a una Liga concreta, nunca a la Competencia. Un mismo Jugador puede pronosticar valores distintos para el mismo Partido en cada Liga en la que participe (ver Sección 9).

---

# 4. Jerarquía del dominio

```
Experience
└── Competencia Oficial                              (ADMIN)
    └── Edición
        ├── Configuración de puntuación               (ADMIN)
        └── Fecha
            └── Partido
                └── Resultado Oficial                  (ADMIN)

Liga                                                    (PLAYER)
├── Competencia Oficial de referencia                  (FK, obligatoria)
├── Alcance: Competencia completa | Fecha X → Fecha Y
├── Código de invitación
├── Participantes (Usuarios PLAYER)
├── Pronóstico (por Participante, por Partido)          (PLAYER, pertenece a esta Liga)
│    └── Evaluación                                     (motor, automático)
├── Ranking de Liga                                     (calculado)
└── Premios de Liga                                     (opcionales)
```

Nótese que la Liga **no cuelga** de la Competencia como un hijo más en el árbol deportivo: es una entidad de otra naturaleza que **referencia** a la Competencia (y opcionalmente a un rango de Fechas dentro de ella) para saber qué Partidos y Resultados Oficiales existen. A diferencia de esos dos, el Pronóstico no se lee desde la Competencia: se crea y pertenece directamente a la Liga.

---

# 5. Flujo del Administrador

```
Login
  │
  ▼
Dashboard ADMIN
  │
  ├── Administrar Experiences
  ├── Crear/administrar Competencias Oficiales
  ├── Crear Ediciones → Fechas → Partidos
  ├── Cargar Resultados Oficiales  ──────► dispara Motor de Puntuación
  ├── Configurar puntuación (Edición / Experience)
  └── Administrar usuarios ADMIN
```

El `ADMIN` nunca ve, crea ni administra Ligas, Participantes ni Jugadores. Su universo termina en el Resultado Oficial y la configuración de las reglas del juego. Esto es una reducción de alcance respecto del panel actual (que hoy administra *todos* los usuarios, ver Sección 8 del informe de impacto).

---

# 6. Flujo del Jugador

```
Registro (siempre rol PLAYER)
  │
  ▼
Login
  │
  ▼
Dashboard PLAYER
  │
  ├── Explorar Competencias Oficiales (solo lectura: fixture, resultados)
  │
  ├── Crear una Liga
  │     ├── Elegir Competencia Oficial
  │     ├── Elegir alcance: completa o Fecha X → Fecha Y
  │     ├── Definir nombre
  │     └── Se genera código de invitación → el creador queda como Participante
  │
  ├── Unirse a una Liga existente con un código
  │
  └── Para cada una de sus Ligas:
        ├── Cargar/editar sus Pronósticos sobre los Partidos del alcance de esa Liga
        ├── Ver Ranking de la Liga
        ├── Ver Premios de la Liga (si existen)
        └── Si es el creador: administrar la Liga
              (editar datos, ver Participantes, definir Premios)
```

Un mismo Jugador puede pertenecer a cualquier cantidad de Ligas simultáneamente, incluso varias sobre la misma Competencia Oficial (ej. "Liga Amigos" y "Liga Oficina", ambas sobre el mismo Mundial) — y puede cargar un pronóstico **distinto** para el mismo Partido en cada una, porque el Pronóstico ya no es global: pertenece a la Liga en la que se cargó (ver Sección 9). El Pronóstico ya no se carga "sobre la Competencia" de forma independiente de una Liga: para pronosticar, el Jugador siempre debe estar dentro de una Liga.

---

# 7. Modelo de entidades

## Entidad nueva: `League` (Liga)

| Campo | Descripción |
|---|---|
| `Id` | Identificador |
| `Name` | Nombre de la Liga (ej. "Liga Amigos") |
| `CompetitionId` | Competencia Oficial de referencia (obligatoria) |
| `ScopeType` | `FullCompetition` \| `RoundRange` |
| `RoundFromId` / `RoundToId` | Solo si `ScopeType = RoundRange`; deben pertenecer a la misma Competencia |
| `InviteCode` | Código único para unirse |
| `CreatedByUserId` | Jugador creador (administrador de la Liga) |
| `CreatedAtUtc` / `UpdatedAtUtc` | Auditoría |

## Entidad nueva: `LeagueParticipant`

| Campo | Descripción |
|---|---|
| `Id` | Identificador |
| `LeagueId` | Liga a la que pertenece |
| `UserId` | Jugador participante |
| `JoinedAtUtc` | Fecha de ingreso |

Único por (`LeagueId`, `UserId`) — un Jugador no puede unirse dos veces a la misma Liga. El creador de la Liga se inserta automáticamente como Participante al crearla.

## Entidad que se modifica: `Prediction` (Pronóstico)

**El Pronóstico deja de ser global.** Pasa a pertenecer siempre a una Liga concreta.

| Campo | Descripción |
|---|---|
| `Id` | Identificador |
| `LeagueId` ⭐ nuevo | Liga a la que pertenece este Pronóstico (obligatorio) |
| `MatchId` | Partido pronosticado |
| `UserId` | Jugador que pronosticó (debe ser Participante de `LeagueId`) |
| `PredictedHomeScore` / `PredictedAwayScore` | Marcador pronosticado |
| `CreatedAtUtc` / `UpdatedAtUtc` | Auditoría |

**Identidad lógica**: `LeagueId + UserId + MatchId`, única. Un mismo Jugador puede tener un Pronóstico distinto para el mismo Partido en cada Liga en la que participe.

## Entidades existentes que se reutilizan sin cambios conceptuales

- `PredictionEvaluation`, `Match`, `Round`, `Edition`, `Competition`, `Experience`.
- `Prize` (Premio): se extiende para admitir un tercer ámbito, `League`, además de los existentes `Edition`/`Round`.

## Entidad que se simplifica: `User` / roles

Rol único por Usuario, restringido a `ADMIN` o `PLAYER` (hoy ya son solo 2 roles con otro nombre — ver informe de impacto).

---

# 8. Relación entre Competencia Oficial y Liga

```
Competencia Oficial: "Mundial 2026"           (una sola, administrada por ADMIN)
        │
        │  referenciada por N Ligas independientes
        │
        ├── Liga "Amigos del Barrio"     (creada por Jugador A)
        ├── Liga "Oficina Ventas"        (creada por Jugador B)
        ├── Liga "Familia Pérez"         (creada por Jugador C)
        └── Liga "Hinchas Club X"        (creada por Jugador D)
```

- Una Liga pertenece exactamente a **una** Competencia Oficial (relación N:1, `League.CompetitionId`).
- Una Competencia Oficial puede ser el origen de **cualquier cantidad** de Ligas (0, 1 o miles).
- La Liga nunca crea, edita ni copia Ediciones, Fechas, Partidos ni Resultados: siempre lee los de la Competencia Oficial que referencia.
- Si la Liga define un rango de Fechas (`RoundRange`), ese rango filtra qué Partidos de la Competencia entran en el Ranking y los Premios de esa Liga — nunca cambia el Fixture ni el Resultado en sí.

---

# 9. Relación entre Liga y Pronósticos

**Decisión definitiva (corrige la interpretación inicial de la versión anterior de este documento): el Pronóstico pertenece a la Liga. No es global.**

Identidad lógica del Pronóstico: `LeagueId + UserId + MatchId`. Un mismo Jugador pronostica de forma **independiente** en cada Liga: puede cargar un resultado distinto para el mismo Partido según en qué Liga lo esté cargando.

```
Partido: "Argentina vs Brasil"                (uno solo, compartido — dato de la Competencia Oficial)
        │
        │  Pablo participa en 2 Ligas sobre el Mundial
        │
        ├── Liga "Familia"   → Pronóstico de Pablo: 2-0   (pertenece a esta Liga)
        └── Liga "Oficina"   → Pronóstico de Pablo: 1-1   (pertenece a esta otra Liga)

Cada Pronóstico puntúa ÚNICAMENTE dentro de su propia Liga.
```

Lo que **sí** se comparte y nunca se duplica entre Ligas es exclusivamente la Capa Deportiva Oficial: el Partido y su Resultado Oficial. Lo que **nunca** se comparte entre Ligas es el Pronóstico — cada Liga tiene su propio conjunto de Pronósticos, aunque referencien el mismo Partido.

**Ranking de Liga**: se calcula exclusivamente con los Pronósticos (y sus Evaluaciones) cargados **dentro de esa Liga**. No hace falta filtrar por lista de Participantes para saber "de quién son los pronósticos que cuentan" — alcanza con mirar los Pronósticos cuyo `LeagueId` sea el de esa Liga, ya que un Pronóstico no puede existir sin pertenecer a una Liga y a un Participante de ella. Sí se sigue filtrando por el alcance de Fechas de la Liga (Competencia completa, o rango `RoundFromId`–`RoundToId`) cuando corresponda.

Esto reutiliza la misma lógica de `RankingService` (Sprint 6): nunca calcula ni duplica puntos, solo consulta Evaluaciones existentes y las ordena. El único cambio es que ahora agrupa por `LeagueId` en vez de agrupar globalmente por Edición/Fecha.

**Consecuencia sobre la Evaluación**: `PredictionEvaluation` sigue siendo 1 a 1 con `Prediction` (por `PredictionId`), sin cambios — simplemente ahora hay potencialmente varias `Prediction` distintas (una por Liga) para el mismo Usuario+Partido, y por lo tanto varias `PredictionEvaluation` independientes, una por cada una.

---

# 10. Ventajas de este modelo respecto del modelo anterior

- **Separa definitivamente el dato deportivo del dato social.** Antes, el `ADMIN` era responsable implícito de todo, incluidos los jugadores. Ahora administra un solo Fixture; miles de comunidades se construyen solas encima.
- **Escala sin desarrollo ni operación adicional por cliente.** Un mismo Mundial soporta una cantidad ilimitada de Ligas sin que nadie del equipo de PlayPredict intervenga — esto es exactamente el objetivo descripto en `PLAYPREDICT_ESTRATEGIA_v1.0.md` ("una organización pueda crear una experiencia completa sin escribir código"), llevado ahora también al Jugador.
- **Refuerza el modelo de negocio de engagement** (`MODELO_NEGOCIO_PLAYPREDICT_v1.0.md`): cada Liga creada por un Jugador es, en los hechos, viralización orgánica — cada código de invitación compartido trae nuevos registros sin costo de adquisición.
- **Cero duplicación de datos deportivos.** Fixture y Resultados Oficiales se leen una sola vez desde su fuente (la Competencia Oficial); ninguna Liga los copia. Los Pronósticos, en cambio, son intencionalmente propios de cada Liga — esto es lo que le da sentido competitivo real a tener varias Ligas: en cada una se compite de forma independiente, incluso sobre los mismos Partidos.
- **Reutiliza motores existentes sin modificarlos.** `RankingService` y el modelo de `Prize` se extienden con un filtro/ámbito nuevo; no se reescribe ningún motor.
- **Roles más simples.** Dos roles (`ADMIN`/`PLAYER`) en vez de una matriz de permisos más amplia, reduce superficie de error de autorización.

---

# 11. Arquitectura prevista para futuras ampliaciones

Explícitamente fuera de este Sprint 8.5, pero compatible con el modelo aquí definido sin requerir rediseño:

- Selección de la Liga por fases del torneo (ej. "solo octavos en adelante"), no solo por rango de Fechas.
- Ligas públicas y descubribles (hoy solo se accede por código de invitación).
- Co-administradores dentro de una misma Liga (hoy solo el creador administra).
- Una Liga combinando más de una Competencia Oficial (ej. "Liga Fútbol + Básquet").
- Premios de Liga patrocinados, integrándose con el concepto de Sponsor ya definido a nivel Experience.
- Estadísticas y logros por Liga (alineado con `MODELO_CONCEPTUAL_JUGADOR_v1.0.md`, Secciones 11-12, y con el Roadmap: Sprint 12 Estadísticas, Sprint 15 Gamificación).
- Ranking histórico/mensual por Liga.

Ninguna de estas ampliaciones requiere modificar la relación fundamental Competencia Oficial ↔ Liga ↔ Participante definida en este documento.

---

# 12. Resumen visual del modelo completo

```
                          ┌───────────────┐
                          │   Experience   │  (ADMIN)
                          └───────┬───────┘
                                  │
                          ┌───────▼───────┐
                          │  Competencia   │  (ADMIN)
                          │    Oficial     │
                          └───────┬───────┘
                                  │
                     ┌────────────┼────────────┐
                     │                          │
             ┌───────▼───────┐          ┌───────▼───────┐
             │    Edición     │          │  Liga (N)      │  (PLAYER)
             │   (ADMIN)      │          │  referencia →  │
             └───────┬───────┘          │  esta Competencia
                     │                   └───────┬───────┘
             ┌───────▼───────┐                   │
             │     Fecha      │◄──────────────────┘ (alcance opcional:
             │   (ADMIN)      │                       Fecha X → Fecha Y)
             └───────┬───────┘
                     │
             ┌───────▼───────┐
             │    Partido     │
             │   (ADMIN)      │
             └───────┬───────┘
                     │
              ┌──────▼──────┐
              │  Resultado   │
              │   Oficial    │
              │   (ADMIN)    │
              └──────┬──────┘
                     │  compartido y leído por referencia
                     │  (nunca copiado) desde cada Liga
                     ▼
              ┌─────────────────────────────┐
              │           Liga (N)            │
              │  ┌──────────────────────────┐ │
              │  │ Pronóstico (por          │ │
              │  │ Participante y Partido)  │ │  (PLAYER, pertenece a ESTA Liga)
              │  │       ↓                   │ │
              │  │   Evaluación (motor)      │ │
              │  └──────────────────────────┘ │
              │             ↓                  │
              │      Ranking de Liga            │
              │      Premios de Liga (op.)      │
              └─────────────────────────────┘
```

---

# 13. Definición final

PlayPredict deja de ser una plataforma donde el `ADMIN` administra tanto el deporte como a la comunidad. A partir del Sprint 8.5, el `ADMIN` administra una única fuente de verdad deportiva (Experience → Competencia → Edición → Fecha → Partido → Resultado), y cualquier `PLAYER` puede construir sobre ella, sin límite y sin intervención, su propia comunidad de juego a través de Ligas.

Este es el modelo conceptual oficial vigente del MVP a partir de este Sprint. Toda funcionalidad futura deberá ser consistente con él.
