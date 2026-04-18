# CompetitionConfig MVP — Design Spec

**Date**: 2026-04-17
**Status**: Draft — pending user review
**Step**: 1 of 5 in the MVP roadmap for Eugenio's demo

## Context

This is the first implementation step of the thin end-to-end MVP slice for OVR, described in the project memory `project_mvp_roadmap`. The goal of this step is to build the CompetitionConfig module's MVP: create Event instances and generate single-elimination bracket structure (Phases + empty Units) that downstream modules can consume.

**Deliverable**: an operator can `POST /events` and `POST /events/{rsc}/generate-structure` to end up with an Event that has Phases and all bracket Units existing structurally, with an `EventStructureGeneratedEvent` emitted for downstream modules.

**Scope**: boxing pilot only. Single-elimination format only. Size 2 to 128 entries. No UI yet — only API. No Entries coupling (size is explicit input).

**Reference docs**:
- `docs/odf-domain-structure.md` — ODF hierarchy, RSC structure, phase codes, message mapping
- `docs/sessions-and-units.md` — Session/Unit relationship
- `CLAUDE.md` — architecture conventions (vertical-slice, 3 validation levels, common codes)

## Design decisions summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| `Unit` boundary | Separate aggregate | Operational lifecycle independent of Event |
| `Phase` boundary | Entity inside `Event` aggregate | Structure invariants protected by Event |
| Lineup ownership | DataEntry (`UnitResult` status = `START_LIST`) | Aligned with ODF; single source of truth |
| Bracket generation trigger | Manual, explicit `size` | Preserves operator control, decouples from Entries in MVP |
| `Format` modeling | Enum with `SingleElimination` only | Future-friendly without over-engineering |
| Phase code validation against CC | Deferred | `PhaseCodes` as domain constants in MVP; validate against CC later |
| Phase code convention | ODF standard | `R128`, `R64-`, `R32-`, `8FNL`, `QFNL`, `SFNL`, `FNL-` (not `R16`/`QUAR`/`SEMI`) |

## Section 1 — Architecture and bounded context

CompetitionConfig owns **structural definition** of Events: identity (RSC), metadata (discipline, gender, event code, name), and bracket shape (Phases + empty Units).

**Responsibilities:**
- Create Event instances
- Generate structure (Phases + Units) for a given Format + Size
- Enforce structural invariants (an Event's Phases are consistent with its Format)
- Emit `EventStructureGeneratedEvent` after persistence

**Not its responsibilities:**
- Who competes in a Unit — DataEntry (`UnitResult.Competitors[]` with status `START_LIST`)
- When a Unit is scheduled — Scheduling (`Session` by `SessionCode` reference)
- Unit results — DataEntry (`UnitResult` in later states)
- Progression between phases — Progression

**Outbound dependencies:**
- `ICommonCodeCache` (SharedKernel) — validate `discipline`, `eventCode` against CC
- `Rsc`, `Gender` value objects (SharedKernel)
- `IPublisher` (MediatR) — dispatch integration events

**Inbound dependencies (MVP 1):** none. Future MVPs consume the integration event.

**Folder structure:**

```
OVR.Modules.CompetitionConfig/
├── CompetitionConfigModule.cs       # DI + endpoint mapping
├── Domain/
│   ├── Event.cs                     # aggregate
│   ├── Phase.cs                     # entity inside Event
│   ├── Unit.cs                      # aggregate
│   ├── PhaseCodes.cs                # ODF standard constants
│   ├── CompetitionFormat.cs         # enum
│   └── BracketGenerator.cs          # domain service
├── Features/
│   ├── CreateEvent/
│   └── GenerateEventStructure/
├── Persistence/
│   ├── EventDocument.cs
│   ├── EventMapping.cs
│   ├── IEventRepository.cs
│   ├── MongoEventRepository.cs
│   ├── UnitDocument.cs
│   ├── UnitMapping.cs
│   ├── IUnitRepository.cs
│   └── MongoUnitRepository.cs
├── Errors/
│   └── CompetitionConfigErrors.cs
└── I18n/
    ├── eng.json
    ├── spa.json
    └── por.json
```

**Cleanup colateral:** delete `Domain/Discipline.cs` — dead scaffolding that duplicates `CC@DISCIPLINE`.

## Section 2 — Domain components

### `Event` aggregate

Identity: Event RSC (34-char string with padding; Event-level).

```csharp
public sealed class Event : AggregateRoot<string>
{
    public Rsc Rsc { get; private set; }
    public string Discipline { get; private set; }     // "BOX" — always 3 chars, uppercase
    public Gender Gender { get; private set; }
    public string EventCode { get; private set; }      // "57KG" — stored as-input (1..8 chars), NOT padded
    public string? Modifier { get; private set; }      // null in boxing MVP; stored as-input (1..10 chars) if present
    public string Name { get; private set; }
    public CompetitionFormat? Format { get; private set; }
    public int? Size { get; private set; }
    public IReadOnlyList<Phase> Phases => _phases.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime? StructureGeneratedAt { get; private set; }

    private readonly List<Phase> _phases = new();

    private Event() { }

    public static Event Create(
        Rsc rsc,
        string discipline,
        Gender gender,
        string eventCode,
        string? modifier,
        string name);

    public ErrorOr<IReadOnlyList<Rsc>> GenerateStructure(
        CompetitionFormat format,
        int size,
        int startUnitNumber,
        BracketGenerator generator);
}
```

**Invariants:**
- `GenerateStructure` can only be called once (returns `StructureAlreadyGenerated` otherwise).
- `size ∈ [2, 128]`.
- Only `CompetitionFormat.SingleElimination` accepted in MVP.
- `_phases` is always ordered by `Order` ascending.

### `Phase` entity (inside `Event`)

```csharp
public sealed class Phase : Entity<string>   // Id = PhaseCode within Event
{
    public string Code { get; private set; }       // "8FNL", "QFNL", "SFNL", "FNL-"
    public int Order { get; private set; }         // 0, 1, 2...
    public int UnitCount { get; private set; }     // 8, 4, 2, 1 for size=16

    private Phase() { }

    internal static Phase Create(string code, int order, int unitCount);
}
```

Pure structure, no behavior. Created only from within the Event aggregate.

### `Unit` aggregate

Identity: Unit RSC (34 chars, Unit-level).

```csharp
public sealed class Unit : AggregateRoot<string>
{
    public Rsc Rsc { get; private set; }             // level = Unit
    public Rsc EventRsc { get; private set; }        // derived for query
    public string PhaseCode { get; private set; }    // "8FNL"
    public int UnitNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Unit() { }

    public static Unit Create(Rsc rsc);
    // Derives EventRsc, PhaseCode, UnitNumber from parsed RSC.
    // Throws if rsc.Level != RscLevel.Unit.
}
```

In MVP 1, Unit is structural-only. No schedule, no lineup, no result.

### `CompetitionFormat` enum

```csharp
public enum CompetitionFormat
{
    SingleElimination = 1
}
```

### `PhaseCodes` static class

Standard ODF phase codes as domain constants. Not validated against CC in MVP.

```csharp
public static class PhaseCodes
{
    // Knockouts (used in MVP)
    public const string R128 = "R128";
    public const string R64 = "R64-";
    public const string R32 = "R32-";
    public const string EighthFinals = "8FNL";    // Round of 16
    public const string QuarterFinals = "QFNL";
    public const string SemiFinals = "SFNL";
    public const string Final = "FNL-";

    // Others (future use, documented for reference)
    public const string Preliminaries = "PREL";
    public const string Qualification = "QUAL";
    public const string Heat = "HEAT";
    public const string LuckyLoser = "LL--";
    public const string Repechage = "REP-";
}
```

### `BracketGenerator` domain service

Pure algorithm. No I/O. Produces a data structure; does not mutate aggregates.

```csharp
public sealed class BracketGenerator
{
    public BracketPlan Generate(
        CompetitionFormat format,
        int size,
        int startUnitNumber);
}

public sealed record BracketPlan(
    IReadOnlyList<PhaseSpec> Phases,
    IReadOnlyList<string> UnitLocalSegments);
    // UnitLocalSegments: 12 final chars of the Unit RSC (phase 4 + unitBlock 8)
    // Example: ["8FNL0001----", "8FNL0002----", ..., "FNL-0001----"]

public sealed record PhaseSpec(string Code, int Order, int UnitCount);
```

**Algorithm:**

1. `M = SmallestPowerOf2AtLeast(size)`. Throws if `size < 2` or `size > 128`.
2. Lookup phases by M:

   | M | Phases (in order) |
   |---|---|
   | 2 | `FNL-` |
   | 4 | `SFNL`, `FNL-` |
   | 8 | `QFNL`, `SFNL`, `FNL-` |
   | 16 | `8FNL`, `QFNL`, `SFNL`, `FNL-` |
   | 32 | `R32-`, `8FNL`, `QFNL`, `SFNL`, `FNL-` |
   | 64 | `R64-`, `R32-`, `8FNL`, `QFNL`, `SFNL`, `FNL-` |
   | 128 | `R128`, `R64-`, `R32-`, `8FNL`, `QFNL`, `SFNL`, `FNL-` |

3. For each phase at index `i`:
   - `unitCount = M / (2^(i+1))`
   - For `n in 1..unitCount`:
     - `unitBlock = ZeroPad4(startUnitNumber + accumulated) + "--"`
     - `segment = phaseCode + unitBlock` (always 12 chars total)

4. Total units generated = `M - 1`.

### Repositories

```csharp
public interface IEventRepository
{
    Task<Event?> GetByRscAsync(string eventRsc, CancellationToken ct);
    Task AddAsync(Event @event, CancellationToken ct);
    Task UpdateAsync(Event @event, CancellationToken ct);
}

public interface IUnitRepository
{
    Task<Unit?> GetByRscAsync(string unitRsc, CancellationToken ct);
    Task<IReadOnlyList<Unit>> ListByEventAsync(string eventRsc, CancellationToken ct);
    Task AddManyAsync(IEnumerable<Unit> units, CancellationToken ct);
}
```

### Integration event

Add to `src/OVR.SharedKernel/Domain/Events/Integration/EventStructureGeneratedEvent.cs`:

```csharp
public sealed record EventStructureGeneratedEvent(
    string EventRsc,
    string Format,
    int Size,
    IReadOnlyList<PhaseInfo> Phases,
    IReadOnlyList<string> UnitRscs,
    DateTime GeneratedAt) : DomainEventBase;

public sealed record PhaseInfo(string Code, int Order, int UnitCount);
```

## Section 3 — Data flows

### Create Event

```
POST /api/competition-config/events
Body: { discipline, gender, eventCode, modifier?, name }
   ↓
CreateEventEndpoint → MediatR.Send(CreateEventCommand)
   ↓
[LoggingBehavior → ValidationBehavior]
CreateEventValidator: field shapes, lengths, casing
   ↓
CreateEventHandler
   ├─ cache.Exists("DISCIPLINE", discipline) → else InvalidDiscipline
   ├─ cache.Exists("EVENT", eventCode) → else InvalidEventCode
   ├─ Build Rsc (34 chars total):
   │    discipline(3)
   │    + gender(1)
   │    + eventCode.PadRight(8,'-')
   │    + (modifier?.PadRight(10,'-') ?? new string('-',10))
   │    + new string('-', 12)      // 4 phase + 8 unitBlock, all dashes for Event-level RSC
   │    → pass to Rsc.Create(...) which validates format
   ├─ eventRepository.GetByRscAsync(rsc.Value) → if exists, EventAlreadyExists
   ├─ Event.Create(...)
   └─ eventRepository.AddAsync(event)
   ↓
201 Created { rsc: "BOXM57KG--------------------------" }
Location: /api/competition-config/events/{rsc}
```

Creating an Event **does not** emit an integration event. Only `GenerateStructure` emits one.

### Generate structure

```
POST /api/competition-config/events/{rsc}/generate-structure
Body: { format: "SingleElimination", size: 13, startUnitNumber?: 1 }
   ↓
GenerateEventStructureEndpoint → MediatR.Send(GenerateEventStructureCommand)
   ↓
[LoggingBehavior → ValidationBehavior]
Validator: size ∈ [2,128], format enum valid, startUnitNumber >= 1
   ↓
GenerateEventStructureHandler
   ├─ eventRepository.GetByRscAsync(eventRsc) → else EventNotFound
   ├─ event.GenerateStructure(format, size, startUnitNumber, bracketGenerator)
   │    (inside aggregate):
   │    ├─ If Format already set → StructureAlreadyGenerated
   │    ├─ bracketGenerator.Generate(format, size, startUnitNumber) → BracketPlan
   │    ├─ _phases = plan.Phases.Select(s => Phase.Create(s.Code, s.Order, s.UnitCount))
   │    ├─ Format = format; Size = size; StructureGeneratedAt = UtcNow
   │    ├─ Build absolute Unit RSCs:
   │    │     eventPrefix = this.Rsc.Value[..22]  // discipline+gender+event = 22 chars
   │    │     unitRscs = plan.UnitLocalSegments.Select(seg => Rsc.Create(eventPrefix + seg))
   │    ├─ RaiseDomainEvent(EventStructureGeneratedEvent(...))
   │    └─ return unitRscs
   ├─ units = unitRscs.Select(Unit.Create)
   ├─ unitRepository.AddManyAsync(units)
   ├─ eventRepository.UpdateAsync(event)
   ├─ foreach domainEvent: publisher.Publish(domainEvent)
   └─ event.ClearDomainEvents()
   ↓
200 OK { eventRsc, format, size, phases, unitRscs }
```

### Transactional ordering (MVP pragmatism)

The handler performs 3 writes (units bulk insert, event update, event publish). MongoDB 3.4 driver supports multi-document transactions, but introducing `IClientSessionHandle` adds ceremony we defer past MVP.

**MVP ordering:**
1. `unitRepository.AddManyAsync(units)` — uses `InsertMany` with `IsOrdered = false` so a partial success is observable
2. `eventRepository.UpdateAsync(event)` — sets Format, Size, Phases, StructureGeneratedAt
3. `publisher.Publish(EventStructureGeneratedEvent)`

**Failure scenarios and recovery:**

- **Step 1 fails completely** (connection error before any insert): no Units persisted, Event unchanged → client retry succeeds cleanly.
- **Step 1 fails partially** (some Units inserted, some not, e.g. connection drop mid-bulk): orphan Units exist, Event has no Format set. Client retry hits `AddManyAsync` which fails on duplicate `_id`. **Recovery requires operator action**: delete the Event (which cascades to orphan Units via a separate cleanup endpoint, not in MVP scope) and retry. Documented as a known limitation.
- **Step 2 fails after step 1 succeeds**: Units persisted, Event has no Format set. Same orphan situation as above.
- **Step 3 fails after steps 1-2 succeed**: full state persisted but no integration event. Downstream modules will not learn about the structure. **Manual recovery**: operator triggers a "republish structure event" endpoint (not in MVP scope).

**MVP posture**: we accept these failure modes rather than introduce transactions now. The probability of partial-failure during a bracket generation request is low in practice, and the blast radius is contained (one Event).

Deferred: multi-document transaction for this flow, plus Event deletion/cascade endpoint. Both go in the deferred items list below.

### Downstream consumers (out of MVP 1 scope, illustrative)

```
EventStructureGeneratedEvent
  ├─ DataEntry (MVP 3): create UnitResult per unit, status=START_LIST, fill lineup from Entries
  ├─ Scheduling (MVP 2): mark units as schedulable
  └─ Progression (MVP 4): compute bracket advancement graph
```

## Section 4 — Error handling and validation

### 3 validation levels

**Level 1 — Input (FluentValidation):**

`CreateEventValidator`:
- `Discipline`: not empty, length == 3, uppercase
- `Gender`: not empty, one of {M, W, X}
- `EventCode`: not empty, length 1..8, uppercase alphanumeric
- `Modifier`: if not null, length 1..10, uppercase alphanumeric
- `Name`: not empty, length 1..80

`GenerateEventStructureValidator`:
- `EventRsc`: not empty, length == 34
- `Format`: valid enum
- `Size`: integer, `[2, 128]`
- `StartUnitNumber`: integer, `[1, 9999]` (fits in zero-pad-4)

Validation failures → `400 Bad Request` automatically via `ValidationBehavior`.

**Level 2 — Application (handler, `ErrorOr`):**

All typed errors defined in `Errors/CompetitionConfigErrors.cs`:

| Error | ErrorType | HTTP | Message key |
|-------|-----------|------|-------------|
| `InvalidDiscipline` | Validation | 400 | `CompetitionConfig.InvalidDiscipline` |
| `InvalidEventCode` | Validation | 400 | `CompetitionConfig.InvalidEventCode` |
| `EventAlreadyExists` | Conflict | 409 | `CompetitionConfig.EventAlreadyExists` |
| `EventNotFound` | NotFound | 404 | `CompetitionConfig.EventNotFound` |
| `StructureAlreadyGenerated` | Conflict | 409 | `CompetitionConfig.StructureAlreadyGenerated` |
| `UnsupportedFormat` | Validation | 400 | `CompetitionConfig.UnsupportedFormat` |
| `InvalidSize` | Validation | 400 | `CompetitionConfig.InvalidSize` |

**Level 3 — Domain (aggregate invariants):**

`Event.GenerateStructure` re-validates:
- Format already set → `StructureAlreadyGenerated`
- Format not `SingleElimination` → `UnsupportedFormat`
- Size out of range → `InvalidSize`

Level 3 does not trust Levels 1-2; it protects the aggregate regardless of caller.

### i18n

Create `src/OVR.Modules.CompetitionConfig/I18n/{eng,spa,por}.json` with translations for all 7 error keys.

Example `eng.json`:
```json
{
  "CompetitionConfig.InvalidDiscipline": "Discipline '{{discipline}}' is not recognized.",
  "CompetitionConfig.InvalidEventCode": "Event code '{{eventCode}}' is not recognized.",
  "CompetitionConfig.EventAlreadyExists": "An event with RSC '{{rsc}}' already exists.",
  "CompetitionConfig.EventNotFound": "Event '{{rsc}}' was not found.",
  "CompetitionConfig.StructureAlreadyGenerated": "Structure for event '{{rsc}}' was already generated.",
  "CompetitionConfig.UnsupportedFormat": "Competition format '{{format}}' is not supported.",
  "CompetitionConfig.InvalidSize": "Bracket size {{size}} is out of range (2..128)."
}
```

Register in `OVR.Modules.CompetitionConfig.csproj`:
```xml
<Content Include="I18n\**"
         Link="I18n.CompetitionConfig\%(RecursiveDir)%(Filename)%(Extension)"
         CopyToOutputDirectory="PreserveNewest" />
```

FluentValidation messages use global translations in `src/OVR.Api/I18n/*.json` — no module-specific work needed.

### Explicit non-goals

- No transactional rollback for partial persistence failures. Mitigated by `_id` idempotency + retry.
- No cross-event coherence checks. Uniqueness guaranteed by RSC as `_id`.
- No PhaseCode validation against CC. Domain constants.
- No optimistic concurrency (`_v` field). Two concurrent `GenerateStructure` requests — one wins, other returns `StructureAlreadyGenerated`.

## Section 5 — Testing

### Unit tests

Project: `tests/OVR.Modules.CompetitionConfig.Tests/` (new, modeled after `tests/OVR.Modules.Entries.Tests/`).

Stack: xUnit + FluentAssertions + NSubstitute (matches repo convention).

**`BracketGeneratorTests`** — must achieve 100% branch coverage:

- `Generate_WithSize2_ReturnsSinglePhaseWithOneUnit`
- `Generate_WithSize4_Returns_SFNL_FNL_WithCorrectUnitCounts`
- `Generate_WithSize8_Returns_QFNL_SFNL_FNL`
- `Generate_WithSize16_Returns_8FNL_QFNL_SFNL_FNL`
- `Generate_WithSize32_Returns_R32_through_FNL`
- `Generate_WithSize13_RoundsUpToM16_WithSamePhases`
- `Generate_WithSize33_RoundsUpToM64`
- `Generate_StartingAtUnitNumber5_FirstUnitSegmentStartsWith_0005`
- `Generate_WithSize1_Throws_OutOfRange`
- `Generate_WithSize129_Throws_OutOfRange`
- `Generate_WithUnsupportedFormat_Throws`

Each test asserts: total units = M-1, phase order, unitCount per phase, 12-char segment format.

**`EventAggregateTests`**:
- `Create_WithValidInputs_BuildsRscCorrectly`
- `GenerateStructure_SetsFormatSizeAndPhases`
- `GenerateStructure_RaisesEventStructureGeneratedEvent_WithCorrectPayload`
- `GenerateStructure_CalledTwice_ReturnsStructureAlreadyGenerated`
- `GenerateStructure_WithUnsupportedFormat_ReturnsError`

**`UnitAggregateTests`**:
- `Create_FromUnitLevelRsc_DerivesEventRscPhaseCodeAndUnitNumber`
- `Create_FromNonUnitLevelRsc_Throws`

**`PhaseTests`**: construction and property tests.

### Integration tests

Project: extend `tests/OVR.Api.IntegrationTests/` with `CompetitionConfig/` subfolder.

Stack: Testcontainers.MongoDb + TestServer.

**`CreateEventEndpointTests`**:
- `POST_WithValidPayload_Returns201WithRscInLocation`
- `POST_WithUnknownDiscipline_Returns400_WithInvalidDisciplineError`
- `POST_WithDuplicateRsc_Returns409_EventAlreadyExists`
- `POST_WithMissingGender_Returns400_FromValidator`

**`GenerateEventStructureEndpointTests`**:
- `POST_ForSize16_Returns200AndPersists15Units`
- `POST_ForSize13_Returns200AndPersists15Units_RoundedToM16`
- `POST_OnAlreadyGeneratedEvent_Returns409`
- `POST_OnMissingEvent_Returns404`
- `POST_WithSize1_Returns400_FromValidator`
- `POST_generate_structure_PublishesEventStructureGeneratedEvent`

### CC mocking

Unit tests mock `ICommonCodeCache` with NSubstitute. Integration tests seed the `common_codes` collection with a minimal set (BOX, 57KG, MSINGLES) via a fixture before running.

### Coverage targets for "MVP done"

- 100% branches of `BracketGenerator.Generate`
- Happy paths end-to-end for both endpoints
- At least one test per error type (400/404/409)
- One test for integration event publishing

Out of scope: performance tests, concurrency tests, Mongo load tests.

## Persistence layout

Two collections in MongoDB:

**`events` collection** — Event documents with Phases embedded:

```json
{
  "_id": "BOXM57KG--------------------------",
  "discipline": "BOX",
  "gender": "M",
  "eventCode": "57KG",
  "modifier": null,
  "name": "Men's 57kg",
  "format": "SingleElimination",
  "size": 16,
  "phases": [
    { "code": "8FNL", "order": 0, "unitCount": 8 },
    { "code": "QFNL", "order": 1, "unitCount": 4 },
    { "code": "SFNL", "order": 2, "unitCount": 2 },
    { "code": "FNL-", "order": 3, "unitCount": 1 }
  ],
  "createdAt": "...",
  "structureGeneratedAt": "..."
}
```

**`units` collection** — one document per Unit:

```json
{
  "_id": "BOXM57KG--------------8FNL0001----",
  "eventRsc": "BOXM57KG--------------------------",
  "phaseCode": "8FNL",
  "unitNumber": 1,
  "createdAt": "..."
}
```

Index suggestion: `{ eventRsc: 1, phaseCode: 1, unitNumber: 1 }` on `units` for `ListByEventAsync` queries.

## Deferred items (for later MVPs)

- `IEntryReader` contract in Entries — needed in MVP 3 (DataEntry lineup filling) and for optional size/entries cross-validation
- "Entries closed" state in Entries — potential auto-generation trigger (option C deferred)
- Cross-validation that `size` matches active entries count
- Phase code validation against `CC @PHASE_TYPE` (once confirmed the Excel includes it)
- `WellKnownCodeTypes.PhaseType` constant — add during this validation rollout
- Multi-format support (`PoolRoundRobin`, `DoubleElimination`) — additive via enum + switch pattern
- Multi-document transaction for `GenerateEventStructure` persistence (using `IClientSessionHandle`)
- Event deletion / cascade cleanup endpoint (needed for recovery from partial-failure of `GenerateEventStructure`)
- "Republish structure event" endpoint (recovery for when persistence succeeds but publish fails)
- Regenerate bracket flow (delete + recreate)

## Out of scope (explicit)

- UI (Blazor WASM comes after API stabilizes)
- Authentication/authorization
- Event versioning / history
- Bracket visualization data (that's DataDistribution via `DT_BRACKETS`)
- Seeding algorithm (lives in DataEntry when filling lineup)
- Bye handling (emergent property: a Unit with only 1 competitor in DataEntry is a bye)
