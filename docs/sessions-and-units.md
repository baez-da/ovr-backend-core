# Sessions y Units en ODF — Referencia rápida

Documento de referencia para evitar consultas repetidas al oráculo (NotebookLM) sobre conceptos base de ODF relacionados con calendario y estructura de competencia.

> Fuente: ODF Foundation Principles + Data Dictionaries (OWG 2026). Para dudas no cubiertas aquí, consultar NotebookLM `86c2df5c-5884-4b53-abf8-2cf74f2fb876`.

---

## Definiciones

**Session**: agrupación lógica de una o más **units** que ocurren sin pausas significativas, en un **venue**, en un **día**.

**Unit**: la unidad atómica de competencia — un partido, carrera, combate o ronda concreta. Identificada por su **RSC** (34 chars).

Regla de oro: **Session y Unit son hermanos, no padre-hijo**. La Unit referencia a la Session por `SessionCode`.

---

## Para qué sirve una Session

- Dicta apertura/cierre de puertas del venue.
- Se alinea con ticketing y broadcast.
- Solo aplica a actividades en field of play gestionadas por OVR (competencia oficial + entrenamiento oficial).
- **No** aplica a: conferencias de prensa, actividades en Villa Olímpica, team captain meetings, draws.

---

## Codificación

### SessionCode — formato `DDDnn`

- `DDD` = código de disciplina (3 letras)
- `nn` = número secuencial dentro de la disciplina

Ejemplos: `ATH01`, `ARC02`, `BDM01`, `BDM02`.

### Entrenamiento oficial — formato `DDDTa`

- `T` fija (training)
- `a` = letra secuencial (`a`, `b`, `c`...)

Ejemplos: `SKJTa`, `SKJTb`.

### Reglas críticas de codificación

1. **Congelados ~18 meses antes de los Juegos** (por impacto en ticketing).
2. **Nunca se renumeran** — aunque el orden cronológico se rompa por cambios de calendario.
3. **Inmutables una vez asignados** — incluso si la session se mueve de día.

---

## Estructura en `DT_SCHEDULE`

```xml
<Competition>
  <Session SessionCode="BDM01"
           StartDate="2020-07-30T08:00:00+09:00"
           EndDate="2020-07-30T14:00:00+09:00"
           Leadin="5:00"
           Venue="MFS"
           VenueName="Musashino Forest Sp Plaza">
    <SessionName Language="ENG" Value="Badminton Session 1" />
  </Session>

  <Session SessionCode="BDM02"
           StartDate="2020-07-30T15:30:00+09:00"
           EndDate="2020-07-30T18:30:00+09:00"
           Venue="MFS" VenueName="Musashino Forest Sp Plaza">
    <SessionName Language="ENG" Value="Badminton Session 2" />
  </Session>

  <Unit Code="BDMMSINGLES-----------FNL-0001----"
        SessionCode="BDM01"
        ScheduleStatus="SCHEDULED"
        StartDate="2020-07-30T10:00:00+09:00" />
</Competition>
```

Observaciones:

- `<Session>` y `<Unit>` son elementos **hermanos** bajo `<Competition>`.
- La relación es **por referencia** (`Unit.SessionCode` apunta a `Session.SessionCode`).
- Una Session puede contener N Units. Una Unit pertenece a una sola Session.

---

## Campos de una Session

| Campo | M/O | Descripción |
|-------|-----|-------------|
| `SessionCode` | **M** | Identificador único (`DDDnn`), max 10 chars |
| `StartDate` | **M** | Inicio programado (DateTime o Date en etapas tempranas) |
| `EndDate` | **M** | Fin programado |
| `HideStartDate` | O | `Y` si el horario es estimado/bajo embargo |
| `HideEndDate` | O | `Y` si el fin es estimado/bajo embargo |
| `Leadin` | O | Tiempo (`mm:ss`) entre inicio de session y primera unit |
| `Venue` | **M** | Common code del venue |
| `VenueName` | **M** | Descripción ENG del venue |
| `SessionStatus` | O | Solo `CANCELLED`. Si no se envía, se asume scheduled. |
| `SessionType` | O | Common code (morning, afternoon...) |
| `Medal` | O | Medallas de oro planificadas en esta session |
| `FOP` | O | Fields of play a usar (solo pre-Games) |
| `ModificationIndicator` | O | `N` (new) o `U` (update) — solo en `DT_SCHEDULE_UPDATE` |
| `SessionName` | **M** | Nombre multilingüe (1..N con `Language` + `Value`) |

**Ciclo de vida de Session**: binario. Existe (scheduled implícito) o `CANCELLED`. No hay "running" ni "finished" — a diferencia de Units.

---

## Representación visual

```
Competition (DT_SCHEDULE)
│
├── Session "BDM01" (30/Jul 08:00–14:00 @ MFS)
│   ├── Unit BDMMSINGLES-...0001  (08:05)
│   ├── Unit BDMMSINGLES-...0002  (08:30)
│   └── Unit BDMMSINGLES-...0003  (09:00)
│
├── Session "BDM02" (30/Jul 15:30–18:30 @ MFS)
│   ├── Unit BDMMSINGLES-...0004  (15:35)
│   └── Unit BDMMSINGLES-...0005  (16:00)
│
└── Session "BDM03" (31/Jul 08:00–14:00 @ MFS)
    └── ...
```

El orden dentro de una session lo define `StartDate` de cada Unit.

---

## Casos especiales — Cambios de calendario

Estos son los escenarios que el módulo de **Scheduling** debe manejar:

| Escenario | Qué pasa con la Session | Qué pasa con las Units |
|-----------|------------------------|------------------------|
| Units movidas a otra session existente *(tennis, sailing por clima)* | Sin cambios | Se reasignan (nuevo `SessionCode`) |
| Todas las units movidas a otra session existente *(rowing por viento)* | Original → `CANCELLED` | Se reasignan a otras sessions |
| Session completa movida a otro día *(alpine skiing)* | Se mueve con su code | Se mueven con la session |
| Units movidas a session nueva *(clima deteriora mid-session)* | Se crea session nueva con próximo code secuencial | Se asignan a la nueva |
| Unit interrumpida que termina en otra session | — | **Conserva el `SessionCode` original donde empezó** |

> Los codes pueden quedar fuera de orden cronológico. No se renumeran.

---

## `DT_SCHEDULE_UPDATE` — Cambios incrementales

- **No** reenvía todo el calendario. Solo envía las sessions/units modificadas.
- Clave de identificación: `Unit @Code`.
- `ModificationIndicator`: `N` (nueva) o `U` (actualización).
- Si llega un `DT_SCHEDULE` completo → descartar todos los `DT_SCHEDULE_UPDATE` previos.

Cancelación de session (todas las units movidas):

```xml
<Session SessionCode="ROW03"
         SessionStatus="CANCELLED"
         ModificationIndicator="U" ... />
```

---

## Implicaciones para nuestro bounded context de Scheduling

1. **Session es un aggregate propio**. Tiene identidad (`SessionCode`), ciclo de vida y reglas de negocio (no renumerar, venue obligatorio, leadin).
2. **Unit → Session es referencia, no composición**. Una unit puede cambiar de session; la session no la "contiene".
3. **`SessionCode` es inmutable** una vez asignado, incluso al mover de día.
4. **Status de session es binario**: existe o `CANCELLED`. No mezclar con estados de unit (`SCHEDULED`/`LIVE`/`FINISHED`...).
5. **Leadin es operativo**: tiempo entre arranque formal de session y primera unit real.
6. **La relación "Unit tiene horario" vive en Scheduling**, no en CompetitionConfig. CompetitionConfig define que la Unit existe estructuralmente (Round of 16, Match 3); Scheduling le pone `SessionCode` + `StartDate` + `Venue` + `FOP`.

---

## Módulos que tocan Session/Unit

| Módulo | Rol sobre Session | Rol sobre Unit |
|--------|-------------------|----------------|
| CompetitionConfig | — | Define existencia estructural (RSC, fase, formato) |
| Scheduling | **Owner** del aggregate Session | Asigna `SessionCode`, horario, venue, FOP |
| DataEntry | Consume (read model: "qué units hay hoy en Mat 1") | Registra scores, estado competitivo |
| Progression | — | Lee resultado y avanza a siguiente fase |
| DataDistribution | Emite `<Session>` en `DT_SCHEDULE` / `DT_SCHEDULE_UPDATE` | Emite `<Unit>` con `SessionCode` |

Comunicación entre módulos: **domain events** (ej: `UnitScheduledEvent` con `SessionCode`, `StartDate`, `FOP`) y **contracts** (`IScheduleReader`).
