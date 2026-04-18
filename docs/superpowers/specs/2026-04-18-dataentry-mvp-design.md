# DataEntry MVP — Design Spec

**Date:** 2026-04-18
**Module:** `OVR.Modules.DataEntry`
**Scope:** MVP 3 of the thin end-to-end roadmap (Boxing, single-elimination pilot)
**Depends on:** MVP 1 (CompetitionConfig), MVP 2 (Scheduling), existing Entries module

## Goal

Deliver the piece of the end-to-end loop that sits between Scheduling and Progression: when a Unit is scheduled, automatically build its start list from active Entries; let an operator walk the result through its ODF-aligned lifecycle (`START_LIST → LIVE → OFFICIAL`); emit an event that Progression can consume to advance the winner.

Success looks like: *"open session 1 / mat 1 → see units with competitors → capture a result → mark OFFICIAL → downstream modules see the winner."*

## Non-goals (explicitly deferred)

- Clocks (game clock, round timer, stop/resume as first-class concepts).
- Scoreboards and realtime push to external displays.
- Warnings and knockdown capture (model has placeholders; no endpoints).
- Protests, amendments, revert from OFFICIAL.
- Reacting to schedule changes or unschedule after UnitResult exists.
- Progression (winners advancing to next bracket slot) — MVP 4.
- ODF XML emission — MVP 5.
- Judge identities (real officials assigned via OfficialAssignment). MVP 3 uses positional IDs `J1/J2/J3`.
- Pool formats, repechage, generic events.

## ODF alignment

DataEntry's state machine and data model mirror the ODF Boxing Data Dictionary. Key references consulted via NotebookLM:

- `DT_RESULT` for boxing uses three `ResultStatus` values: `START_LIST`, `LIVE`, `OFFICIAL`. No `INTERMEDIATE`. `PROVISIONAL` exists in the base catalog but is not used in the boxing lifecycle.
- Match segments are modeled as `Periods/Period` with `Code="R1"|"R2"|"R3"`.
- Scorecards use `ExtendedPeriod` with `Type="EP"`, `Code="SCR_H"|"SCR_A"`, `Pos="J1".."J5"`, `Value` numeric (10-point must).
- Per-competitor totals use `ExtendedResult` with `Code="JUDGE"`.
- Bout-level result info uses `ExtendedInfo` with `Type="UI"` and codes `RES_CODE`, `PERIOD`, `ROUND`, `TIME`.
- `Result` carries `ResultType ∈ {Points, RM_Points, RM}`, `WLT ∈ {W, L}`, `SortOrder ∈ {1 (Red), 2 (Blue)}`.
- Start list carries only `SortOrder`, `COLOUR`, `SEED`, and `DETAILED` (for `NOCOMP` placeholders). No weight/stance/reach in `DT_RESULT`.

## Module layout

```
OVR.Modules.DataEntry/
├── DataEntryModule.cs
├── Domain/
│   ├── UnitResult.cs                  // aggregate root
│   ├── Competitor.cs                  // value object
│   ├── Period.cs                      // value object
│   ├── PeriodScorecard.cs             // value object
│   ├── Decision.cs                    // value object
│   ├── ResultStatus.cs                // StartList | Live | Official
│   ├── ResultType.cs                  // Points | RmPoints | Rm
│   ├── ResultCode.cs                  // WP | KO | TkoI | TkoR | Dsq | Bdsq | Dko | Wo | Abd | Nc
│   ├── JudgePosition.cs               // J1 | J2 | J3
│   └── Wlt.cs                         // W | L
├── SportRules/
│   ├── BoxingRules.cs                 // PeriodCount=3, JudgeCount=3, MinScore=6, MaxScore=10
│   └── TenPointMustResolver.cs        // compute Decision from periods
├── Lineup/
│   ├── IFirstRoundLineupResolver.cs
│   └── SeedBasedFirstRoundLineupResolver.cs
├── Features/
│   ├── CreateUnitResultOnScheduled/   // INotificationHandler<UnitScheduledEvent>
│   ├── StartUnit/
│   ├── ScorePeriod/
│   ├── FinishByStoppage/
│   ├── ConfirmUnitResult/
│   ├── GetUnitResult/
│   └── ListUnitResults/
├── Persistence/
│   ├── UnitResultDocument.cs
│   ├── UnitResultMapping.cs
│   ├── IUnitResultRepository.cs
│   ├── MongoUnitResultRepository.cs
│   └── DataEntryIndexInitializer.cs   // skeleton; no extra indexes in MVP 3
├── Errors/
│   └── DataEntryErrors.cs
├── I18n/
│   ├── eng.json
│   ├── spa.json
│   └── por.json
└── OVR.Modules.DataEntry.csproj
```

## Changes to other modules

### OVR.Modules.CompetitionConfig

- Add `SeedA` and `SeedB` as nullable `int?` on the `Unit` aggregate, populated by `BracketGenerator` at generation time for first-round units.
- Add `Contracts/IUnitLineupReader.cs`:
  ```csharp
  public interface IUnitLineupReader
  {
      Task<(int? seedA, int? seedB)> GetSeedsForUnit(string unitRsc, CancellationToken ct);
  }
  ```
- Implementation reads from `MongoUnitRepository`.
- No API surface change. Update existing unit tests and generator tests.

### OVR.Modules.Entries

- Add `Contracts/IEntryReader.cs`:
  ```csharp
  public interface IEntryReader
  {
      Task<IReadOnlyList<EntryDto>> ListActiveByEventRsc(string eventRsc, CancellationToken ct);
  }
  public sealed record EntryDto(
      ParticipantId ParticipantId,
      int? Seed,
      Organisation Organisation);
  ```
- Implementation sits on top of `MongoEntryRepository`, filters `Status == Active`.
- No change to `Entry` aggregate or API surface.

### OVR.Modules.Scheduling

- Add `Contracts/IUnitScheduleReader.cs`:
  ```csharp
  public interface IUnitScheduleReader
  {
      Task<IReadOnlyList<string>> ListUnitRscs(
          string? sessionCode, string? locationCode, CancellationToken ct);
  }
  ```
- Used by DataEntry's `ListUnitResults` endpoint to resolve the filter before joining with `UnitResult` documents.

### OVR.SharedKernel

Add four integration events under `Domain/Events/Integration/`:

```csharp
public sealed record UnitResultStartListCreatedEvent(
    string UnitRsc,
    string EventRsc,
    IReadOnlyList<CompetitorSnapshot> Competitors,
    DateTime CreatedAt) : DomainEventBase;

public sealed record UnitResultLiveEvent(
    string UnitRsc,
    DateTime StartedAt) : DomainEventBase;

public sealed record UnitResultPeriodScoredEvent(
    string UnitRsc,
    string PeriodCode,
    IReadOnlyList<ScorecardSnapshot> Scorecards,
    DateTime ScoredAt) : DomainEventBase;

public sealed record UnitResultOfficialEvent(
    string UnitRsc,
    string? WinnerParticipantId,
    string ResultCode,
    string ResultType,
    string? DecisionMark,
    string? StoppageRound,
    string? StoppageTime,
    DateTime ConfirmedAt) : DomainEventBase;

public sealed record CompetitorSnapshot(
    int SortOrder, string? ParticipantId, int? Seed, string Organisation);

public sealed record ScorecardSnapshot(
    string JudgePos, int HomeScore, int AwayScore);
```

Mark the existing `ResultConfirmedEvent` with `[Obsolete("Use UnitResultOfficialEvent instead.")]`. Do not remove in MVP 3.

## Domain model

### `UnitResult` (aggregate root)

```csharp
public sealed class UnitResult : AggregateRoot<string>
{
    public Rsc UnitRsc { get; private set; }
    public ResultStatus Status { get; private set; }
    public IReadOnlyList<Competitor> Competitors { get; }     // exactly 2
    public IReadOnlyList<Period> Periods { get; }             // grows R1..R3
    public Decision? Decision { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public string? CurrentPeriodCode { get; private set; }    // mirrors ODF UI PERIOD
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public static ErrorOr<UnitResult> CreateForFirstRound(
        Rsc unitRsc, Competitor red, Competitor blue);

    public static UnitResult Hydrate(...);  // mapper-only reconstitution

    public ErrorOr<Success> Start();
    public ErrorOr<Success> ScorePeriod(
        string periodCode, IReadOnlyList<PeriodScorecard> cards);
    public ErrorOr<Success> FinishByStoppage(
        ResultCode resultCode, string stoppageRound, string stoppageTime,
        ParticipantId? winnerParticipantId);
    public ErrorOr<Success> Confirm();
}
```

### Value objects

```csharp
public sealed record Competitor(
    int SortOrder,                          // 1=Red, 2=Blue
    ParticipantId? ParticipantId,           // null iff Nocomp
    string? NocompDetail,                   // ODF EUE DETAILED; null in MVP 3
    int? Seed,
    Organisation Organisation,
    Wlt? Wlt);                              // set at Confirm()

public sealed record Period(
    string Code,                            // "R1"|"R2"|"R3"
    IReadOnlyList<PeriodScorecard> Scorecards);

public sealed record PeriodScorecard(
    JudgePosition JudgePos,                 // J1|J2|J3
    int HomeScore,                          // SCR_H
    int AwayScore);                         // SCR_A

public sealed record Decision(
    ResultType Type,                        // Points|RmPoints|Rm
    ResultCode Code,
    string? DecisionMark,                   // "3:0"|"2:1"|"2:0" — Points/RmPoints only
    string? StoppageRound,                  // "R2" — Rm/RmPoints only
    string? StoppageTime,                   // "mm:ss"
    ParticipantId? WinnerParticipantId);
```

### Invariants (enforced inside the aggregate)

| # | Invariant | Enforced in |
|---|-----------|-------------|
| I1 | Exactly 2 competitors with `SortOrder ∈ {1,2}`, distinct. | `CreateForFirstRound` |
| I2 | Period scorecards: exactly 3 entries, one per `J1/J2/J3`, no duplicates. | `ScorePeriod` |
| I3 | `HomeScore` and `AwayScore` are integers in `[6..10]`. | `ScorePeriod` |
| I4 | Periods are scored in order `R1 → R2 → R3`. Scoring a later period before the prior fails. | `ScorePeriod` |
| I5 | Cannot score a period when `Decision != null`. | `ScorePeriod` |
| I6 | `Confirm()` requires `Decision != null`. | `Confirm` |
| I7 | `FinishByStoppage` requires `Status == Live` and `Decision == null`. | `FinishByStoppage` |
| I8 | Valid transitions: `StartList → Live` (via `Start`); `Live → Official` (via `Confirm`). Any other transition returns a typed error. | `Start`, `Confirm` |
| I9 | If `ResultCode ∈ {Nc, Dko, Bdsq}`, `WinnerParticipantId` must be null; otherwise must equal one of the two competitor participant IDs. | `FinishByStoppage`, `TenPointMustResolver` |
| I10 | `WLT` on competitors is set only at `Confirm()`: winner `W`, other `L`; both `L` if no winner. | `Confirm` |
| I11 | `ScorePeriod` requires `Status == Live`. | `ScorePeriod` |
| I12 | `FinishByStoppage` rejects `ResultCode.Wp` (WP is reserved for point decisions produced by `TenPointMustResolver`). | `FinishByStoppage` |

### Status lifecycle (three ODF states)

```
StartList ── Start() ──> Live
                             │
                             ├── ScorePeriod(R1) → ScorePeriod(R2) → ScorePeriod(R3)
                             │        └── after R3: TenPointMustResolver → Decision populated
                             │
                             ├── FinishByStoppage(...) → Decision populated
                             │
                             └── Confirm() ──> Official  (emits UnitResultOfficialEvent)
```

`CurrentPeriodCode` is updated on `Start()` (`"R1"`) and after each `ScorePeriod` (`"R2"`, `"R3"`, then stays at `"R3"` after R3 is scored). It mirrors ODF `UI PERIOD`.

## `TenPointMustResolver` logic

Invoked by `UnitResult` after `ScorePeriod("R3")` closes the last period. Inputs: the three periods. Outputs: a `Decision`.

1. Per judge, sum `HomeScore` and `AwayScore` across all 3 periods.
2. Per judge, determine that judge's pick: `Home` if home total > away total; `Away` if lower; `Draw` if equal.
3. Count votes across judges: `redVotes`, `blueVotes`, `drawVotes` (sum to 3).
4. Classify outcome:

   | redVotes | blueVotes | drawVotes | Result |
   |----------|-----------|-----------|--------|
   | 3 | 0 | 0 | `ResultType=Points`, `Code=WP`, `DecisionMark="3:0"`, winner=Red |
   | 0 | 3 | 0 | `ResultType=Points`, `Code=WP`, `DecisionMark="3:0"`, winner=Blue |
   | 2 | 1 | 0 | `ResultType=Points`, `Code=WP`, `DecisionMark="2:1"`, winner=Red (split) |
   | 1 | 2 | 0 | Same, winner=Blue (split) |
   | 2 | 0 | 1 | `ResultType=Points`, `Code=WP`, `DecisionMark="2:0"`, winner=Red (majority) |
   | 0 | 2 | 1 | Same, winner=Blue (majority) |
   | 1 | 1 | 1 | `ResultType=Rm`, `Code=Nc`, winner=null |
   | 0 | 0 | 3 | Same |
   | any other | | | `ResultType=Rm`, `Code=Nc`, winner=null (defensive fallback) |

5. `DecisionMark` is not set when `ResultType=Rm`.
6. `StoppageRound` and `StoppageTime` are always null on a points decision.

## Lineup construction

### `IFirstRoundLineupResolver`

```csharp
public interface IFirstRoundLineupResolver
{
    (Competitor red, Competitor blue) Resolve(
        int seedA, int seedB, IReadOnlyList<EntryDto> activeEntries);
}
```

### `SeedBasedFirstRoundLineupResolver` implementation

1. Find the entries whose `Seed` matches `seedA` and `seedB` respectively.
2. Assign the lower seed number (e.g., seed 1 vs seed 8 → seed 1 wins Red) to `SortOrder=1` (Red).
3. The other to `SortOrder=2` (Blue).
4. Build `Competitor` for each with `ParticipantId`, `Seed`, `Organisation` from the entry. `NocompDetail=null`, `Wlt=null`.

Failure modes:
- Seed not found among active entries → resolver returns `Error.NotFound("DataEntry.LineupResolutionFailed")`. Handler logs warning and aborts creation (no `UnitResult` created).
- Duplicate seeds in active entries → resolver returns error (defensive; should be prevented upstream by Entries invariants).

## Handler: `CreateUnitResultOnScheduled`

Implements `INotificationHandler<UnitScheduledEvent>`.

```
1. Idempotency: if repository already has UnitResult with this UnitRsc → log info "skipping, already exists" and return. (TD R3)
2. Call IUnitLineupReader.GetSeedsForUnit(unitRsc).
   - If either seed is null → log warn "unit has no seeds, skipping lineup fill" and return.
3. Call IEntryReader.ListActiveByEventRsc(eventRsc).
4. Call IFirstRoundLineupResolver.Resolve(seedA, seedB, activeEntries).
   - On failure → log warn + return without creating.
5. Call UnitResult.CreateForFirstRound(unitRsc, red, blue).
6. Repository.Save(unitResult).
7. Publish domain events (draining via IPublisher), then ClearDomainEvents().
```

The initial `UnitResult.CreateForFirstRound` raises `UnitResultStartListCreatedEvent` as a domain event. The handler drains and publishes it after persistence.

**Idempotency guard:** relies on `_id == UnitRsc` being unique in Mongo. `repository.ExistsAsync(unitRsc)` is called before `Save`; on race (two events arrive concurrently), `Save` is implemented with `InsertOneAsync` and `MongoWriteException` on duplicate key is caught and treated as "already exists" (no-op). Same pattern as MVP 2's location-startTime index.

## Ignored events (documented TD)

DataEntry **does not** subscribe to:
- `UnitScheduleChangedEvent`
- `UnitUnscheduledEvent`

Rationale: in MVP 3 the `UnitResult` does not persist session/location data, so most changes are irrelevant. Unschedule after `UnitResult` exists raises policy questions (destroy lineup? keep?) that need coordinated design with Progression. Tracked as TD-DE-02.

## API surface

All endpoints under `/api/data-entry`. All pass `HttpContext` to `.ToApiResult(httpContext)` / `.ToCreatedResult(...)` for i18n.

| Method | Route | Body | Success | Errors |
|--------|-------|------|---------|--------|
| GET | `/unit-results/{rsc}` | — | 200 with full snapshot | 404 `UnitResultNotFound` |
| GET | `/unit-results?sessionCode=&locationCode=&status=` | — | 200 with summary array | — |
| POST | `/unit-results/{rsc}/start` | — | 204 | 404, 422 `InvalidStatusTransition` |
| POST | `/unit-results/{rsc}/periods/{code}/score` | `Scorecards[]` | 204 | 404, 422 (I2/I3/I4/I5, `InvalidPeriodOrder`, etc.) |
| POST | `/unit-results/{rsc}/finish-stoppage` | `FinishStoppageRequest` | 204 | 404, 422 (I7, `InvalidStoppageData`) |
| POST | `/unit-results/{rsc}/confirm` | — | 204 | 404, 422 `DecisionRequired`, `InvalidStatusTransition` |

Request DTOs:

```csharp
public sealed record ScorePeriodRequest(IReadOnlyList<ScorecardDto> Scorecards);
public sealed record ScorecardDto(string JudgePos, int HomeScore, int AwayScore);

public sealed record FinishStoppageRequest(
    string ResultCode,
    string StoppageRound,
    string StoppageTime,
    string? WinnerParticipantId);
```

Response DTOs are denormalized read models (flat JSON with codes only per CLAUDE.md — no localized descriptions). Example GET shape:

```json
{
  "unitRsc": "8FNL0001----",
  "status": "Live",
  "currentPeriodCode": "R2",
  "startedAt": "2026-04-18T14:30:00Z",
  "competitors": [
    { "sortOrder": 1, "participantId": "NOC-ESP-0001",
      "seed": 1, "organisation": "ESP", "wlt": null },
    { "sortOrder": 2, "participantId": "NOC-POL-0014",
      "seed": 8, "organisation": "POL", "wlt": null }
  ],
  "periods": [
    { "code": "R1", "scorecards": [
      { "judgePos": "J1", "homeScore": 10, "awayScore": 9 },
      { "judgePos": "J2", "homeScore": 10, "awayScore": 9 },
      { "judgePos": "J3", "homeScore": 9, "awayScore": 10 }
    ]}
  ],
  "decision": null,
  "createdAt": "2026-04-18T14:00:00Z",
  "updatedAt": "2026-04-18T14:30:00Z"
}
```

### FluentValidation (level 1) for request DTOs

- `ScorePeriodRequest.Scorecards`: count exactly 3; each `JudgePos` in `{"J1","J2","J3"}`; each `HomeScore`/`AwayScore` in `[6,10]`.
- Route parameter `code` in `{"R1","R2","R3"}`.
- `FinishStoppageRequest.ResultCode` in the typed catalog, and must not be `Wp` (I12).
- `FinishStoppageRequest.StoppageRound` in `{"R1","R2","R3"}`.
- `FinishStoppageRequest.StoppageTime` matches regex `^\d{1,2}:\d{2}$`.

Aggregate invariants (level 3) complement these. Repository existence (level 2) is checked in handlers before invoking domain methods.

## Persistence

**Collection:** `unitResults`.

Document shape:

```json
{
  "_id": "8FNL0001----",
  "status": "StartList",
  "competitors": [
    { "sortOrder": 1, "participantId": "NOC-ESP-0001",
      "nocompDetail": null, "seed": 1, "organisation": "ESP", "wlt": null },
    { "sortOrder": 2, "participantId": "NOC-POL-0014",
      "nocompDetail": null, "seed": 8, "organisation": "POL", "wlt": null }
  ],
  "periods": [],
  "decision": null,
  "startedAt": null,
  "endedAt": null,
  "currentPeriodCode": null,
  "createdAt": "2026-04-18T10:30:00Z",
  "updatedAt": null
}
```

Indexes: only `_id`. `DataEntryIndexInitializer : IHostedService` is scaffolded but adds no indexes in MVP 3 (kept for consistency with Scheduling).

`MongoUnitResultRepository` uses `InsertOneAsync` for create (catches `E11000` → idempotent no-op) and `ReplaceOneAsync` for update.

## Errors and i18n

Module errors live in `Errors/DataEntryErrors.cs`:

| Error key | Type | HTTP |
|-----------|------|------|
| `DataEntry.UnitResultNotFound` | NotFound | 404 |
| `DataEntry.InvalidStatusTransition` | Validation | 422 |
| `DataEntry.InvalidPeriodOrder` | Validation | 422 |
| `DataEntry.InvalidScorecardCount` | Validation | 422 |
| `DataEntry.InvalidScoreRange` | Validation | 422 |
| `DataEntry.DuplicateJudgePosition` | Validation | 422 |
| `DataEntry.PeriodAlreadyScored` | Validation | 422 |
| `DataEntry.DecisionAlreadyExists` | Validation | 422 |
| `DataEntry.DecisionRequired` | Validation | 422 |
| `DataEntry.InvalidStoppageData` | Validation | 422 |
| `DataEntry.LineupResolutionFailed` | (internal; not exposed as 4xx) | — |

i18n: `src/OVR.Modules.DataEntry/I18n/{eng,spa,por}.json` with flat keys `DataEntry.*`. `.csproj` includes:

```xml
<Content Include="I18n\**" Link="I18n.DataEntry\%(RecursiveDir)%(Filename)%(Extension)"
         CopyToOutputDirectory="PreserveNewest" />
```

## Testing

### Unit tests (`OVR.Modules.DataEntry.Tests`)

- **Aggregate invariants:** I1–I12, positive and negative.
- **State transitions:** `Start` from each status; `Confirm` from each status with/without `Decision`; `ScorePeriod` / `FinishByStoppage` in each status.
- **Period ordering:** scoring R2 before R1 fails; re-scoring R1 after it's closed fails.
- **`TenPointMustResolver`:** all classified outcomes (3-0 both sides, 2-1 both sides, 2-0 majority both sides, 1-1-1 draw, 0-0-3 all-draw, defensive fallback).
- **`SeedBasedFirstRoundLineupResolver`:** red-corner assignment to lower seed; entry-not-found; duplicate seeds.

Estimated ~35 tests.

### Integration tests (`OVR.Api.IntegrationTests`)

- Handler reacts to `UnitScheduledEvent`: creates `UnitResult` in `StartList` with correct lineup.
- Idempotency: two `UnitScheduledEvent` for same `UnitRsc` → only one `UnitResult`.
- Full points path: start → score R1 → R2 → R3 → confirm → `Official`, `UnitResultOfficialEvent` published with correct winner.
- Stoppage path: start → `FinishByStoppage(Tko_I, R2, "01:30", winnerParticipantId)` → confirm → `Official`.
- `FinishByStoppage(Nc, ...)` with a winnerParticipantId → 422 `InvalidStoppageData`.
- `GET /unit-results/{rsc}` returns expected snapshot per state.
- `GET /unit-results?sessionCode=X` joins via `IUnitScheduleReader`.
- API error paths: 404, 422 each mapped to ProblemDetails with correct i18n per `Language` header.

Estimated ~18 tests.

Use the existing `WebApplicationFactory<Program>` pattern from MVP 2 (Testcontainers + CC seeding + `xunit.runner.json` with serialized collections).

## Technical debt

- **TD-DE-01** — `SeedBasedFirstRoundLineupResolver` duplicates the convention "lower seed → Red corner" that could live in CompetitionConfig. The pairing logic itself is not duplicated (thanks to B2's `IUnitLineupReader`), only the corner convention. Refactor path: expose corner assignment via `IUnitLineupReader.GetCornerAssignment(unitRsc)`. Trigger: a second discipline with a different corner rule.
- **TD-DE-02** — DataEntry ignores `UnitScheduleChangedEvent` and `UnitUnscheduledEvent`. Changes to scheduling after `UnitResult` exists are invisible to DataEntry. Any inconsistency is operator-managed in MVP 3. Post-demo task: design sync policy (probably: mark `UnitResult` as orphan on unschedule while `StartList`; block unschedule once `Live`/`Official`).
- **TD-DE-03** — Warnings and knockdowns are not captured. Domain model reserves the shape (via future `Period.Incidents`) but no endpoints or storage in MVP 3. DataDistribution (MVP 5) will emit `0` for `ER WARNING` and `ER KD` until added.
- **TD-DE-04** — Hardcoded 3 judges × 3 periods for boxing (`BoxingRules.PeriodCount=3, JudgeCount=3`). When `ISportRuleEngine` is activated with a second discipline, these move to rule-based configuration.
- **TD-DE-05** — No revert from `Official`. Protests and amendments require cross-module coordination with Progression. Out of scope.
- **TD-DE-06** — `ResultConfirmedEvent` in SharedKernel is obsoleted by `UnitResultOfficialEvent` and marked `[Obsolete]`. Remove once audit confirms no consumer remains.

## Migration / deployment notes

- No schema migration required (new collection, new fields on existing CompetitionConfig's `Unit`).
- `Unit.SeedA` / `Unit.SeedB` are additive and nullable; documents written before this MVP simply don't have them and the reader returns `(null, null)` (current behavior). When re-generating a bracket, the new fields are populated.
- No breaking API changes elsewhere.

## References

- `consolidated-architecture.md` — bounded-context alignment for Data Entry.
- `docs/superpowers/specs/2026-04-17-competitionconfig-mvp-design.md` — MVP 1 (Unit aggregate, BracketGenerator).
- `docs/superpowers/specs/2026-04-17-scheduling-mvp-design.md` — MVP 2 (UnitScheduledEvent shape, index strategy).
- ODF Boxing Data Dictionary (consulted via NotebookLM notebook `86c2df5c-5884-4b53-abf8-2cf74f2fb876`).
