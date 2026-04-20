# MVP 4 — Progression Module Design

**Date:** 2026-04-18
**Status:** Draft — ready for review
**Branch target:** `feat/progression-mvp`
**Predecessor specs:** `2026-04-17-competitionconfig-mvp-design.md`, `2026-04-17-scheduling-mvp-design.md`, `2026-04-18-dataentry-mvp-design.md`

## 1. Goal

Deliver the fourth MVP slice of OVR: **when a unit result goes OFFICIAL, the winner automatically appears in the correct slot of the next unit in the bracket**. This closes the loop between DataEntry and the bracket tree and is the last backend piece before ODF emission (MVP 5).

Pilot scope: boxing single-elimination, pure bracket, no pools, no repechage. The design leaves the door open for future topologies (repechage, pools, ranking-based progression) without committing to them now.

## 2. ODF grounding

The ODF standard does not define a dedicated `DT_PROGRESSION` message. Progression is expressed by embedding structural information inside three existing messages:

- **`DT_BRACKETS`** for head-to-head elimination formats. Each `<BracketItem>` contains `<CompetitorPlace Pos="1|2">` whose anchor is a `<PreviousUnit Unit="..." WLT="W|L" />` node that tells receivers which slot of which prior unit feeds this place.
- **`DT_POOL_STANDING`** for round-robin pools, resolved by rank.
- **`DT_PHASE_RESULT`** for ranking-based phases (swimming, athletics, gymnastics), using the `QualificationMark` attribute (`Q`, `q`).

Boxing uses only `DT_BRACKETS`. Progression is driven by the attribute `WLT="W"` — the winner of the previous unit advances to the corresponding `<CompetitorPlace>`. Byes appear as `CompetitorPlace Code="BYE"`. Double losses (DKO, BDSQ) assign `WLT="L"` to both competitors, leaving the next unit's slot at `NOCOMP`.

Common to all formats is the abstraction: a progression edge is a tuple `(SourceUnit, Outcome) → (TargetUnit, SlotPosition)`. For MVP 4 we model this abstraction explicitly and restrict its outcomes to the winner/loser dimension (`WLT`).

## 3. Architectural overview

### 3.1 Modules touched

| Module | Change |
|---|---|
| `CompetitionConfig` | `BracketGenerator` computes progression edges alongside unit structure. `EventStructureGeneratedEvent` extended with `Edges`. |
| `Progression` | New aggregate `BracketProgression`, two new event handlers, MongoDB persistence. Obsolete `ResultConfirmedHandler` removed. |
| `DataEntry` | New operation `UnitResult.AdvanceCompetitor(slot, participantId)`, new handler for `CompetitorAdvancedEvent`, auto-OFFICIAL bye handling in the start-list creation path. |
| `SharedKernel` | Three new integration events: `CompetitorAdvancedEvent`, `ProgressionSkippedEvent`, `EventProgressionCompletedEvent`. `ProgressionEdge` value object. `Outcome` enum. |

### 3.2 Core decisions

- **Where progression state lives**: `BracketProgression` is a new aggregate owned by the `Progression` module, one document per event. It holds the edge graph plus buffering state. CompetitionConfig does not learn about "what happens next"; it owns only structure.
- **How the graph is built**: `BracketGenerator` already knows canonical pairings. It returns the edges together with the unit list, and those edges travel inside `EventStructureGeneratedEvent`. This keeps the pairing convention in a single owner and avoids duplicating the algorithm.
- **How the winner lands**: purely event-driven. Progression emits `CompetitorAdvancedEvent`; DataEntry reacts. No command coupling between modules.
- **When advancements are emitted**: Progression buffers every computed advancement until the target unit's `UnitResult` exists (signalled by `UnitResultStartListCreatedEvent`). DataEntry therefore never sees a `CompetitorAdvancedEvent` for a unit whose aggregate is not yet materialised. This removes the need for a partial/pending `UnitResult` state in DataEntry.
- **Outcome dimension**: `Outcome { W, L }` from day one. For MVP 4 the generator emits only `W` edges, but the model is correct-shaped for repechage the day we add it.

### 3.3 End-to-end flow (happy path)

```
[CompetitionConfig] GenerateEventStructure
  ├─ BracketGenerator produces units + edges
  └─ emits EventStructureGeneratedEvent { eventRsc, phases, unitRscs, edges }

[Progression] EventStructureGeneratedHandler
  └─ BracketProgression.Create(eventRsc, edges); persists.

[Scheduling]  (operator schedules unit) → emits UnitScheduledEvent

[DataEntry]   UnitScheduledHandler
  ├─ resolves seeds via IEntryReader → builds Competitors
  ├─ creates UnitResult in StartList
  └─ emits UnitResultStartListCreatedEvent

[Progression] UnitResultStartListCreatedHandler
  ├─ agg.MarkTargetReady(unitRsc)
  └─ Flush any buffered advancements for that target
      → for each: emit CompetitorAdvancedEvent

[Operator]    data entry during the bout; confirm result

[DataEntry]   emits UnitResultOfficialEvent { unitRsc, winnerParticipantId?, resultCode, ... }

[Progression] UnitResultOfficialHandler
  ├─ agg.RecordAdvancement(sourceRsc, W, winnerParticipantId)
  │    ├─ winnerParticipantId == null → emit ProgressionSkippedEvent
  │    ├─ no outgoing edge (terminal unit) → emit EventProgressionCompletedEvent
  │    ├─ target ready → emit CompetitorAdvancedEvent directly
  │    └─ target not ready → buffer in PendingAdvancements
  └─ persists

[DataEntry]   CompetitorAdvancedHandler
  └─ UnitResult.AdvanceCompetitor(slot, participantId) → persists
```

## 4. Domain model

### 4.1 `BracketProgression` (aggregate, Progression module)

```csharp
public sealed class BracketProgression
{
    public string EventRsc { get; private set; }
    public IReadOnlyList<ProgressionEdge> Edges { get; private set; }
    public IReadOnlySet<string> ReadyTargets => _readyTargets;
    public IReadOnlyList<PendingAdvancement> PendingAdvancements => _pending;
    public DateTime CreatedAt { get; private set; }

    private readonly HashSet<string> _readyTargets;
    private readonly List<PendingAdvancement> _pending;
    private readonly Dictionary<(string Source, Outcome), ProgressionEdge> _edgeIndex;

    // Factory — runs invariants.
    public static ErrorOr<BracketProgression> Create(string eventRsc, IEnumerable<ProgressionEdge> edges);

    // Hydration — bypasses invariants for persistence reconstitution.
    internal static BracketProgression Hydrate(
        string eventRsc,
        IReadOnlyList<ProgressionEdge> edges,
        IEnumerable<string> readyTargets,
        IEnumerable<PendingAdvancement> pending,
        DateTime createdAt);

    public AdvancementOutcome RecordAdvancement(
        string sourceUnitRsc,
        Outcome outcome,
        string? participantId);

    public IReadOnlyList<PendingAdvancement> MarkTargetReady(string targetUnitRsc);
}

public sealed record ProgressionEdge(
    string SourceUnitRsc,
    Outcome Outcome,
    string TargetUnitRsc,
    int TargetSlot);

public enum Outcome { W, L }

public sealed record PendingAdvancement(
    string TargetUnitRsc,
    int TargetSlot,
    string ParticipantId,
    string SourceUnitRsc,
    DateTime RecordedAt);

public abstract record AdvancementOutcome
{
    public sealed record Ready(ProgressionEdge Edge, string ParticipantId) : AdvancementOutcome;
    public sealed record Buffered(ProgressionEdge Edge, string ParticipantId) : AdvancementOutcome;
    public sealed record Terminal(string SourceUnitRsc, string? ChampionParticipantId) : AdvancementOutcome;
    public sealed record Skipped(string SourceUnitRsc, string Reason) : AdvancementOutcome;
}
```

**Invariants enforced in `Create`:**

- `EventRsc` non-empty.
- Every edge's `TargetSlot ∈ {1, 2}`.
- No two edges share the same `(SourceUnitRsc, Outcome)` key — the outgoing target for a given outcome is unique.
- No two edges share the same `(TargetUnitRsc, TargetSlot)` — each slot of the target unit has at most one feeder.
- Edges refer to units within the same event (the aggregate cannot verify this without external state; the invariant is trust in the caller).

**Buffering rule (documented inline in `RecordAdvancement`):**

> Advancements toward a target whose `StartList` has not been created are buffered in-aggregate. This keeps DataEntry's `UnitResult` lifecycle strictly linear (`StartList → Live → Official`) and avoids introducing a "pending competitor" state there. If future sports require direct advancement before scheduling, revisit this invariant — the buffer is the single point of change.

### 4.2 `UnitResult.AdvanceCompetitor` (DataEntry, extension)

```csharp
public ErrorOr<Success> AdvanceCompetitor(int slot, string participantId);
```

Behaviour:

- Pre-condition: aggregate is in `StartList` state. Any other state returns `Errors.UnitNotInStartList`.
- `slot ∈ {1, 2}`. Otherwise `Errors.InvalidSlot`.
- Locate the `Competitor` at that slot.
  - If slot is empty: set the `ParticipantId`, raise `CompetitorPlacedFromProgressionEvent` (domain event, internal).
  - If slot holds the **same** participant: no-op. Idempotent re-delivery.
  - If slot holds a **different** participant: `Errors.SlotConflict`. Handler logs and skips — rollback is out of scope.

Documented inline:

> Pre-condition: UnitResult is in StartList state. Progression guarantees this by withholding advancements until `UnitResultStartListCreatedEvent` has fired. Revisit if direct-advancement-without-scheduling becomes a requirement.

### 4.3 Bye handling (DataEntry, extension to start-list creation)

Current MVP 3 path: `UnitScheduledHandler` resolves seeds via `IEntryReader` and builds `UnitResult` in `StartList`. Extension for MVP 4:

- After seed resolution, if exactly one real competitor is present (the other slot resolves to a bye), the handler:
  1. Builds the `UnitResult` directly in `OFFICIAL` state with the single competitor marked as winner.
  2. Emits both `UnitResultStartListCreatedEvent` (for downstream consumers expecting start-list parity) and `UnitResultOfficialEvent` immediately in sequence.
- Normal progression flow then applies, without Progression caring about the concept of "bye".

## 5. Integration events (SharedKernel)

```csharp
// Extended — new `Edges` property. Existing consumers (Scheduling) ignore it.
public sealed record EventStructureGeneratedEvent(
    string EventRsc,
    IReadOnlyList<PhaseSummary> Phases,
    IReadOnlyList<string> UnitRscs,
    IReadOnlyList<ProgressionEdge> Edges
) : DomainEventBase;

// Progression → DataEntry
public sealed record CompetitorAdvancedEvent(
    string EventRsc,
    string TargetUnitRsc,
    int TargetSlot,
    string ParticipantId,
    string SourceUnitRsc,
    DateTime AdvancedAt
) : DomainEventBase;

// Progression → observability / MVP 5 DataDistribution
public sealed record ProgressionSkippedEvent(
    string EventRsc,
    string SourceUnitRsc,
    string Reason,           // "NoWinner" for MVP 4
    DateTime SkippedAt
) : DomainEventBase;

// Progression → MVP 5 DataDistribution
public sealed record EventProgressionCompletedEvent(
    string EventRsc,
    string FinalUnitRsc,
    string ChampionParticipantId,
    DateTime CompletedAt
) : DomainEventBase;
```

`ProgressionEdge` and `Outcome` live in `SharedKernel/Domain/Progression/` so both CompetitionConfig and Progression can reference them.

## 6. `BracketGenerator` extension

For single-elim of size `S = 2^k`, the canonical edge formula is:

```
for each phase in [R1, R2, ..., SFNL]:   # exclude final
    for each unit N in phase:
        targetUnit  = ceil(N / 2) in nextPhase
        targetSlot  = (N is odd) ? 1 : 2
        edge = (sourceRsc, Outcome.W, targetRsc, targetSlot)
```

For draw sizes that are not powers of 2, bye-bearing units still emit their outgoing edge (the single real competitor will become the winner). The algorithm does not emit edges out of the final phase.

**Dependency note:** MVP 1's `BracketGenerator` is documented to support sizes 2–128. Before MVP 4 implementation starts, verify whether non-power-of-2 draws are already emitted by MVP 1 or need extension here. If MVP 1 only emits powers of 2, the bye auto-OFFICIAL path in DataEntry (§4.3) is reachable only after that extension lands — plan writes this as an explicit sub-task rather than silently leaving bye handling unreachable.

The generator returns `(units, edges)` from a single call; the handler of `GenerateEventStructure` passes both into the integration event.

## 7. Handlers

### 7.1 Progression — `EventStructureGeneratedHandler`

```
Handle(event):
    if BracketProgression exists for event.EventRsc → return (idempotent)
    create BracketProgression.Create(event.EventRsc, event.Edges)
    persist (InsertOneAsync + catch E11000 → no-op)
```

Idempotency pattern follows MVP 3 lesson: `ExistsAsync + InsertOneAsync` with E11000 catch, return `bool` from `SaveNewAsync` so duplicate handler invocations skip event publication.

### 7.2 Progression — `UnitResultOfficialHandler`

```
Handle(event):
    eventRsc = DeriveEventRscFromUnitRsc(event.UnitRsc)  // RSC parsing
    agg = repository.GetByEventAsync(eventRsc)
    if agg == null → log error "BracketProgression not found" and return
    outcome = agg.RecordAdvancement(event.UnitRsc, Outcome.W, event.WinnerParticipantId)
    persist agg
    switch outcome:
        Ready(edge, pid)      → publish CompetitorAdvancedEvent
        Buffered              → (state persisted; nothing to emit)
        Terminal(src, pid)    → publish EventProgressionCompletedEvent
        Skipped(src, reason)  → publish ProgressionSkippedEvent
```

### 7.3 Progression — `UnitResultStartListCreatedHandler` (new)

```
Handle(event):
    eventRsc = DeriveEventRscFromUnitRsc(event.UnitRsc)
    agg = repository.GetByEventAsync(eventRsc)
    if agg == null → log and return
    flushed = agg.MarkTargetReady(event.UnitRsc)
    persist agg
    for each pending in flushed:
        publish CompetitorAdvancedEvent(eventRsc, pending.TargetUnitRsc,
                                         pending.TargetSlot, pending.ParticipantId,
                                         pending.SourceUnitRsc, now())
```

### 7.4 DataEntry — `CompetitorAdvancedHandler` (new)

```
Handle(event):
    ur = repository.GetByUnitRscAsync(event.TargetUnitRsc)
    if ur == null → log error and return (invariant violation; should not happen)
    result = ur.AdvanceCompetitor(event.TargetSlot, event.ParticipantId)
    if result.IsError:
        if SlotConflict          → log warning, return
        if UnitNotInStartList    → log warning, return  (late re-delivery)
        else                     → propagate
    else:
        persist ur
        drain domain events
```

### 7.5 Removal

Delete `src/OVR.Modules.Progression/EventHandlers/ResultConfirmedHandler.cs`. The obsolete `ResultConfirmedEvent` in SharedKernel remains marked `[Obsolete]`; we clean it up after the demo, not in this MVP.

## 8. Persistence

- Collection `progression_brackets`, key `EventRsc`.
- Unique index on `EventRsc`.
- Document shape mirrors the aggregate: `eventRsc`, `edges[]`, `readyTargets[]`, `pendingAdvancements[]`, `createdAt`.
- Mapping via `BracketProgressionMapping.ToDomain/ToDocument` using the `Hydrate` factory for reconstitution.
- Enum parsing follows MVP 3 pattern: `UnitResultMapping.ParseEnum<TEnum>` wraps failures with document ID + field name.
- Index initialization via an `IHostedService` (`ProgressionIndexInitializer`) registered in `AddProgressionModule`.

## 9. HTTP surface

None beyond the placeholder endpoint already present in `ProgressionModule`. No read/write endpoints in MVP 4 — Progression is fully event-driven. Observability endpoint is deferred (TD-PG-03).

## 10. i18n

Three new module-scoped error codes, translated in `src/OVR.Modules.Progression/I18n/{eng,spa,por}.json`:

- `Progression.BracketNotFound` — inbound event refers to an unknown event.
- `Progression.DuplicateEdge` — generator invariant violation.
- `Progression.UnknownOutcome` — parsing failure during hydration.

No global validation keys needed (the module doesn't receive user input).

## 11. Edge-case matrix

| Case | Handler behaviour | Event emitted |
|---|---|---|
| Normal result, target ready | Emit advancement immediately | `CompetitorAdvancedEvent` |
| Normal result, target not ready | Buffer in aggregate | (none) |
| Normal result, target was buffered, schedules later | `MarkTargetReady` flushes | `CompetitorAdvancedEvent` (possibly many) |
| `WinnerParticipantId == null` (DKO/BDSQ/NC) | No advancement | `ProgressionSkippedEvent` (reason `NoWinner`) |
| Source unit has no outgoing edge (FNL-), with winner | No advancement | `EventProgressionCompletedEvent` |
| Source unit has no outgoing edge (FNL-), winner `null` | No advancement | `ProgressionSkippedEvent` — event ends without declared champion; operator recourse is out of scope |
| Bye unit (R1 with one competitor) | DataEntry auto-OFFICIALs | Standard flow; Progression unaware of bye |
| Re-emission of `UnitResultOfficialEvent` | `AdvanceCompetitor` is idempotent on same participant | Possibly duplicate `CompetitorAdvancedEvent`; DataEntry no-ops |
| Target UnitResult advanced then confirmed Live/Official before a late advancement | `AdvanceCompetitor` returns `UnitNotInStartList`; handler logs and returns | (none) — out of scope for MVP 4 |

## 12. Testing plan

### 12.1 Unit tests (domain)

**`BracketProgression`:**

- `Create_WithValidEdges_BuildsAggregate`
- `Create_WithDuplicateSourceOutcome_ReturnsError`
- `Create_WithDuplicateTargetSlot_ReturnsError`
- `Create_WithInvalidSlot_ReturnsError`
- `RecordAdvancement_WithEdgeAndReadyTarget_ReturnsReady`
- `RecordAdvancement_WithEdgeAndTargetNotReady_ReturnsBufferedAndPersistsPending`
- `RecordAdvancement_WithNoOutgoingEdge_ReturnsTerminal`
- `RecordAdvancement_WithNullParticipant_ReturnsSkipped`
- `RecordAdvancement_SameSourceTwice_DoesNotDuplicatePending`
- `MarkTargetReady_WithBufferedPending_FlushesAndClears`
- `MarkTargetReady_WithNoPending_ReturnsEmpty`
- `Hydrate_FromPersistedState_ReconstitutesAllFields`

**`BracketGenerator` (extended):**

- `Generate_ForSizeOf8_Produces6EdgesWithCanonicalSlots`
- `Generate_ForSizeOf16_Produces14Edges`
- `Generate_OddUnitNumbers_MapToSlot1`
- `Generate_EvenUnitNumbers_MapToSlot2`
- `Generate_DoesNotEmitEdgesOutOfFinal`

**`UnitResult.AdvanceCompetitor`:**

- `AdvanceCompetitor_WhenSlotEmpty_FillsSlotAndRaisesEvent`
- `AdvanceCompetitor_WhenSlotHasSameParticipant_IsNoOp`
- `AdvanceCompetitor_WhenSlotHasDifferentParticipant_ReturnsSlotConflict`
- `AdvanceCompetitor_WhenStateNotStartList_ReturnsUnitNotInStartList`
- `AdvanceCompetitor_WithInvalidSlot_ReturnsInvalidSlot`

**Bye handling in `UnitScheduledHandler`:**

- `UnitScheduledHandler_ResolvedOneCompetitor_AutoConfirmsOfficial`
- `UnitScheduledHandler_ResolvedOneCompetitor_EmitsStartListAndOfficialEvents`
- `UnitScheduledHandler_ResolvedTwoCompetitors_BehavesAsMvp3`

### 12.2 Integration tests (WebApplicationFactory + Testcontainers)

- `HappyPathBracketOf4_ProgressesThroughFinal`: schedule and confirm each unit in a 4-athlete draw; assert champion declared via `EventProgressionCompletedEvent`.
- `AdvancementBeforeTargetScheduled_BuffersAndFlushesOnScheduling`: confirm SFNL0001 before FNL is scheduled; schedule FNL; assert slot 1 filled.
- `Dko_EmitsProgressionSkippedEvent_AndTargetSlotRemainsEmpty`.
- `ByeInRound1_AutoAdvancesWithoutUserInteraction`: generate 6-athlete draw; schedule bye unit; assert winner present in next round.
- `RepeatedUnitResultOfficial_DoesNotDuplicateAdvancement`: manually publish the event twice; assert target slot holds one participant and `AdvanceCompetitor` invoked idempotently.

### 12.3 Out of scope (tests not added)

- Rollback / post-OFFICIAL corrections — TD-PG-01.
- Repechage topologies — TD-PG-02.
- Pool-to-bracket transitions — not applicable to boxing.

## 13. Technical debt

- **TD-PG-01 — Rollback of progression**: no mechanism to undo a `CompetitorAdvancedEvent` once applied. A referee correction of a confirmed OFFICIAL result leaves downstream units desynchronised. Fix requires a `UnitResult.WithdrawCompetitor` operation plus cascading through the bracket tree.
- **TD-PG-02 — Non-single-elim topologies**: pools, repechage, ranking-based progression. `Outcome { W, L }` supports repechage structurally; pools need a richer outcome (rank-in-group). Defer until a second discipline is piloted.
- **TD-PG-03 — Buffer observability**: no endpoint or metric for pending advancements or buffer age. Useful for diagnosing desynchronisation between Scheduling and Progression. Add `GET /api/progression/{eventRsc}` plus Prometheus gauges when the need arises.
- **TD-PG-04 — Reason granularity on `ProgressionSkippedEvent`**: current reason is `"NoWinner"`. ODF distinguishes `DKO`, `BDSQ`, `NC`, etc. MVP 5 (DataDistribution) will need the detail to emit `DT_BRACKETS` correctly.
- **TD-PG-05 — Obsolete `ResultConfirmedEvent`**: still in SharedKernel with `[Obsolete]`. Remove after the Eugenio demo; we leave it to avoid cascade churn in MVP 4.
- **TD-PG-06 — `"BYE"` organisation sentinel in bye handling**: `UnitResult.CreateByeOfficial` constructs the absent opponent's `Competitor` with `Organisation.Create("BYE")`. Organisations are CC-validated values and `"BYE"` does not exist in `ICommonCodeCache`. Works today because no validation runs on this construction path. Fix options: model the absent slot as a nullable `Organisation?`, introduce a `Competitor.Bye()` factory, or register `"BYE"` as a legitimate organisation code. Revisit when adding CC validation to aggregate constructors.

## 14. Dependencies and sequencing

No external dependency changes. New tests use the existing Testcontainers/WebApplicationFactory scaffolding. xunit.runner.json setting `parallelizeTestCollections: false` remains required.

Sequencing within the MVP:

1. SharedKernel additions (`ProgressionEdge`, `Outcome`, three new events) — unblocks all downstream work.
2. CompetitionConfig: `BracketGenerator` extension + `EventStructureGeneratedEvent` payload + handler update.
3. Progression: `BracketProgression` aggregate + mapping + repository + three handlers.
4. DataEntry: `UnitResult.AdvanceCompetitor` + handler + bye auto-OFFICIAL.
5. Integration tests end-to-end.
6. i18n files and final build/test sweep.

## 15. Out of scope (explicit)

- ODF message emission (`DT_BRACKETS`, `DT_RESULT`) — MVP 5.
- Rollback / correction of confirmed results — TD-PG-01.
- Repechage, pools, ranking-based progression — TD-PG-02.
- UI changes — the Blazor surface is not in scope until all backend MVPs are done.
- Authentication / authorisation of progression endpoints — there are no endpoints yet.
