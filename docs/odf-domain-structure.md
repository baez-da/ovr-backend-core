# Estructura de dominio ODF — Referencia

Mapa de la jerarquía de competición en ODF, relaciones entre conceptos (RSC, Event, Phase, Unit, Result, Medal, Start List) y cómo se proyectan en nuestros bounded contexts.

> Complemento de [`sessions-and-units.md`](sessions-and-units.md). Para dudas no cubiertas: NotebookLM `86c2df5c-5884-4b53-abf8-2cf74f2fb876`.

---

## Jerarquía ODF

```
Competition                    (evento global — OWG 2026)
│
├── Sport                      (Boxing, Swimming, Gymnastics...)
│   │
│   └── Discipline             (SWM, BOX, GAR, GRY, ALP...)
│       │
│       └── Event              ◄── nivel que otorga medallas
│           │
│           ├── Phase          (PREL, QUAR, SEMI, FNL-, POOL)
│           │   │
│           │   └── Unit       ◄── celda atómica de competencia
│           │       │
│           │       ├── Competitors (referencia a ParticipantCode)
│           │       ├── Results / Scores
│           │       └── Sub-Units (opcional: runs, matches de team event)
│           │
│           └── Bracket / Progression rules
│
└── Session                    ◄── agrupación horaria, NO es parte de la jerarquía
    │                              de competición (ver sessions-and-units.md)
    └── Units (por referencia SessionCode)
```

**Regla clave**: `Session` es ortogonal a la jerarquía de competición. Se conecta a Units solo por referencia (`SessionCode`).

---

## El RSC (Results System Code)

El RSC es un código jerárquico de **34 caracteres alfanuméricos** que identifica **cada nivel** de la jerarquía, no solo las Units.

### Estructura

```
 SWM  M  100M--------------   FNL-   000100--
 └┬┘  │  └──────┬─────────┘   └┬─┘   └──┬───┘
 3ch  1ch     18ch            4ch     8ch
 Disc Gen     Event            Phase   Unit+SubUnit
```

| Tramo | Chars | Contenido | Ejemplo |
|-------|-------|-----------|---------|
| Discipline | 3 | Código de disciplina | `SWM`, `BOX`, `ALP`, `GAR` |
| Gender | 1 | Género del evento | `M`, `W`, `X` (mixed/open) |
| Event | 18 | EventType (8) + Modifier (10) | `100M----` + `--------` |
| Phase | 4 | Fase | `PREL`, `QUAR`, `SEMI`, `FNL-` |
| Unit | 8 | UnitNum (6) + SubUnit (2) | `000100--` |

### Reglas del código

- Longitud fija 34 chars. Mayúsculas. Alfanumérico + `.` + `-`.
- El `-` se usa como **relleno a la derecha** cuando una parte es más corta que su máximo.
- Cada nivel tiene su RSC truncado direccionable:
  - **Event RSC** = 22 chars (Disc + Gen + Event)
  - **Phase RSC** = 26 chars (+ Phase)
  - **Unit RSC** = 34 chars (+ Unit)

### El RSC NO identifica participantes

Los participantes tienen su propio `ParticipantCode` (8 chars típicamente) y se enlazan al RSC dentro de mensajes como `DT_RESULT`, `DT_ENTRIES`, etc.

---

## Niveles y mensajes ODF

### Tabla maestra: qué mensaje apunta a qué nivel

| Mensaje | `DocumentCode` (header) | Propósito |
|---------|-------------------------|-----------|
| `DT_SCHEDULE` | Competition | Calendario completo (sessions + units) |
| `DT_SCHEDULE_UPDATE` | Competition | Cambios incrementales de calendario |
| `DT_PARTIC` | Competition | Personas por disciplina |
| `DT_PARTIC_TEAMS` | Competition | Equipos por evento |
| `DT_PARTIC_HORSES` | Competition | Caballos (equitación) |
| `DT_ENTRIES` | Event | Inscripciones en un evento |
| `DT_CONFIG` | Unit / Phase / Event | Config fija (menor nivel aplicable) |
| **`DT_RESULT`** | **Unit** | Start list + live + resultados de la unit |
| `DT_ESL` | **Unit** | Extended Start List |
| `DT_PHASE_RESULT` | **Phase** | Ranking agregado entre units de la fase |
| `DT_POOL_STANDING` | **Phase** | Standings de grupos (round-robin) |
| `DT_CUMULATIVE_RESULT` | Phase o Event | Scores acumulados multi-unit |
| `DT_BRACKETS` | **Event** | Progresión visual del bracket |
| `DT_RANKING` | **Event** | Ranking final definitivo al cerrar el evento |
| `DT_MEDALLISTS` | **Event** (c/ `Unit` por medalla) | Medallistas del evento |
| `DT_MEDALS` | Competition | Medallero global |

---

## Relación entre conceptos

### Event ↔ Phase ↔ Unit

- Un **Event** contiene 1..N **Phases** (ej: `PREL → QUAR → SEMI → FNL-`).
- Una **Phase** contiene 1..N **Units** (ej: 8 heats en la fase preliminar de natación).
- Una **Unit** es la celda atómica donde realmente compiten los atletas.

```
Event (100M Freestyle Men)
  └── Phase (PREL - Preliminaries)
  │     ├── Unit (Heat 1)
  │     ├── Unit (Heat 2)
  │     └── ... (hasta Heat 8)
  ├── Phase (SEMI)
  │     ├── Unit (Semifinal 1)
  │     └── Unit (Semifinal 2)
  └── Phase (FNL-)
        └── Unit (Final)
```

### Sub-units (8 chars = 6 UnitNum + 2 SubUnit)

Casos donde una Unit se subdivide:
- Runs en un heat de Alpine Team Event
- Matches individuales dentro de un Team Event de Table Tennis

**No** son sub-units: periodos en deportes de equipo (basquet, hockey) — esos son intermedios dentro del mismo Unit.

Cuando no hay sub-units: chars 7-8 = `--`.

### Start List vs Results

**No existe un `DT_STARTLIST` separado**. Es el mismo `DT_RESULT` con distintos `ResultStatus`:

```
DT_RESULT status=START_LIST    ◄── antes de empezar (sin scores, solo IRMs tipo DNS)
DT_RESULT status=LIVE          ◄── durante la unit
DT_RESULT status=INTERMEDIATE  ◄── breaks lógicos (fin de periodo, etc.)
DT_RESULT status=UNCONFIRMED   ◄── terminó pero pendiente
DT_RESULT status=UNOFFICIAL    ◄── resultado sin confirmar
DT_RESULT status=OFFICIAL      ◄── confirmado
DT_RESULT status=PROTESTED     ◄── bajo apelación
DT_RESULT status=PROVISIONAL   ◄── pendiente decisión IOC/CAS/IF
```

Todos apuntan a la misma Unit. Es la **state machine del resultado**, no mensajes distintos.

### Phase Result vs Cumulative Result vs Ranking

Tres mensajes que agregan, pero con propósitos distintos:

| Mensaje | Compara | Ejemplo de uso |
|---------|---------|----------------|
| `DT_PHASE_RESULT` | Competidores entre Units de **la misma Phase** | Top 16 de 8 heats de natación pasan a SEMI |
| `DT_CUMULATIVE_RESULT` | Scores que **se acumulan** a través de units/phases | Decathlon, Vela, Gimnasia All-Around/Team |
| `DT_RANKING` | Ranking **final** de todo el Event al cerrar | Posiciones 1..N del 100M masculino completo |

### El caso Gimnasia (GAR/GRY)

Punto de confusión común. La regla sigue siendo **DT_RESULT es unit-level**, pero gimnasia tiene capas extras:

**Qualification** (una subdivisión = una Unit):
- 1 `DT_RESULT` por aparato en cada subdivisión (usando `DocumentSubcode` = código del aparato)
- 1 `DT_PHASE_RESULT` por aparato con el ranking entre subdivisiones
- 1 `DT_CUMULATIVE_RESULT` por aparato para All-Around y Team

**All-Around Final**:
- 1 `DT_RESULT` por aparato (start list + resultado de ese aparato, sin rank)
- 1 `DT_CUMULATIVE_RESULT` con ranking all-around acumulado
- 1 `DT_CURRENT` para info previous/current/next

**Conclusión**: gimnasia **no rompe** la regla "DT_RESULT es unit-level" — añade más mensajes agregadores encima, pero cada DT_RESULT sigue siendo de una Unit.

### Medallas (DT_MEDALLISTS)

Según el Olympic Charter: **Event = competición que da lugar a medallas**. Por eso `DT_MEDALLISTS` tiene el RSC de Event en su header.

Pero como las medallas se deciden en Units concretas, cada `<Medal>` dentro del mensaje lleva un atributo `Unit` obligatorio con el Unit RSC donde se otorgó:

```xml
<DT_MEDALLISTS DocumentCode="<Event RSC>" ResultStatus="PARTIAL">
  <Medal Code="ME_BRONZE" Unit="<Unit RSC del match de bronce>">
    <Competitor Code="..." />
  </Medal>
</DT_MEDALLISTS>
```

Esto es crítico en **básquet, hockey, box**, donde bronce y oro se pelean en Units separadas:

- `ResultStatus=PARTIAL`: se emite cuando termina una Unit medallera (ej: bronce) pero faltan otras (oro)
- `ResultStatus=UNOFFICIAL`: tras la última Unit, 5 min antes de la ceremonia
- `ResultStatus=OFFICIAL`: confirmado

---

## Validación de afirmaciones comunes

| Afirmación | Veredicto | Matiz |
|------------|-----------|-------|
| El RSC identifica Units | ⚠️ Parcial | Identifica todos los niveles (Event, Phase, Unit) por prefijos |
| Un resultado pertenece a una Unit | ✅ | `DT_RESULT` es siempre unit-level |
| Gimnasia rompe la regla y tiene resultados por Phase | ❌ | Añade `DT_PHASE_RESULT` encima, pero `DT_RESULT` sigue siendo por Unit (con `DocumentSubcode` por aparato) |
| Las medallas se otorgan en Units específicas | ✅ | `<Medal>` lleva atributo `Unit` aunque el mensaje sea Event-level |
| Las start lists van a una Unit | ✅ | Son el mismo `DT_RESULT` con `ResultStatus="START_LIST"` |
| Las sessions contienen Units | ⚠️ Por referencia | Session y Unit son hermanos en `DT_SCHEDULE`; Unit referencia Session por `SessionCode` |
| El RSC identifica participantes | ❌ | Participantes tienen su propio `ParticipantCode` |

---

## Proyección en bounded contexts

### CompetitionConfig
- **Aggregate root**: `Event` (identidad = Event RSC)
- **Entities hijas**: `Phase`, `Unit` como **estructura**, sin scores ni horario
- La jerarquía `Event → Phase → Unit` vive aquí como grafo estructural
- **Responsabilidad**: definir *que existe* una Unit en una Phase de un Event, con su formato

### Scheduling
- **Aggregate root**: `Session` (identidad = SessionCode)
- **Relación a Unit**: **solo por referencia** (Scheduling no es dueño de Unit)
- **Enriquece la Unit**: le asigna `SessionCode`, `StartDate`, `Venue`, `FOP`, `OrderInSession`
- Emite domain event (ej: `UnitScheduledEvent`) que read models consumen

### Entries
- **Aggregate root**: `EventEntry` (participante/equipo inscrito en un Event)
- `DT_PARTIC` (importado) → `DT_ENTRIES` (pendiente)
- Un entry no tiene Unit aún — la Unit se asigna vía progresión/bracket/seeding

### DataEntry
- **Aggregate root**: `UnitResult` (identidad = Unit RSC)
- Lleva `ResultStatus` como state machine (`START_LIST → LIVE → ... → OFFICIAL`)
- Contiene `Competitors[]` con scores
- **Start List NO es aggregate separado** — es un estado del `UnitResult`
- Único módulo que muta resultados de Unit

### Progression
- **No es dueña de estado** — es un domain service
- Lee `UnitResult.OFFICIAL` → calcula avance → actualiza estructura en CompetitionConfig o crea nuevos `EventEntry` en la siguiente phase
- Emite `DT_PHASE_RESULT`, `DT_POOL_STANDING`, `DT_BRACKETS`

### Reporting
- Read models derivados: ranking final de Event (`DT_RANKING`), medallistas (`DT_MEDALLISTS`)
- Consume resultados de múltiples Units para proyectar Event-level

### DataDistribution
- Consume domain events de todos los módulos
- Emite mensajes `DT_*` hacia consumidores externos

---

## Decisiones de modelado abiertas

Puntos que vale la pena discutir al implementar:

1. **¿Unit es aggregate separado o entity hija de Event?**
   Propuesta: **aggregate separado**. Ciclo de vida operativo propio (scheduling, live results) independiente del Event. Event define *que existe*; Unit tiene vida propia.

2. **¿Sub-units (posiciones 7-8 del Unit RSC)?**
   Propuesta: **entities hijas dentro del aggregate Unit**. ODF las reserva como direccionables, pero no tienen vida independiente.

3. **¿Ranking final de Event como aggregate o proyección?**
   Propuesta: **proyección read model** en Reporting. Derivado de resultados de Units, no fuente de verdad.

4. **¿Start List como concepto modelado o solo un estado del UnitResult?**
   Propuesta: **solo estado**. ODF la trata como el mismo mensaje en estado `START_LIST`.

5. **¿Medallist como aggregate o proyección?**
   Propuesta: **entity dentro de un aggregate `EventMedals`** (por Event), con referencia a la Unit donde se otorgó. Su ciclo de vida (`PARTIAL → UNOFFICIAL → OFFICIAL`) justifica tener identidad.
