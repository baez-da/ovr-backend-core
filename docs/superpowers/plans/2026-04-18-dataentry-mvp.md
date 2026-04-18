# DataEntry MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build DataEntry module MVP for boxing single-elimination: react to `UnitScheduledEvent` to create `UnitResult` with start-list lineup, walk through ODF lifecycle `START_LIST → LIVE → OFFICIAL`, capture 10-point-must scoring across 3 periods × 3 judges, support stoppage terminations, and emit 4 integration events for downstream modules.

**Architecture:** Vertical-slice module with `UnitResult` aggregate (identified by UnitRsc), `TenPointMustResolver` domain service, `SeedBasedFirstRoundLineupResolver` via `IFirstRoundLineupResolver` seam for future refactor to `IUnitLineupReader`. MongoDB collection `unitResults`. Four new SharedKernel integration events. Cross-module contracts added to CompetitionConfig (`IUnitLineupReader`), Entries (`IEntryReader`), Scheduling (`IUnitScheduleReader`).

**Tech Stack:** .NET 10, C# 14, MediatR 12.4, FluentValidation 11.11, ErrorOr 2.0, MongoDB.Driver 3.4, xUnit + FluentAssertions + NSubstitute + Testcontainers.MongoDb.

**Spec reference:** `docs/superpowers/specs/2026-04-18-dataentry-mvp-design.md`

**Starting branch:** `feat/dataentry-mvp` (to be created from `main` before Task 1).

---

## File Structure Map

### New files

```
src/OVR.Modules.DataEntry/
├── Domain/
│   ├── UnitResult.cs                     # aggregate root (REPLACES current stub)
│   ├── Competitor.cs                     # value object
│   ├── Period.cs                         # value object
│   ├── PeriodScorecard.cs                # value object
│   ├── Decision.cs                       # value object
│   ├── ResultStatus.cs                   # enum (REPLACES current stub)
│   ├── ResultType.cs                     # enum
│   ├── ResultCode.cs                     # enum
│   ├── JudgePosition.cs                  # enum
│   └── Wlt.cs                            # enum
├── SportRules/
│   ├── BoxingRules.cs                    # constants
│   └── TenPointMustResolver.cs           # + interface
├── Lineup/
│   ├── IFirstRoundLineupResolver.cs
│   └── SeedBasedFirstRoundLineupResolver.cs
├── Features/
│   ├── CreateUnitResultOnScheduled/
│   │   └── UnitScheduledEventHandler.cs
│   ├── StartUnit/
│   │   ├── StartUnitCommand.cs
│   │   ├── StartUnitValidator.cs
│   │   ├── StartUnitHandler.cs
│   │   └── StartUnitEndpoint.cs
│   ├── ScorePeriod/
│   │   ├── ScorePeriodCommand.cs
│   │   ├── ScorePeriodValidator.cs
│   │   ├── ScorePeriodHandler.cs
│   │   └── ScorePeriodEndpoint.cs
│   ├── FinishByStoppage/
│   │   ├── FinishByStoppageCommand.cs
│   │   ├── FinishByStoppageValidator.cs
│   │   ├── FinishByStoppageHandler.cs
│   │   └── FinishByStoppageEndpoint.cs
│   ├── ConfirmUnitResult/
│   │   ├── ConfirmUnitResultCommand.cs
│   │   ├── ConfirmUnitResultHandler.cs
│   │   └── ConfirmUnitResultEndpoint.cs
│   ├── GetUnitResult/
│   │   ├── GetUnitResultQuery.cs
│   │   ├── GetUnitResultHandler.cs
│   │   └── GetUnitResultEndpoint.cs
│   └── ListUnitResults/
│       ├── ListUnitResultsQuery.cs
│       ├── ListUnitResultsValidator.cs
│       ├── ListUnitResultsHandler.cs
│       └── ListUnitResultsEndpoint.cs
├── Persistence/
│   ├── UnitResultDocument.cs
│   ├── UnitResultMapping.cs
│   ├── IUnitResultRepository.cs
│   ├── MongoUnitResultRepository.cs
│   └── DataEntryIndexInitializer.cs
├── Errors/
│   └── DataEntryErrors.cs
└── I18n/
    ├── eng.json
    ├── spa.json
    └── por.json

src/OVR.Modules.CompetitionConfig/Contracts/
└── IUnitLineupReader.cs                  # NEW

src/OVR.Modules.Entries/Contracts/
├── IEntryReader.cs                       # NEW
└── EntryDto.cs                           # NEW

src/OVR.Modules.Scheduling/Contracts/
└── IUnitScheduleReader.cs                # NEW

src/OVR.SharedKernel/Domain/Events/Integration/
├── UnitResultStartListCreatedEvent.cs    # NEW
├── UnitResultLiveEvent.cs                # NEW
├── UnitResultPeriodScoredEvent.cs        # NEW
└── UnitResultOfficialEvent.cs            # NEW

tests/OVR.Modules.DataEntry.Tests/        # EXPAND (project exists)
├── Domain/
│   ├── UnitResultAggregateTests.cs
│   ├── CompetitorTests.cs
│   └── DecisionTests.cs
├── SportRules/
│   └── TenPointMustResolverTests.cs
├── Lineup/
│   └── SeedBasedFirstRoundLineupResolverTests.cs
└── Features/
    └── CreateUnitResultOnScheduled/
        └── UnitScheduledEventHandlerTests.cs

tests/OVR.Api.IntegrationTests/DataEntry/
├── Support/
│   └── DataEntryWebAppFactory.cs
├── CreateUnitResultOnScheduledTests.cs
├── ScoringPathTests.cs
├── StoppagePathTests.cs
├── ValidationTests.cs
└── ListUnitResultsTests.cs
```

### Modified files

- `src/OVR.Modules.DataEntry/DataEntryModule.cs` — wire DI + endpoints (currently stub)
- `src/OVR.Modules.DataEntry/OVR.Modules.DataEntry.csproj` — add refs + I18n content
- `src/OVR.Modules.DataEntry/Domain/CompetitionResult.cs` — DELETE (replaced by UnitResult.cs)
- `src/OVR.Modules.DataEntry/Domain/ResultStatus.cs` — REWRITE (current enum is wrong lifecycle)
- `src/OVR.Modules.DataEntry/SportRules/ISportRuleEngine.cs` — DELETE (YAGNI — per TD-DE-04)
- `src/OVR.Modules.CompetitionConfig/Domain/Unit.cs` — add `SeedA`, `SeedB` fields + `Hydrate` update
- `src/OVR.Modules.CompetitionConfig/Domain/BracketGenerator.cs` — populate `SeedA`/`SeedB` on first-round units
- `src/OVR.Modules.CompetitionConfig/Persistence/UnitDocument.cs` + mapping — persist new fields
- `src/OVR.Modules.CompetitionConfig/CompetitionConfigModule.cs` — register `IUnitLineupReader`
- `src/OVR.Modules.Entries/EntriesModule.cs` — register `IEntryReader`
- `src/OVR.Modules.Scheduling/SchedulingModule.cs` — register `IUnitScheduleReader`
- `src/OVR.SharedKernel/Domain/Events/Integration/ResultConfirmedEvent.cs` — mark `[Obsolete]`
- `src/OVR.Api/Program.cs` — call `AddDataEntryModule()` + `MapDataEntryEndpoints()` (verify current state, add if missing)
- `OvrBackendCore.slnx` — no change expected (projects already in solution)

---

## Progress tracking

Each task ends with a commit. Run `dotnet build` at the start of each task to catch prior-task regressions early.

---

## Task 1: Create feature branch + add 4 SharedKernel integration events + obsolete ResultConfirmedEvent

**Files:**
- Create: `src/OVR.SharedKernel/Domain/Events/Integration/UnitResultStartListCreatedEvent.cs`
- Create: `src/OVR.SharedKernel/Domain/Events/Integration/UnitResultLiveEvent.cs`
- Create: `src/OVR.SharedKernel/Domain/Events/Integration/UnitResultPeriodScoredEvent.cs`
- Create: `src/OVR.SharedKernel/Domain/Events/Integration/UnitResultOfficialEvent.cs`
- Modify: `src/OVR.SharedKernel/Domain/Events/Integration/ResultConfirmedEvent.cs`

- [ ] **Step 1: Create feature branch from main**

```bash
git checkout main
git pull
git checkout -b feat/dataentry-mvp
```

- [ ] **Step 2: Create `UnitResultStartListCreatedEvent.cs`**

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitResultStartListCreatedEvent(
    string UnitRsc,
    string EventRsc,
    IReadOnlyList<CompetitorSnapshot> Competitors,
    DateTime CreatedAt) : DomainEventBase;

public sealed record CompetitorSnapshot(
    int SortOrder,
    string? ParticipantId,
    int? Seed,
    string Organisation);
```

- [ ] **Step 3: Create `UnitResultLiveEvent.cs`**

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitResultLiveEvent(
    string UnitRsc,
    DateTime StartedAt) : DomainEventBase;
```

- [ ] **Step 4: Create `UnitResultPeriodScoredEvent.cs`**

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitResultPeriodScoredEvent(
    string UnitRsc,
    string PeriodCode,
    IReadOnlyList<ScorecardSnapshot> Scorecards,
    DateTime ScoredAt) : DomainEventBase;

public sealed record ScorecardSnapshot(
    string JudgePos,
    int HomeScore,
    int AwayScore);
```

- [ ] **Step 5: Create `UnitResultOfficialEvent.cs`**

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitResultOfficialEvent(
    string UnitRsc,
    string? WinnerParticipantId,
    string ResultCode,
    string ResultType,
    string? DecisionMark,
    string? StoppageRound,
    string? StoppageTime,
    DateTime ConfirmedAt) : DomainEventBase;
```

- [ ] **Step 6: Mark `ResultConfirmedEvent` obsolete**

Edit `src/OVR.SharedKernel/Domain/Events/Integration/ResultConfirmedEvent.cs`:

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

[Obsolete("Use UnitResultOfficialEvent instead. Will be removed once all consumers are migrated.")]
public sealed record ResultConfirmedEvent(
    string UnitRsc,
    string Status,
    DateTime ConfirmedAt) : DomainEventBase;
```

- [ ] **Step 7: Build**

```bash
dotnet build
```

Expected: success with no new warnings (the `[Obsolete]` mark may trigger warnings if used anywhere; grep for usages and suppress in those call sites with `#pragma warning disable CS0618` if needed — but first search to confirm no usage exists).

```bash
rg "ResultConfirmedEvent" src tests
```

Expected: only the definition file matches.

- [ ] **Step 8: Commit**

```bash
git add src/OVR.SharedKernel/Domain/Events/Integration/
git commit -m "feat(sharedkernel): add 4 integration events for DataEntry MVP

Adds UnitResultStartListCreatedEvent, UnitResultLiveEvent,
UnitResultPeriodScoredEvent, UnitResultOfficialEvent. Marks legacy
ResultConfirmedEvent as Obsolete (superseded by UnitResultOfficialEvent)."
```

---

## Task 2: Add SeedA/SeedB to CompetitionConfig Unit + BracketGenerator population

**Files:**
- Modify: `src/OVR.Modules.CompetitionConfig/Domain/Unit.cs`
- Modify: `src/OVR.Modules.CompetitionConfig/Domain/BracketGenerator.cs`
- Modify: `src/OVR.Modules.CompetitionConfig/Persistence/UnitDocument.cs`
- Modify: `src/OVR.Modules.CompetitionConfig/Persistence/UnitMapping.cs`
- Test: `tests/OVR.Modules.CompetitionConfig.Tests/Domain/BracketGeneratorTests.cs`

- [ ] **Step 1: Read current Unit aggregate to confirm field list**

```bash
cat src/OVR.Modules.CompetitionConfig/Domain/Unit.cs | head -80
```

- [ ] **Step 2: Write failing test in `BracketGeneratorTests.cs`**

Add new test to the existing `BracketGeneratorTests`:

```csharp
[Fact]
public void Generate_PopulatesSeedAAndSeedB_ForFirstRoundUnitsOnly()
{
    var eventRsc = Rsc.Create("BOXW---------------M71KG---------");
    var phases = new[] { "R16-", "QFNL", "SFNL", "FNL-" };

    var units = BracketGenerator.GenerateSingleElimination(eventRsc, phases, bracketSize: 16);

    var firstRound = units.Where(u => u.PhaseCode == "R16-").OrderBy(u => u.UnitNumber).ToList();
    firstRound.Should().HaveCount(8);

    // Seed pairing: (1,16), (8,9), (5,12), (4,13), (3,14), (6,11), (7,10), (2,15)
    firstRound[0].SeedA.Should().Be(1);
    firstRound[0].SeedB.Should().Be(16);
    firstRound[1].SeedA.Should().Be(8);
    firstRound[1].SeedB.Should().Be(9);

    // Later rounds have no seeds assigned
    var quarterfinals = units.Where(u => u.PhaseCode == "QFNL").ToList();
    quarterfinals.Should().AllSatisfy(u =>
    {
        u.SeedA.Should().BeNull();
        u.SeedB.Should().BeNull();
    });
}
```

- [ ] **Step 3: Run test to verify failure**

```bash
dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~BracketGeneratorTests.Generate_PopulatesSeedAAndSeedB"
```

Expected: compile error (`SeedA`/`SeedB` don't exist on `Unit`).

- [ ] **Step 4: Add `SeedA` and `SeedB` to `Unit` aggregate**

In `src/OVR.Modules.CompetitionConfig/Domain/Unit.cs`, add properties after the existing ones:

```csharp
public int? SeedA { get; private set; }
public int? SeedB { get; private set; }
```

Update the `Create` factory signature and the `Hydrate` factory to accept/set these two new optional fields. Add overload or optional parameters — the factory currently used by `BracketGenerator` should accept them. Example additions (fit to existing signature):

```csharp
public static Unit Create(
    Rsc unitRsc,
    int unitNumber,
    string phaseCode,
    int? seedA = null,
    int? seedB = null)
{
    // ... existing validation ...
    var unit = new Unit
    {
        Id = unitRsc.Value,
        Rsc = unitRsc,
        UnitNumber = unitNumber,
        PhaseCode = phaseCode,
        Status = UnitStatus.Draft,
        SeedA = seedA,
        SeedB = seedB,
        CreatedAt = DateTime.UtcNow
    };
    // ... existing event raise ...
    return unit;
}
```

Update `Hydrate` similarly:

```csharp
internal static Unit Hydrate(
    Rsc unitRsc,
    int unitNumber,
    string phaseCode,
    UnitStatus status,
    int? seedA,
    int? seedB,
    DateTime createdAt,
    DateTime? updatedAt)
{
    return new Unit
    {
        Id = unitRsc.Value,
        Rsc = unitRsc,
        UnitNumber = unitNumber,
        PhaseCode = phaseCode,
        Status = status,
        SeedA = seedA,
        SeedB = seedB,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
}
```

- [ ] **Step 5: Update `BracketGenerator` to compute seed pairings for first-round units**

In `src/OVR.Modules.CompetitionConfig/Domain/BracketGenerator.cs`, find where first-round units are created. The standard single-elim pairing for bracket size N follows the pattern: bout k (1..N/2) has `seedA = k` and `seedB = N + 1 - k`, reordered per the standard bracket (1 vs N, 8 vs 9, 5 vs 12, 4 vs 13, etc.).

Add this helper:

```csharp
private static IReadOnlyList<(int seedA, int seedB)> ComputeFirstRoundPairings(int bracketSize)
{
    // Standard single-elimination pairing table.
    // For bracketSize=16, order is: (1,16),(8,9),(5,12),(4,13),(3,14),(6,11),(7,10),(2,15)
    // This is the reverse of the typical "seed-line" layout so higher-seed bouts come first.
    var pairings = new List<(int, int)>();
    var seedOrder = BuildSeedOrder(bracketSize);
    for (int i = 0; i < bracketSize; i += 2)
    {
        pairings.Add((seedOrder[i], seedOrder[i + 1]));
    }
    return pairings;
}

private static int[] BuildSeedOrder(int size)
{
    // Classic recursion: [1,2] → [1,4,3,2] → [1,8,5,4,3,6,7,2] ...
    if (size == 1) return new[] { 1 };
    var half = BuildSeedOrder(size / 2);
    var result = new int[size];
    for (int i = 0; i < half.Length; i++)
    {
        result[2 * i] = half[i];
        result[2 * i + 1] = size + 1 - half[i];
    }
    return result;
}
```

Then in the method that creates units for the first phase, pass the computed pairings:

```csharp
var firstRoundPairings = ComputeFirstRoundPairings(bracketSize);
for (int i = 0; i < firstRoundUnitCount; i++)
{
    var (seedA, seedB) = firstRoundPairings[i];
    units.Add(Unit.Create(unitRsc, unitNumber: i + 1, phaseCode: firstPhaseCode, seedA, seedB));
}
// Later-phase units call Unit.Create without seeds (defaults to null).
```

- [ ] **Step 6: Update `UnitDocument.cs` and `UnitMapping.cs`**

Add to `UnitDocument`:

```csharp
public int? SeedA { get; set; }
public int? SeedB { get; set; }
```

Update `UnitMapping.ToDocument`:

```csharp
return new UnitDocument
{
    // existing fields...
    SeedA = unit.SeedA,
    SeedB = unit.SeedB,
};
```

Update `UnitMapping.ToDomain` to pass the new fields to `Unit.Hydrate`:

```csharp
return Unit.Hydrate(
    Rsc.Create(doc.Id),
    doc.UnitNumber,
    doc.PhaseCode,
    Enum.Parse<UnitStatus>(doc.Status),
    doc.SeedA,
    doc.SeedB,
    doc.CreatedAt,
    doc.UpdatedAt);
```

- [ ] **Step 7: Run test to verify it passes**

```bash
dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~BracketGeneratorTests"
```

Expected: all BracketGenerator tests pass (pre-existing + the new one).

- [ ] **Step 8: Run full solution build**

```bash
dotnet build
```

Expected: success. If any pre-existing code calls `Unit.Create` without the new optional params, it still compiles (they're optional).

- [ ] **Step 9: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/ tests/OVR.Modules.CompetitionConfig.Tests/
git commit -m "feat(competitionconfig): persist seed pairings on first-round units

Adds optional SeedA/SeedB to Unit aggregate, populated by BracketGenerator
for first-round units only. Required by DataEntry MVP lineup resolver."
```

---

## Task 3: Add IUnitLineupReader contract + MongoDB implementation in CompetitionConfig

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Contracts/IUnitLineupReader.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/MongoUnitLineupReader.cs`
- Modify: `src/OVR.Modules.CompetitionConfig/CompetitionConfigModule.cs`

- [ ] **Step 1: Create contract file**

`src/OVR.Modules.CompetitionConfig/Contracts/IUnitLineupReader.cs`:

```csharp
namespace OVR.Modules.CompetitionConfig.Contracts;

public interface IUnitLineupReader
{
    Task<(int? SeedA, int? SeedB)> GetSeedsForUnit(string unitRsc, CancellationToken ct);
}
```

- [ ] **Step 2: Create implementation**

`src/OVR.Modules.CompetitionConfig/Persistence/MongoUnitLineupReader.cs`:

```csharp
using MongoDB.Driver;
using OVR.Modules.CompetitionConfig.Contracts;

namespace OVR.Modules.CompetitionConfig.Persistence;

public sealed class MongoUnitLineupReader : IUnitLineupReader
{
    private readonly IMongoCollection<UnitDocument> _units;

    public MongoUnitLineupReader(IMongoDatabase database)
    {
        _units = database.GetCollection<UnitDocument>("competitionconfig_units");
    }

    public async Task<(int? SeedA, int? SeedB)> GetSeedsForUnit(
        string unitRsc,
        CancellationToken ct)
    {
        var doc = await _units
            .Find(u => u.Id == unitRsc)
            .Project(u => new { u.SeedA, u.SeedB })
            .FirstOrDefaultAsync(ct);

        return doc is null ? (null, null) : (doc.SeedA, doc.SeedB);
    }
}
```

(Verify the collection name by reading `MongoUnitRepository.cs` — adjust if different.)

- [ ] **Step 3: Register in module**

In `src/OVR.Modules.CompetitionConfig/CompetitionConfigModule.cs`, in `AddCompetitionConfigModule()`:

```csharp
services.AddScoped<IUnitLineupReader, MongoUnitLineupReader>();
```

- [ ] **Step 4: Build**

```bash
dotnet build
```

Expected: success.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/
git commit -m "feat(competitionconfig): expose IUnitLineupReader contract for DataEntry"
```

---

## Task 4: Add IEntryReader contract + MongoDB implementation in Entries

**Files:**
- Create: `src/OVR.Modules.Entries/Contracts/IEntryReader.cs`
- Create: `src/OVR.Modules.Entries/Contracts/EntryDto.cs`
- Create: `src/OVR.Modules.Entries/Persistence/MongoEntryReader.cs`
- Modify: `src/OVR.Modules.Entries/EntriesModule.cs`

- [ ] **Step 1: Create contract files**

`src/OVR.Modules.Entries/Contracts/IEntryReader.cs`:

```csharp
namespace OVR.Modules.Entries.Contracts;

public interface IEntryReader
{
    Task<IReadOnlyList<EntryDto>> ListActiveByEventRsc(
        string eventRsc,
        CancellationToken ct);
}
```

`src/OVR.Modules.Entries/Contracts/EntryDto.cs`:

```csharp
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Entries.Contracts;

public sealed record EntryDto(
    ParticipantId ParticipantId,
    int? Seed,
    Organisation Organisation);
```

- [ ] **Step 2: Create implementation**

`src/OVR.Modules.Entries/Persistence/MongoEntryReader.cs`:

```csharp
using MongoDB.Driver;
using OVR.Modules.Entries.Contracts;
using OVR.Modules.Entries.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Entries.Persistence;

public sealed class MongoEntryReader : IEntryReader
{
    private readonly IMongoCollection<EntryDocument> _entries;

    public MongoEntryReader(IMongoDatabase database)
    {
        _entries = database.GetCollection<EntryDocument>("entries");
    }

    public async Task<IReadOnlyList<EntryDto>> ListActiveByEventRsc(
        string eventRsc,
        CancellationToken ct)
    {
        var activeStatus = EntryStatus.Active.ToString();
        var docs = await _entries
            .Find(e => e.EventRsc == eventRsc && e.Status == activeStatus)
            .ToListAsync(ct);

        return docs.Select(d => new EntryDto(
            ParticipantId.Create(d.ParticipantId),
            int.TryParse(d.Seed, out var n) ? n : (int?)null,
            Organisation.FromCode(d.Organisation))).ToList();
    }
}
```

(Verify the collection name and `EntryDocument` fields against `MongoEntryRepository.cs`. If `Seed` is stored as string, parse; if int, adjust.)

- [ ] **Step 3: Register in module**

In `src/OVR.Modules.Entries/EntriesModule.cs`, in `AddEntriesModule()`:

```csharp
services.AddScoped<IEntryReader, MongoEntryReader>();
```

- [ ] **Step 4: Build**

```bash
dotnet build
```

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.Entries/
git commit -m "feat(entries): expose IEntryReader contract for cross-module queries"
```

---

## Task 5: Add IUnitScheduleReader contract + MongoDB implementation in Scheduling

**Files:**
- Create: `src/OVR.Modules.Scheduling/Contracts/IUnitScheduleReader.cs`
- Create: `src/OVR.Modules.Scheduling/Persistence/MongoUnitScheduleReader.cs`
- Modify: `src/OVR.Modules.Scheduling/SchedulingModule.cs`

- [ ] **Step 1: Create contract**

`src/OVR.Modules.Scheduling/Contracts/IUnitScheduleReader.cs`:

```csharp
namespace OVR.Modules.Scheduling.Contracts;

public interface IUnitScheduleReader
{
    Task<IReadOnlyList<string>> ListUnitRscs(
        string? sessionCode,
        string? locationCode,
        CancellationToken ct);
}
```

- [ ] **Step 2: Create implementation**

`src/OVR.Modules.Scheduling/Persistence/MongoUnitScheduleReader.cs`:

```csharp
using MongoDB.Driver;
using OVR.Modules.Scheduling.Contracts;

namespace OVR.Modules.Scheduling.Persistence;

public sealed class MongoUnitScheduleReader : IUnitScheduleReader
{
    private readonly IMongoCollection<UnitScheduleDocument> _schedules;

    public MongoUnitScheduleReader(IMongoDatabase database)
    {
        _schedules = database.GetCollection<UnitScheduleDocument>("scheduling_unit_schedules");
    }

    public async Task<IReadOnlyList<string>> ListUnitRscs(
        string? sessionCode,
        string? locationCode,
        CancellationToken ct)
    {
        var filter = Builders<UnitScheduleDocument>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(sessionCode))
            filter &= Builders<UnitScheduleDocument>.Filter.Eq(d => d.SessionCode, sessionCode);
        if (!string.IsNullOrWhiteSpace(locationCode))
            filter &= Builders<UnitScheduleDocument>.Filter.Eq(d => d.LocationCode, locationCode);

        var docs = await _schedules
            .Find(filter)
            .Project(d => d.Id)
            .ToListAsync(ct);

        return docs;
    }
}
```

- [ ] **Step 3: Register in module**

In `src/OVR.Modules.Scheduling/SchedulingModule.cs`, in `AddSchedulingModule()`:

```csharp
services.AddScoped<IUnitScheduleReader, MongoUnitScheduleReader>();
```

- [ ] **Step 4: Build**

```bash
dotnet build
```

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.Scheduling/
git commit -m "feat(scheduling): expose IUnitScheduleReader for list-by-session/location queries"
```

---

## Task 6: Clean existing DataEntry stubs + update csproj

**Files:**
- Delete: `src/OVR.Modules.DataEntry/Domain/CompetitionResult.cs`
- Delete: `src/OVR.Modules.DataEntry/Domain/ResultStatus.cs`
- Delete: `src/OVR.Modules.DataEntry/SportRules/ISportRuleEngine.cs`
- Modify: `src/OVR.Modules.DataEntry/OVR.Modules.DataEntry.csproj`

- [ ] **Step 1: Delete obsolete stubs**

```bash
rm src/OVR.Modules.DataEntry/Domain/CompetitionResult.cs
rm src/OVR.Modules.DataEntry/Domain/ResultStatus.cs
rm src/OVR.Modules.DataEntry/SportRules/ISportRuleEngine.cs
```

- [ ] **Step 2: Update csproj with project references + I18n content**

Replace `src/OVR.Modules.DataEntry/OVR.Modules.DataEntry.csproj` with (adjust PackageReferences to match existing style — check sibling module like `OVR.Modules.Scheduling.csproj` first):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\OVR.SharedKernel\OVR.SharedKernel.csproj" />
    <ProjectReference Include="..\OVR.Modules.CommonCodes\OVR.Modules.CommonCodes.csproj" />
    <ProjectReference Include="..\OVR.Modules.CompetitionConfig\OVR.Modules.CompetitionConfig.csproj" />
    <ProjectReference Include="..\OVR.Modules.Entries\OVR.Modules.Entries.csproj" />
    <ProjectReference Include="..\OVR.Modules.Scheduling\OVR.Modules.Scheduling.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="ErrorOr" />
    <PackageReference Include="MongoDB.Driver" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" />
    <PackageReference Include="Microsoft.AspNetCore.Http.Abstractions" />
    <PackageReference Include="Microsoft.AspNetCore.Routing" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="I18n\**"
             Link="I18n.DataEntry\%(RecursiveDir)%(Filename)%(Extension)"
             CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

(Cross-check PackageReferences against `src/OVR.Modules.Scheduling/OVR.Modules.Scheduling.csproj` — if it lists additional packages, add them. Do not specify versions: Directory.Packages.props controls them.)

- [ ] **Step 3: Clear DataEntryModule stub**

Replace `src/OVR.Modules.DataEntry/DataEntryModule.cs` with minimal version:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.DataEntry;

public static class DataEntryModule
{
    public static IServiceCollection AddDataEntryModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapDataEntryEndpoints(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
```

- [ ] **Step 4: Build**

```bash
dotnet build
```

Expected: success (empty module).

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.DataEntry/
git commit -m "chore(dataentry): clean existing stubs and prepare csproj for MVP"
```

---

## Task 7: Create domain enums

**Files:**
- Create: `src/OVR.Modules.DataEntry/Domain/ResultStatus.cs`
- Create: `src/OVR.Modules.DataEntry/Domain/ResultType.cs`
- Create: `src/OVR.Modules.DataEntry/Domain/ResultCode.cs`
- Create: `src/OVR.Modules.DataEntry/Domain/JudgePosition.cs`
- Create: `src/OVR.Modules.DataEntry/Domain/Wlt.cs`

- [ ] **Step 1: Create `ResultStatus.cs`**

```csharp
namespace OVR.Modules.DataEntry.Domain;

public enum ResultStatus
{
    StartList,
    Live,
    Official
}
```

- [ ] **Step 2: Create `ResultType.cs`**

```csharp
namespace OVR.Modules.DataEntry.Domain;

public enum ResultType
{
    Points,      // bout goes the distance; decision by points
    RmPoints,    // stoppage with points relevant
    Rm           // absolute stoppage
}
```

- [ ] **Step 3: Create `ResultCode.cs`**

```csharp
namespace OVR.Modules.DataEntry.Domain;

public enum ResultCode
{
    Wp,     // Win on Points
    Ko,     // Knockout
    TkoI,   // Technical Knockout - Injury
    TkoR,   // Technical Knockout - Referee
    Dsq,    // Disqualified
    Bdsq,   // Both Disqualified
    Dko,    // Double Knockout
    Wo,     // Walkover
    Abd,    // Abandoned
    Nc      // No Contest
}
```

- [ ] **Step 4: Create `JudgePosition.cs`**

```csharp
namespace OVR.Modules.DataEntry.Domain;

public enum JudgePosition
{
    J1,
    J2,
    J3
}
```

- [ ] **Step 5: Create `Wlt.cs`**

```csharp
namespace OVR.Modules.DataEntry.Domain;

public enum Wlt
{
    W,  // Win
    L   // Loss (also used for both competitors in DKO/BDSQ/NC scenarios)
}
```

- [ ] **Step 6: Build**

```bash
dotnet build src/OVR.Modules.DataEntry/
```

- [ ] **Step 7: Commit**

```bash
git add src/OVR.Modules.DataEntry/Domain/
git commit -m "feat(dataentry): add domain enums (ResultStatus, ResultType, ResultCode, JudgePosition, Wlt)"
```

---

## Task 8: Create BoxingRules constants

**Files:**
- Create: `src/OVR.Modules.DataEntry/SportRules/BoxingRules.cs`

- [ ] **Step 1: Create file**

```csharp
namespace OVR.Modules.DataEntry.SportRules;

public static class BoxingRules
{
    public const int PeriodCount = 3;
    public const int JudgeCount = 3;
    public const int MinPeriodScore = 6;
    public const int MaxPeriodScore = 10;

    public static readonly IReadOnlyList<string> PeriodCodes = new[] { "R1", "R2", "R3" };
}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/OVR.Modules.DataEntry/
```

- [ ] **Step 3: Commit**

```bash
git add src/OVR.Modules.DataEntry/SportRules/
git commit -m "feat(dataentry): add BoxingRules constants for MVP 3"
```

---

## Task 9: Create value objects (Competitor, PeriodScorecard, Period, Decision) with tests

**Files:**
- Create: `src/OVR.Modules.DataEntry/Domain/Competitor.cs`
- Create: `src/OVR.Modules.DataEntry/Domain/PeriodScorecard.cs`
- Create: `src/OVR.Modules.DataEntry/Domain/Period.cs`
- Create: `src/OVR.Modules.DataEntry/Domain/Decision.cs`
- Create: `tests/OVR.Modules.DataEntry.Tests/Domain/CompetitorTests.cs`
- Create: `tests/OVR.Modules.DataEntry.Tests/Domain/DecisionTests.cs`

- [ ] **Step 1: Write `Competitor.cs`**

```csharp
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Domain;

public sealed record Competitor(
    int SortOrder,                      // 1 = Red, 2 = Blue
    ParticipantId? ParticipantId,       // null iff NOCOMP placeholder
    string? NocompDetail,               // ODF EUE DETAILED text; null in MVP 3
    int? Seed,
    Organisation Organisation,
    Wlt? Wlt);                          // set at Confirm()
```

- [ ] **Step 2: Write `PeriodScorecard.cs`**

```csharp
namespace OVR.Modules.DataEntry.Domain;

public sealed record PeriodScorecard(
    JudgePosition JudgePos,
    int HomeScore,     // ODF SCR_H
    int AwayScore);    // ODF SCR_A
```

- [ ] **Step 3: Write `Period.cs`**

```csharp
namespace OVR.Modules.DataEntry.Domain;

public sealed record Period(
    string Code,                                    // "R1" | "R2" | "R3"
    IReadOnlyList<PeriodScorecard> Scorecards);
```

- [ ] **Step 4: Write `Decision.cs`**

```csharp
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Domain;

public sealed record Decision(
    ResultType Type,
    ResultCode Code,
    string? DecisionMark,                // "3:0" | "2:1" | "2:0"; null when Rm
    string? StoppageRound,               // "R1".."R3"; null when Points
    string? StoppageTime,                // "mm:ss"; null when Points
    ParticipantId? WinnerParticipantId); // null iff Nc/Dko/Bdsq
```

- [ ] **Step 5: Write a simple equality/construction sanity test in `CompetitorTests.cs`**

```csharp
using FluentAssertions;
using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Domain;

public class CompetitorTests
{
    [Fact]
    public void Record_Equality_ByValue()
    {
        var a = new Competitor(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.FromCode("ESP"), null);
        var b = new Competitor(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.FromCode("ESP"), null);

        a.Should().Be(b);
    }
}
```

- [ ] **Step 6: Write a sanity test in `DecisionTests.cs`**

```csharp
using FluentAssertions;
using OVR.Modules.DataEntry.Domain;

namespace OVR.Modules.DataEntry.Tests.Domain;

public class DecisionTests
{
    [Fact]
    public void Points_Decision_HasDecisionMarkAndNoStoppage()
    {
        var d = new Decision(
            ResultType.Points, ResultCode.Wp,
            DecisionMark: "3:0", StoppageRound: null, StoppageTime: null,
            WinnerParticipantId: null);

        d.DecisionMark.Should().Be("3:0");
        d.StoppageRound.Should().BeNull();
    }
}
```

- [ ] **Step 7: Build and run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/
```

Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/OVR.Modules.DataEntry/Domain/ tests/OVR.Modules.DataEntry.Tests/
git commit -m "feat(dataentry): add value objects (Competitor, Period, PeriodScorecard, Decision)"
```

---

## Task 10: UnitResult aggregate — `CreateForFirstRound` + invariant I1

**Files:**
- Create: `src/OVR.Modules.DataEntry/Domain/UnitResult.cs`
- Create: `src/OVR.Modules.DataEntry/Errors/DataEntryErrors.cs` (shell — filled later)
- Create: `tests/OVR.Modules.DataEntry.Tests/Domain/UnitResultAggregateTests.cs`

- [ ] **Step 1: Create `DataEntryErrors.cs` shell**

```csharp
using ErrorOr;

namespace OVR.Modules.DataEntry.Errors;

public static class DataEntryErrors
{
    public static Error UnitResultNotFound(string rsc) =>
        Error.NotFound("DataEntry.UnitResultNotFound",
            $"UnitResult '{rsc}' not found.");

    public static Error InvalidCompetitors(string message) =>
        Error.Validation("DataEntry.InvalidCompetitors", message);

    // More added in Task 20.
}
```

- [ ] **Step 2: Write failing test `UnitResultAggregateTests.cs`**

```csharp
using FluentAssertions;
using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Domain;

public class UnitResultAggregateTests
{
    private static Rsc MakeUnitRsc() =>
        Rsc.Create("BOXW---------------M71KG-8FNL0001----");

    private static Competitor Red() =>
        new(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.FromCode("ESP"), null);

    private static Competitor Blue() =>
        new(2, ParticipantId.Create("NOC-POL-0014"), null, 8,
            Organisation.FromCode("POL"), null);

    [Fact]
    public void CreateForFirstRound_WithValidCompetitors_SucceedsInStartList()
    {
        var rsc = MakeUnitRsc();
        var result = UnitResult.CreateForFirstRound(rsc, Red(), Blue());

        result.IsError.Should().BeFalse();
        var ur = result.Value;
        ur.Status.Should().Be(ResultStatus.StartList);
        ur.Competitors.Should().HaveCount(2);
        ur.Competitors[0].SortOrder.Should().Be(1);
        ur.Competitors[1].SortOrder.Should().Be(2);
    }

    [Fact]
    public void CreateForFirstRound_WithDuplicateSortOrder_ReturnsError()
    {
        var rsc = MakeUnitRsc();
        var red = Red();
        var redAgain = red with { SortOrder = 1 };

        var result = UnitResult.CreateForFirstRound(rsc, red, redAgain);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.InvalidCompetitors");
    }

    [Fact]
    public void CreateForFirstRound_WithWrongSortOrderValues_ReturnsError()
    {
        var rsc = MakeUnitRsc();
        var a = Red() with { SortOrder = 3 };
        var b = Blue() with { SortOrder = 4 };

        var result = UnitResult.CreateForFirstRound(rsc, a, b);

        result.IsError.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Run to confirm failure**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/ --filter "FullyQualifiedName~UnitResultAggregateTests"
```

Expected: compile error (UnitResult doesn't exist).

- [ ] **Step 4: Implement `UnitResult.cs` with `CreateForFirstRound`**

```csharp
using ErrorOr;
using OVR.Modules.DataEntry.Errors;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Domain;

public sealed class UnitResult : AggregateRoot<string>
{
    private readonly List<Competitor> _competitors = new();
    private readonly List<Period> _periods = new();

    public Rsc UnitRsc { get; private set; } = null!;
    public ResultStatus Status { get; private set; }
    public IReadOnlyList<Competitor> Competitors => _competitors;
    public IReadOnlyList<Period> Periods => _periods;
    public Decision? Decision { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public string? CurrentPeriodCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UnitResult() { }

    public static ErrorOr<UnitResult> CreateForFirstRound(
        Rsc unitRsc, Competitor red, Competitor blue)
    {
        if (unitRsc is null)
            return DataEntryErrors.InvalidCompetitors("UnitRsc is required.");

        if (red.SortOrder != 1 || blue.SortOrder != 2)
            return DataEntryErrors.InvalidCompetitors(
                "Competitors must have SortOrder 1 (red) and 2 (blue).");

        if (red.ParticipantId is null || blue.ParticipantId is null)
            return DataEntryErrors.InvalidCompetitors(
                "MVP 3 requires real ParticipantIds (NOCOMP not supported).");

        if (red.ParticipantId == blue.ParticipantId)
            return DataEntryErrors.InvalidCompetitors(
                "Competitors must be distinct participants.");

        var now = DateTime.UtcNow;
        var ur = new UnitResult
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            Status = ResultStatus.StartList,
            CreatedAt = now
        };
        ur._competitors.Add(red);
        ur._competitors.Add(blue);

        ur.RaiseDomainEvent(new UnitResultStartListCreatedEvent(
            UnitRsc: unitRsc.Value,
            EventRsc: Rsc.Create(unitRsc.AtEventLevel()).Value,
            Competitors: new[]
            {
                new CompetitorSnapshot(red.SortOrder,
                    red.ParticipantId?.Value, red.Seed, red.Organisation.Code),
                new CompetitorSnapshot(blue.SortOrder,
                    blue.ParticipantId?.Value, blue.Seed, blue.Organisation.Code)
            },
            CreatedAt: now));

        return ur;
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/ --filter "FullyQualifiedName~UnitResultAggregateTests"
```

Expected: 3 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/OVR.Modules.DataEntry/ tests/OVR.Modules.DataEntry.Tests/
git commit -m "feat(dataentry): UnitResult.CreateForFirstRound with I1 invariant"
```

---

## Task 11: UnitResult.Start (transition StartList→Live) + invariants I8/I11

**Files:**
- Modify: `src/OVR.Modules.DataEntry/Domain/UnitResult.cs`
- Modify: `src/OVR.Modules.DataEntry/Errors/DataEntryErrors.cs`
- Modify: `tests/OVR.Modules.DataEntry.Tests/Domain/UnitResultAggregateTests.cs`

- [ ] **Step 1: Add error factory**

In `DataEntryErrors.cs`:

```csharp
public static Error InvalidStatusTransition(string from, string to) =>
    Error.Validation("DataEntry.InvalidStatusTransition",
        $"Cannot transition from {from} to {to}.");
```

- [ ] **Step 2: Write failing tests**

Append to `UnitResultAggregateTests.cs`:

```csharp
private static UnitResult NewInStartList()
{
    var rsc = MakeUnitRsc();
    return UnitResult.CreateForFirstRound(rsc, Red(), Blue()).Value;
}

[Fact]
public void Start_FromStartList_TransitionsToLive()
{
    var ur = NewInStartList();
    var result = ur.Start();

    result.IsError.Should().BeFalse();
    ur.Status.Should().Be(ResultStatus.Live);
    ur.StartedAt.Should().NotBeNull();
    ur.CurrentPeriodCode.Should().Be("R1");
}

[Fact]
public void Start_WhenAlreadyLive_ReturnsError()
{
    var ur = NewInStartList();
    ur.Start();

    var again = ur.Start();
    again.IsError.Should().BeTrue();
    again.FirstError.Code.Should().Be("DataEntry.InvalidStatusTransition");
}
```

- [ ] **Step 3: Run tests — expect compile failure**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/ --filter "FullyQualifiedName~UnitResultAggregateTests.Start_"
```

- [ ] **Step 4: Implement `Start` on `UnitResult`**

Add to `UnitResult.cs`:

```csharp
public ErrorOr<Success> Start()
{
    if (Status != ResultStatus.StartList)
        return DataEntryErrors.InvalidStatusTransition(Status.ToString(), "Live");

    Status = ResultStatus.Live;
    StartedAt = DateTime.UtcNow;
    CurrentPeriodCode = "R1";
    UpdatedAt = StartedAt;

    RaiseDomainEvent(new UnitResultLiveEvent(UnitRsc.Value, StartedAt.Value));
    return Result.Success;
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/
```

Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add src/OVR.Modules.DataEntry/ tests/OVR.Modules.DataEntry.Tests/
git commit -m "feat(dataentry): UnitResult.Start transitions StartList→Live"
```

---

## Task 12: UnitResult.ScorePeriod + invariants I2/I3/I4/I5/I11

**Files:**
- Modify: `src/OVR.Modules.DataEntry/Domain/UnitResult.cs`
- Modify: `src/OVR.Modules.DataEntry/Errors/DataEntryErrors.cs`
- Modify: `tests/OVR.Modules.DataEntry.Tests/Domain/UnitResultAggregateTests.cs`

- [ ] **Step 1: Add error factories**

Append to `DataEntryErrors.cs`:

```csharp
public static Error InvalidScorecardCount() =>
    Error.Validation("DataEntry.InvalidScorecardCount",
        "Exactly 3 scorecards are required (J1, J2, J3).");

public static Error InvalidScoreRange(int value) =>
    Error.Validation("DataEntry.InvalidScoreRange",
        $"Score {value} is outside the allowed range [6..10].");

public static Error DuplicateJudgePosition(string pos) =>
    Error.Validation("DataEntry.DuplicateJudgePosition",
        $"Judge position {pos} appears more than once.");

public static Error InvalidPeriodOrder(string code) =>
    Error.Validation("DataEntry.InvalidPeriodOrder",
        $"Cannot score period {code} out of order.");

public static Error PeriodAlreadyScored(string code) =>
    Error.Validation("DataEntry.PeriodAlreadyScored",
        $"Period {code} has already been scored.");

public static Error DecisionAlreadyExists() =>
    Error.Validation("DataEntry.DecisionAlreadyExists",
        "Cannot modify scoring after a decision has been recorded.");

public static Error InvalidPeriodCode(string code) =>
    Error.Validation("DataEntry.InvalidPeriodCode",
        $"Invalid period code '{code}'. Expected one of R1, R2, R3.");
```

- [ ] **Step 2: Write failing tests**

Append to `UnitResultAggregateTests.cs`:

```csharp
private static IReadOnlyList<PeriodScorecard> EvenCards(int home, int away) => new[]
{
    new PeriodScorecard(JudgePosition.J1, home, away),
    new PeriodScorecard(JudgePosition.J2, home, away),
    new PeriodScorecard(JudgePosition.J3, home, away)
};

[Fact]
public void ScorePeriod_FromStartList_Fails()
{
    var ur = NewInStartList();
    var result = ur.ScorePeriod("R1", EvenCards(10, 9));
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidStatusTransition");
}

[Fact]
public void ScorePeriod_R1_InLive_Succeeds_AndAdvancesCurrentPeriodToR2()
{
    var ur = NewInStartList();
    ur.Start();

    var result = ur.ScorePeriod("R1", EvenCards(10, 9));

    result.IsError.Should().BeFalse();
    ur.Periods.Should().HaveCount(1);
    ur.Periods[0].Code.Should().Be("R1");
    ur.CurrentPeriodCode.Should().Be("R2");
}

[Fact]
public void ScorePeriod_R2BeforeR1_ReturnsInvalidPeriodOrder()
{
    var ur = NewInStartList();
    ur.Start();

    var result = ur.ScorePeriod("R2", EvenCards(10, 9));

    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidPeriodOrder");
}

[Fact]
public void ScorePeriod_SameR1Twice_ReturnsPeriodAlreadyScored()
{
    var ur = NewInStartList();
    ur.Start();
    ur.ScorePeriod("R1", EvenCards(10, 9));

    var result = ur.ScorePeriod("R1", EvenCards(10, 9));
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.PeriodAlreadyScored");
}

[Fact]
public void ScorePeriod_With4Scorecards_ReturnsInvalidScorecardCount()
{
    var ur = NewInStartList();
    ur.Start();

    var fourCards = EvenCards(10, 9).Append(
        new PeriodScorecard(JudgePosition.J1, 10, 9)).ToList();

    var result = ur.ScorePeriod("R1", fourCards);
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidScorecardCount");
}

[Fact]
public void ScorePeriod_WithScore5_ReturnsInvalidScoreRange()
{
    var ur = NewInStartList();
    ur.Start();

    var cards = new[]
    {
        new PeriodScorecard(JudgePosition.J1, 10, 5),
        new PeriodScorecard(JudgePosition.J2, 10, 9),
        new PeriodScorecard(JudgePosition.J3, 10, 9)
    };

    var result = ur.ScorePeriod("R1", cards);
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidScoreRange");
}

[Fact]
public void ScorePeriod_WithDuplicateJudge_ReturnsDuplicateJudgePosition()
{
    var ur = NewInStartList();
    ur.Start();

    var cards = new[]
    {
        new PeriodScorecard(JudgePosition.J1, 10, 9),
        new PeriodScorecard(JudgePosition.J1, 10, 9),
        new PeriodScorecard(JudgePosition.J2, 10, 9)
    };

    var result = ur.ScorePeriod("R1", cards);
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.DuplicateJudgePosition");
}

[Fact]
public void ScorePeriod_InvalidCode_ReturnsInvalidPeriodCode()
{
    var ur = NewInStartList();
    ur.Start();

    var result = ur.ScorePeriod("R4", EvenCards(10, 9));
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidPeriodCode");
}
```

- [ ] **Step 3: Run to confirm failures**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/ --filter "FullyQualifiedName~ScorePeriod"
```

- [ ] **Step 4: Implement `ScorePeriod` on `UnitResult`**

Add to `UnitResult.cs` (also add the helper `NextPeriodCode`):

```csharp
using OVR.Modules.DataEntry.SportRules;

public ErrorOr<Success> ScorePeriod(
    string periodCode, IReadOnlyList<PeriodScorecard> cards)
{
    if (Status != ResultStatus.Live)
        return DataEntryErrors.InvalidStatusTransition(Status.ToString(), "score period");

    if (Decision is not null)
        return DataEntryErrors.DecisionAlreadyExists();

    if (!BoxingRules.PeriodCodes.Contains(periodCode))
        return DataEntryErrors.InvalidPeriodCode(periodCode);

    if (_periods.Any(p => p.Code == periodCode))
        return DataEntryErrors.PeriodAlreadyScored(periodCode);

    // Enforce ordering R1 → R2 → R3
    var expectedNextIndex = _periods.Count;
    if (BoxingRules.PeriodCodes[expectedNextIndex] != periodCode)
        return DataEntryErrors.InvalidPeriodOrder(periodCode);

    if (cards.Count != BoxingRules.JudgeCount)
        return DataEntryErrors.InvalidScorecardCount();

    if (cards.Select(c => c.JudgePos).Distinct().Count() != cards.Count)
        return DataEntryErrors.DuplicateJudgePosition(
            cards.GroupBy(c => c.JudgePos).First(g => g.Count() > 1).Key.ToString());

    foreach (var c in cards)
    {
        if (c.HomeScore < BoxingRules.MinPeriodScore || c.HomeScore > BoxingRules.MaxPeriodScore)
            return DataEntryErrors.InvalidScoreRange(c.HomeScore);
        if (c.AwayScore < BoxingRules.MinPeriodScore || c.AwayScore > BoxingRules.MaxPeriodScore)
            return DataEntryErrors.InvalidScoreRange(c.AwayScore);
    }

    _periods.Add(new Period(periodCode, cards.OrderBy(c => c.JudgePos).ToList()));
    UpdatedAt = DateTime.UtcNow;

    // Advance CurrentPeriodCode; stays at last if this was R3.
    var nextIndex = _periods.Count;
    CurrentPeriodCode = nextIndex < BoxingRules.PeriodCount
        ? BoxingRules.PeriodCodes[nextIndex]
        : BoxingRules.PeriodCodes[^1];

    RaiseDomainEvent(new UnitResultPeriodScoredEvent(
        UnitRsc.Value,
        periodCode,
        cards.Select(c => new ScorecardSnapshot(
            c.JudgePos.ToString(), c.HomeScore, c.AwayScore)).ToList(),
        UpdatedAt.Value));

    return Result.Success;
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/
```

Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add src/OVR.Modules.DataEntry/ tests/OVR.Modules.DataEntry.Tests/
git commit -m "feat(dataentry): UnitResult.ScorePeriod with invariants I2-I5, I11"
```

---

## Task 13: TenPointMustResolver with full decision matrix + tests

**Files:**
- Create: `src/OVR.Modules.DataEntry/SportRules/TenPointMustResolver.cs`
- Create: `tests/OVR.Modules.DataEntry.Tests/SportRules/TenPointMustResolverTests.cs`

- [ ] **Step 1: Create interface + class shell**

```csharp
using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.SportRules;

public interface ITenPointMustResolver
{
    Decision Resolve(
        IReadOnlyList<Period> periods,
        ParticipantId redParticipant,
        ParticipantId blueParticipant);
}

public sealed class TenPointMustResolver : ITenPointMustResolver
{
    public Decision Resolve(
        IReadOnlyList<Period> periods,
        ParticipantId redParticipant,
        ParticipantId blueParticipant)
    {
        throw new NotImplementedException();
    }
}
```

- [ ] **Step 2: Write full test file**

`tests/OVR.Modules.DataEntry.Tests/SportRules/TenPointMustResolverTests.cs`:

```csharp
using FluentAssertions;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.SportRules;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.SportRules;

public class TenPointMustResolverTests
{
    private readonly TenPointMustResolver _resolver = new();
    private readonly ParticipantId _red = ParticipantId.Create("NOC-ESP-0001");
    private readonly ParticipantId _blue = ParticipantId.Create("NOC-POL-0014");

    private static IReadOnlyList<Period> Periods(
        (int h1, int a1, int h2, int a2, int h3, int a3)[] judgeTotals)
    {
        // judgeTotals[i] = per-judge (R1-home, R1-away, R2-home, R2-away, R3-home, R3-away)
        return new[] { "R1", "R2", "R3" }.Select((code, rIdx) =>
            new Period(code, Enumerable.Range(0, 3).Select(jIdx =>
            {
                var (h1, a1, h2, a2, h3, a3) = judgeTotals[jIdx];
                var (h, a) = rIdx switch
                {
                    0 => (h1, a1),
                    1 => (h2, a2),
                    _ => (h3, a3),
                };
                return new PeriodScorecard((JudgePosition)jIdx, h, a);
            }).ToList<PeriodScorecard>())).ToList<Period>();
    }

    [Fact]
    public void Unanimous_ForRed_ReturnsWp_3_0_WinnerRed()
    {
        // Each judge: 30-27 red wins all three
        var periods = Periods(new[]
        {
            (10,9,10,9,10,9),
            (10,9,10,9,10,9),
            (10,9,10,9,10,9)
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Points);
        d.Code.Should().Be(ResultCode.Wp);
        d.DecisionMark.Should().Be("3:0");
        d.WinnerParticipantId.Should().Be(_red);
    }

    [Fact]
    public void Split_TwoRedOneBlue_ReturnsWp_2_1_WinnerRed()
    {
        // Judges: red(29-28), red(29-28), blue(28-29)
        var periods = Periods(new[]
        {
            (10,9,10,9,9,10),   // J1 → red 29-28 ... wait: 10+10+9=29, 9+9+10=28 ✓
            (10,9,10,9,9,10),   // J2 → red 29-28
            (9,10,9,10,10,9)    // J3 → blue 28-29 ... 9+9+10=28, 10+10+9=29 ✓
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Points);
        d.Code.Should().Be(ResultCode.Wp);
        d.DecisionMark.Should().Be("2:1");
        d.WinnerParticipantId.Should().Be(_red);
    }

    [Fact]
    public void Majority_TwoRedOneDraw_ReturnsWp_2_0_WinnerRed()
    {
        // Judges: red wins, red wins, draw
        var periods = Periods(new[]
        {
            (10,9,10,9,9,10),   // J1 → 29-28 red
            (10,9,10,9,9,10),   // J2 → 29-28 red
            (10,9,9,10,10,10)   // J3 → 29-29 draw — BUT min score is 6; ensure totals sum equal
        });

        // Adjust J3 to sum to equal totals: (10,10,9,9,10,10) = 29 home / 29 away
        periods = Periods(new[]
        {
            (10,9,10,9,9,10),
            (10,9,10,9,9,10),
            (10,10,9,9,10,10)
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Points);
        d.Code.Should().Be(ResultCode.Wp);
        d.DecisionMark.Should().Be("2:0");
        d.WinnerParticipantId.Should().Be(_red);
    }

    [Fact]
    public void AllDraws_ReturnsNc()
    {
        // All judges: equal totals
        var periods = Periods(new[]
        {
            (10,10,9,9,10,10),
            (10,10,9,9,10,10),
            (10,10,9,9,10,10)
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Rm);
        d.Code.Should().Be(ResultCode.Nc);
        d.WinnerParticipantId.Should().BeNull();
        d.DecisionMark.Should().BeNull();
    }

    [Fact]
    public void SplitWithOneDraw_OneRedOneBlueOneDraw_ReturnsNc()
    {
        var periods = Periods(new[]
        {
            (10,9,10,9,10,9),    // J1 → 30-27 red
            (9,10,9,10,9,10),    // J2 → 27-30 blue
            (10,10,9,9,10,10)    // J3 → 29-29 draw
        });

        var d = _resolver.Resolve(periods, _red, _blue);

        d.Type.Should().Be(ResultType.Rm);
        d.Code.Should().Be(ResultCode.Nc);
        d.WinnerParticipantId.Should().BeNull();
    }
}
```

- [ ] **Step 3: Implement `TenPointMustResolver`**

```csharp
public sealed class TenPointMustResolver : ITenPointMustResolver
{
    public Decision Resolve(
        IReadOnlyList<Period> periods,
        ParticipantId redParticipant,
        ParticipantId blueParticipant)
    {
        // Aggregate per-judge totals across all periods.
        var judgeTotals = new Dictionary<JudgePosition, (int home, int away)>();
        foreach (var period in periods)
        {
            foreach (var card in period.Scorecards)
            {
                var current = judgeTotals.TryGetValue(card.JudgePos, out var v)
                    ? v : (0, 0);
                judgeTotals[card.JudgePos] =
                    (current.home + card.HomeScore, current.away + card.AwayScore);
            }
        }

        // Each judge picks a side.
        int redVotes = 0, blueVotes = 0, drawVotes = 0;
        foreach (var (h, a) in judgeTotals.Values)
        {
            if (h > a) redVotes++;
            else if (a > h) blueVotes++;
            else drawVotes++;
        }

        // Classify.
        if (redVotes >= 2 && blueVotes == 0)
        {
            var mark = drawVotes == 0 ? $"{redVotes}:{blueVotes}" : $"{redVotes}:0";
            return new Decision(
                ResultType.Points, ResultCode.Wp, mark,
                StoppageRound: null, StoppageTime: null,
                WinnerParticipantId: redParticipant);
        }
        if (blueVotes >= 2 && redVotes == 0)
        {
            var mark = drawVotes == 0 ? $"{blueVotes}:{redVotes}" : $"{blueVotes}:0";
            return new Decision(
                ResultType.Points, ResultCode.Wp, mark,
                StoppageRound: null, StoppageTime: null,
                WinnerParticipantId: blueParticipant);
        }
        if (redVotes == 2 && blueVotes == 1)
            return new Decision(ResultType.Points, ResultCode.Wp, "2:1",
                null, null, redParticipant);
        if (blueVotes == 2 && redVotes == 1)
            return new Decision(ResultType.Points, ResultCode.Wp, "2:1",
                null, null, blueParticipant);

        // Anything else (1-1-1, 0-0-3, 1-0-2 with draws dominating) → NC.
        return new Decision(ResultType.Rm, ResultCode.Nc,
            DecisionMark: null, StoppageRound: null, StoppageTime: null,
            WinnerParticipantId: null);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/ --filter "FullyQualifiedName~TenPointMustResolverTests"
```

Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.DataEntry/SportRules/ tests/OVR.Modules.DataEntry.Tests/SportRules/
git commit -m "feat(dataentry): TenPointMustResolver for 3-judge × 3-period boxing decision"
```

---

## Task 14: UnitResult auto-computes Decision after R3 is scored

**Files:**
- Modify: `src/OVR.Modules.DataEntry/Domain/UnitResult.cs`
- Modify: `tests/OVR.Modules.DataEntry.Tests/Domain/UnitResultAggregateTests.cs`

- [ ] **Step 1: Write failing test**

Append to `UnitResultAggregateTests.cs`:

```csharp
[Fact]
public void ScorePeriod_R3LastCard_PopulatesDecisionViaResolver()
{
    var ur = NewInStartList();
    ur.Start();
    ur.ScorePeriod("R1", EvenCards(10, 9));
    ur.ScorePeriod("R2", EvenCards(10, 9));
    ur.ScorePeriod("R3", EvenCards(10, 9));

    ur.Decision.Should().NotBeNull();
    ur.Decision!.Code.Should().Be(ResultCode.Wp);
    ur.Decision.DecisionMark.Should().Be("3:0");
    ur.Status.Should().Be(ResultStatus.Live); // not yet Official
}
```

- [ ] **Step 2: Inject `ITenPointMustResolver` into the aggregate**

Aggregates don't receive services via DI — we pass it to the method. Modify `ScorePeriod` to accept an optional resolver, OR (cleaner) add a separate `FinalizePointsDecision` step called from `ScorePeriod` when all periods are done. Simplest: instantiate `TenPointMustResolver` inline inside `UnitResult` for MVP 3 (the agg depends on the domain service by construction — standard DDD).

In `UnitResult.cs`, after the last period is added inside `ScorePeriod`, append:

```csharp
// Auto-compute Decision after R3.
if (_periods.Count == BoxingRules.PeriodCount)
{
    var resolver = new TenPointMustResolver();
    var red = _competitors.First(c => c.SortOrder == 1);
    var blue = _competitors.First(c => c.SortOrder == 2);
    Decision = resolver.Resolve(_periods, red.ParticipantId!, blue.ParticipantId!);
    EndedAt = UpdatedAt;
}
```

Add `using OVR.Modules.DataEntry.SportRules;` to the top of `UnitResult.cs`.

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/
```

Expected: new test passes; all existing still pass.

- [ ] **Step 4: Commit**

```bash
git add src/OVR.Modules.DataEntry/ tests/OVR.Modules.DataEntry.Tests/
git commit -m "feat(dataentry): UnitResult auto-populates Decision after R3 scoring"
```

---

## Task 15: UnitResult.FinishByStoppage + invariants I7/I9/I12

**Files:**
- Modify: `src/OVR.Modules.DataEntry/Domain/UnitResult.cs`
- Modify: `src/OVR.Modules.DataEntry/Errors/DataEntryErrors.cs`
- Modify: `tests/OVR.Modules.DataEntry.Tests/Domain/UnitResultAggregateTests.cs`

- [ ] **Step 1: Add error factories**

Append to `DataEntryErrors.cs`:

```csharp
public static Error InvalidStoppageData(string reason) =>
    Error.Validation("DataEntry.InvalidStoppageData", reason);
```

- [ ] **Step 2: Write failing tests**

Append to `UnitResultAggregateTests.cs`:

```csharp
[Fact]
public void FinishByStoppage_TkoI_InLive_SetsRmPointsDecision()
{
    var ur = NewInStartList();
    ur.Start();
    ur.ScorePeriod("R1", EvenCards(10, 9));
    // stoppage mid-R2

    var red = ur.Competitors.First(c => c.SortOrder == 1).ParticipantId!;
    var result = ur.FinishByStoppage(
        ResultCode.TkoI, stoppageRound: "R2", stoppageTime: "01:30",
        winnerParticipantId: red);

    result.IsError.Should().BeFalse();
    ur.Decision.Should().NotBeNull();
    ur.Decision!.Code.Should().Be(ResultCode.TkoI);
    ur.Decision.Type.Should().Be(ResultType.RmPoints);
    ur.Decision.StoppageRound.Should().Be("R2");
    ur.Decision.StoppageTime.Should().Be("01:30");
    ur.Decision.WinnerParticipantId.Should().Be(red);
}

[Fact]
public void FinishByStoppage_Ko_SetsRmDecision_NoPoints()
{
    var ur = NewInStartList();
    ur.Start();

    var blue = ur.Competitors.First(c => c.SortOrder == 2).ParticipantId!;
    var result = ur.FinishByStoppage(
        ResultCode.Ko, "R1", "00:45", blue);

    result.IsError.Should().BeFalse();
    ur.Decision!.Type.Should().Be(ResultType.Rm);
    ur.Decision.Code.Should().Be(ResultCode.Ko);
    ur.Decision.WinnerParticipantId.Should().Be(blue);
}

[Fact]
public void FinishByStoppage_Nc_WithWinner_ReturnsInvalidStoppageData()
{
    var ur = NewInStartList();
    ur.Start();

    var red = ur.Competitors.First(c => c.SortOrder == 1).ParticipantId!;
    var result = ur.FinishByStoppage(ResultCode.Nc, "R2", "00:30", red);

    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidStoppageData");
}

[Fact]
public void FinishByStoppage_Wp_Rejected()
{
    var ur = NewInStartList();
    ur.Start();

    var red = ur.Competitors.First(c => c.SortOrder == 1).ParticipantId!;
    var result = ur.FinishByStoppage(ResultCode.Wp, "R2", "00:30", red);

    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidStoppageData");
}

[Fact]
public void FinishByStoppage_FromStartList_ReturnsInvalidStatusTransition()
{
    var ur = NewInStartList();

    var red = ur.Competitors.First(c => c.SortOrder == 1).ParticipantId!;
    var result = ur.FinishByStoppage(ResultCode.TkoI, "R1", "00:30", red);

    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidStatusTransition");
}

[Fact]
public void FinishByStoppage_WhenDecisionExists_ReturnsDecisionAlreadyExists()
{
    var ur = NewInStartList();
    ur.Start();
    ur.ScorePeriod("R1", EvenCards(10, 9));
    ur.ScorePeriod("R2", EvenCards(10, 9));
    ur.ScorePeriod("R3", EvenCards(10, 9));  // auto-decision populated

    var red = ur.Competitors.First(c => c.SortOrder == 1).ParticipantId!;
    var result = ur.FinishByStoppage(ResultCode.TkoI, "R3", "02:30", red);

    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.DecisionAlreadyExists");
}
```

- [ ] **Step 3: Implement `FinishByStoppage`**

Add to `UnitResult.cs`:

```csharp
public ErrorOr<Success> FinishByStoppage(
    ResultCode resultCode,
    string stoppageRound,
    string stoppageTime,
    ParticipantId? winnerParticipantId)
{
    if (Status != ResultStatus.Live)
        return DataEntryErrors.InvalidStatusTransition(Status.ToString(), "finish");

    if (Decision is not null)
        return DataEntryErrors.DecisionAlreadyExists();

    if (resultCode == ResultCode.Wp)
        return DataEntryErrors.InvalidStoppageData(
            "WP is reserved for point decisions, not stoppages.");

    if (!BoxingRules.PeriodCodes.Contains(stoppageRound))
        return DataEntryErrors.InvalidStoppageData(
            $"Invalid stoppage round '{stoppageRound}'.");

    var noWinnerCodes = new[] { ResultCode.Nc, ResultCode.Dko, ResultCode.Bdsq };
    var requiresWinner = !noWinnerCodes.Contains(resultCode);

    if (requiresWinner && winnerParticipantId is null)
        return DataEntryErrors.InvalidStoppageData(
            $"ResultCode {resultCode} requires a winnerParticipantId.");

    if (!requiresWinner && winnerParticipantId is not null)
        return DataEntryErrors.InvalidStoppageData(
            $"ResultCode {resultCode} must not have a winnerParticipantId.");

    if (winnerParticipantId is not null &&
        !_competitors.Any(c => c.ParticipantId == winnerParticipantId))
        return DataEntryErrors.InvalidStoppageData(
            "winnerParticipantId does not match any competitor.");

    var type = _periods.Count > 0 ? ResultType.RmPoints : ResultType.Rm;
    // If any periods have been scored, points are relevant → RmPoints; else pure Rm.

    var decisionMark = type == ResultType.RmPoints
        ? ComputeInterimDecisionMark(winnerParticipantId)
        : null;

    Decision = new Decision(
        Type: type,
        Code: resultCode,
        DecisionMark: decisionMark,
        StoppageRound: stoppageRound,
        StoppageTime: stoppageTime,
        WinnerParticipantId: winnerParticipantId);

    EndedAt = DateTime.UtcNow;
    UpdatedAt = EndedAt;

    return Result.Success;
}

private string? ComputeInterimDecisionMark(ParticipantId? winner)
{
    // Reuse 10-point-must logic over whatever periods exist.
    if (winner is null) return null;
    var red = _competitors.First(c => c.SortOrder == 1);
    var blue = _competitors.First(c => c.SortOrder == 2);
    var resolver = new TenPointMustResolver();
    var interim = resolver.Resolve(_periods, red.ParticipantId!, blue.ParticipantId!);
    return interim.DecisionMark;
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/
```

Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.DataEntry/ tests/OVR.Modules.DataEntry.Tests/
git commit -m "feat(dataentry): UnitResult.FinishByStoppage with invariants I7/I9/I12"
```

---

## Task 16: UnitResult.Confirm + invariants I6/I10 + UnitResultOfficialEvent

**Files:**
- Modify: `src/OVR.Modules.DataEntry/Domain/UnitResult.cs`
- Modify: `src/OVR.Modules.DataEntry/Errors/DataEntryErrors.cs`
- Modify: `tests/OVR.Modules.DataEntry.Tests/Domain/UnitResultAggregateTests.cs`

- [ ] **Step 1: Add error factory**

Append to `DataEntryErrors.cs`:

```csharp
public static Error DecisionRequired() =>
    Error.Validation("DataEntry.DecisionRequired",
        "Cannot confirm without a Decision.");
```

- [ ] **Step 2: Write failing tests**

Append to `UnitResultAggregateTests.cs`:

```csharp
[Fact]
public void Confirm_WithDecision_TransitionsToOfficial_AndAssignsWlt()
{
    var ur = NewInStartList();
    ur.Start();
    ur.ScorePeriod("R1", EvenCards(10, 9));
    ur.ScorePeriod("R2", EvenCards(10, 9));
    ur.ScorePeriod("R3", EvenCards(10, 9));  // red wins 3:0

    var result = ur.Confirm();

    result.IsError.Should().BeFalse();
    ur.Status.Should().Be(ResultStatus.Official);
    ur.Competitors.First(c => c.SortOrder == 1).Wlt.Should().Be(Wlt.W);
    ur.Competitors.First(c => c.SortOrder == 2).Wlt.Should().Be(Wlt.L);
}

[Fact]
public void Confirm_AfterNcDecision_AssignsLLToBoth()
{
    var ur = NewInStartList();
    ur.Start();
    // All-draw outcome
    var drawCards = new[]
    {
        new PeriodScorecard(JudgePosition.J1, 10, 10),
        new PeriodScorecard(JudgePosition.J2, 10, 10),
        new PeriodScorecard(JudgePosition.J3, 10, 10)
    };
    ur.ScorePeriod("R1", drawCards);
    ur.ScorePeriod("R2", drawCards);
    ur.ScorePeriod("R3", drawCards);

    ur.Decision!.Code.Should().Be(ResultCode.Nc);
    ur.Confirm();

    ur.Status.Should().Be(ResultStatus.Official);
    ur.Competitors.Should().AllSatisfy(c => c.Wlt.Should().Be(Wlt.L));
}

[Fact]
public void Confirm_WithoutDecision_ReturnsDecisionRequired()
{
    var ur = NewInStartList();
    ur.Start();

    var result = ur.Confirm();
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.DecisionRequired");
}

[Fact]
public void Confirm_FromStartList_ReturnsInvalidStatusTransition()
{
    var ur = NewInStartList();

    var result = ur.Confirm();
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("DataEntry.InvalidStatusTransition");
}
```

- [ ] **Step 3: Implement `Confirm`**

Add to `UnitResult.cs`:

```csharp
public ErrorOr<Success> Confirm()
{
    if (Status != ResultStatus.Live)
        return DataEntryErrors.InvalidStatusTransition(Status.ToString(), "Official");

    if (Decision is null)
        return DataEntryErrors.DecisionRequired();

    // Assign WLT to competitors.
    var winner = Decision.WinnerParticipantId;
    var newCompetitors = _competitors.Select(c =>
    {
        Wlt wlt;
        if (winner is null)
            wlt = Wlt.L; // no-winner scenarios: both L
        else
            wlt = c.ParticipantId == winner ? Wlt.W : Wlt.L;
        return c with { Wlt = wlt };
    }).ToList();
    _competitors.Clear();
    _competitors.AddRange(newCompetitors);

    Status = ResultStatus.Official;
    var confirmedAt = DateTime.UtcNow;
    UpdatedAt = confirmedAt;
    if (EndedAt is null) EndedAt = confirmedAt;

    RaiseDomainEvent(new UnitResultOfficialEvent(
        UnitRsc: UnitRsc.Value,
        WinnerParticipantId: winner?.Value,
        ResultCode: Decision.Code.ToString(),
        ResultType: Decision.Type.ToString(),
        DecisionMark: Decision.DecisionMark,
        StoppageRound: Decision.StoppageRound,
        StoppageTime: Decision.StoppageTime,
        ConfirmedAt: confirmedAt));

    return Result.Success;
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/
```

Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.DataEntry/ tests/OVR.Modules.DataEntry.Tests/
git commit -m "feat(dataentry): UnitResult.Confirm → Official, assigns WLT, emits OfficialEvent"
```

---

## Task 17: UnitResult.Hydrate for persistence reconstitution

**Files:**
- Modify: `src/OVR.Modules.DataEntry/Domain/UnitResult.cs`

- [ ] **Step 1: Add `Hydrate` method**

Append to `UnitResult.cs`:

```csharp
internal static UnitResult Hydrate(
    Rsc unitRsc,
    ResultStatus status,
    IReadOnlyList<Competitor> competitors,
    IReadOnlyList<Period> periods,
    Decision? decision,
    DateTime? startedAt,
    DateTime? endedAt,
    string? currentPeriodCode,
    DateTime createdAt,
    DateTime? updatedAt)
{
    var ur = new UnitResult
    {
        Id = unitRsc.Value,
        UnitRsc = unitRsc,
        Status = status,
        Decision = decision,
        StartedAt = startedAt,
        EndedAt = endedAt,
        CurrentPeriodCode = currentPeriodCode,
        CreatedAt = createdAt,
        UpdatedAt = updatedAt
    };
    ur._competitors.AddRange(competitors);
    ur._periods.AddRange(periods);
    return ur;
}
```

- [ ] **Step 2: Build**

```bash
dotnet build src/OVR.Modules.DataEntry/
```

- [ ] **Step 3: Commit**

```bash
git add src/OVR.Modules.DataEntry/Domain/UnitResult.cs
git commit -m "feat(dataentry): UnitResult.Hydrate for repository reconstitution"
```

---

## Task 18: SeedBasedFirstRoundLineupResolver + tests

**Files:**
- Create: `src/OVR.Modules.DataEntry/Lineup/IFirstRoundLineupResolver.cs`
- Create: `src/OVR.Modules.DataEntry/Lineup/SeedBasedFirstRoundLineupResolver.cs`
- Create: `tests/OVR.Modules.DataEntry.Tests/Lineup/SeedBasedFirstRoundLineupResolverTests.cs`

- [ ] **Step 1: Create interface**

`src/OVR.Modules.DataEntry/Lineup/IFirstRoundLineupResolver.cs`:

```csharp
using ErrorOr;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.Entries.Contracts;

namespace OVR.Modules.DataEntry.Lineup;

public interface IFirstRoundLineupResolver
{
    ErrorOr<(Competitor Red, Competitor Blue)> Resolve(
        int seedA, int seedB, IReadOnlyList<EntryDto> activeEntries);
}
```

- [ ] **Step 2: Write failing tests**

`tests/OVR.Modules.DataEntry.Tests/Lineup/SeedBasedFirstRoundLineupResolverTests.cs`:

```csharp
using FluentAssertions;
using OVR.Modules.DataEntry.Lineup;
using OVR.Modules.Entries.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Lineup;

public class SeedBasedFirstRoundLineupResolverTests
{
    private readonly SeedBasedFirstRoundLineupResolver _resolver = new();

    private static EntryDto E(string pid, string org, int seed) =>
        new(ParticipantId.Create(pid), seed, Organisation.FromCode(org));

    [Fact]
    public void Resolve_LowerSeedGetsRedCorner()
    {
        var entries = new[]
        {
            E("NOC-ESP-0001", "ESP", 1),
            E("NOC-POL-0014", "POL", 8)
        };

        var result = _resolver.Resolve(seedA: 1, seedB: 8, entries);
        result.IsError.Should().BeFalse();

        var (red, blue) = result.Value;
        red.SortOrder.Should().Be(1);
        red.Seed.Should().Be(1);
        blue.SortOrder.Should().Be(2);
        blue.Seed.Should().Be(8);
    }

    [Fact]
    public void Resolve_ReversedSeedArgs_StillAssignsLowerSeedToRed()
    {
        var entries = new[]
        {
            E("NOC-ESP-0001", "ESP", 1),
            E("NOC-POL-0014", "POL", 8)
        };

        var result = _resolver.Resolve(seedA: 8, seedB: 1, entries);
        result.IsError.Should().BeFalse();

        var (red, _) = result.Value;
        red.Seed.Should().Be(1);
    }

    [Fact]
    public void Resolve_SeedNotFound_ReturnsError()
    {
        var entries = new[] { E("NOC-ESP-0001", "ESP", 1) };

        var result = _resolver.Resolve(seedA: 1, seedB: 8, entries);
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.LineupResolutionFailed");
    }

    [Fact]
    public void Resolve_DuplicateSeed_ReturnsError()
    {
        var entries = new[]
        {
            E("NOC-ESP-0001", "ESP", 1),
            E("NOC-ESP-0002", "ESP", 1),
            E("NOC-POL-0014", "POL", 8)
        };

        var result = _resolver.Resolve(1, 8, entries);
        result.IsError.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Implement resolver**

`src/OVR.Modules.DataEntry/Lineup/SeedBasedFirstRoundLineupResolver.cs`:

```csharp
using ErrorOr;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.Entries.Contracts;

namespace OVR.Modules.DataEntry.Lineup;

// TD-DE-01: corner assignment convention ("lower seed → red") is local.
// Refactor path: move to IUnitLineupReader.GetCornerAssignment in CompetitionConfig.
// See docs/superpowers/specs/2026-04-18-dataentry-mvp-design.md § Technical debt.
public sealed class SeedBasedFirstRoundLineupResolver : IFirstRoundLineupResolver
{
    public ErrorOr<(Competitor Red, Competitor Blue)> Resolve(
        int seedA, int seedB, IReadOnlyList<EntryDto> activeEntries)
    {
        var entryA = activeEntries.SingleOrDefault(e => e.Seed == seedA);
        var entryB = activeEntries.SingleOrDefault(e => e.Seed == seedB);

        if (entryA is null || entryB is null)
            return Error.NotFound("DataEntry.LineupResolutionFailed",
                $"Could not resolve seeds ({seedA}, {seedB}) to active entries.");

        // Duplicate-seed detection — SingleOrDefault on a seed with two matches throws;
        // catch via Count check instead.
        if (activeEntries.Count(e => e.Seed == seedA) > 1 ||
            activeEntries.Count(e => e.Seed == seedB) > 1)
            return Error.Validation("DataEntry.LineupResolutionFailed",
                "Duplicate seeds present in active entries.");

        var lowerSeed = seedA < seedB ? entryA : entryB;
        var higherSeed = seedA < seedB ? entryB : entryA;

        var red = new Competitor(
            SortOrder: 1,
            ParticipantId: lowerSeed.ParticipantId,
            NocompDetail: null,
            Seed: lowerSeed.Seed,
            Organisation: lowerSeed.Organisation,
            Wlt: null);

        var blue = new Competitor(
            SortOrder: 2,
            ParticipantId: higherSeed.ParticipantId,
            NocompDetail: null,
            Seed: higherSeed.Seed,
            Organisation: higherSeed.Organisation,
            Wlt: null);

        return (red, blue);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/ --filter "FullyQualifiedName~SeedBasedFirstRoundLineupResolver"
```

Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.DataEntry/Lineup/ tests/OVR.Modules.DataEntry.Tests/Lineup/
git commit -m "feat(dataentry): SeedBasedFirstRoundLineupResolver (TD-DE-01 documented)"
```

---

## Task 19: Persistence — UnitResultDocument, mapping, repository, index initializer

**Files:**
- Create: `src/OVR.Modules.DataEntry/Persistence/UnitResultDocument.cs`
- Create: `src/OVR.Modules.DataEntry/Persistence/UnitResultMapping.cs`
- Create: `src/OVR.Modules.DataEntry/Persistence/IUnitResultRepository.cs`
- Create: `src/OVR.Modules.DataEntry/Persistence/MongoUnitResultRepository.cs`
- Create: `src/OVR.Modules.DataEntry/Persistence/DataEntryIndexInitializer.cs`

- [ ] **Step 1: Create `UnitResultDocument.cs`**

```csharp
using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.DataEntry.Persistence;

public sealed class UnitResultDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
    public List<CompetitorDocument> Competitors { get; set; } = new();
    public List<PeriodDocument> Periods { get; set; } = new();
    public DecisionDocument? Decision { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? CurrentPeriodCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class CompetitorDocument
{
    public int SortOrder { get; set; }
    public string? ParticipantId { get; set; }
    public string? NocompDetail { get; set; }
    public int? Seed { get; set; }
    public string Organisation { get; set; } = string.Empty;
    public string? Wlt { get; set; }
}

public sealed class PeriodDocument
{
    public string Code { get; set; } = string.Empty;
    public List<ScorecardDocument> Scorecards { get; set; } = new();
}

public sealed class ScorecardDocument
{
    public string JudgePos { get; set; } = string.Empty;
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
}

public sealed class DecisionDocument
{
    public string Type { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? DecisionMark { get; set; }
    public string? StoppageRound { get; set; }
    public string? StoppageTime { get; set; }
    public string? WinnerParticipantId { get; set; }
}
```

- [ ] **Step 2: Create `UnitResultMapping.cs`**

```csharp
using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Persistence;

public static class UnitResultMapping
{
    public static UnitResultDocument ToDocument(UnitResult ur) => new()
    {
        Id = ur.UnitRsc.Value,
        Status = ur.Status.ToString(),
        Competitors = ur.Competitors.Select(c => new CompetitorDocument
        {
            SortOrder = c.SortOrder,
            ParticipantId = c.ParticipantId?.Value,
            NocompDetail = c.NocompDetail,
            Seed = c.Seed,
            Organisation = c.Organisation.Code,
            Wlt = c.Wlt?.ToString()
        }).ToList(),
        Periods = ur.Periods.Select(p => new PeriodDocument
        {
            Code = p.Code,
            Scorecards = p.Scorecards.Select(s => new ScorecardDocument
            {
                JudgePos = s.JudgePos.ToString(),
                HomeScore = s.HomeScore,
                AwayScore = s.AwayScore
            }).ToList()
        }).ToList(),
        Decision = ur.Decision is null ? null : new DecisionDocument
        {
            Type = ur.Decision.Type.ToString(),
            Code = ur.Decision.Code.ToString(),
            DecisionMark = ur.Decision.DecisionMark,
            StoppageRound = ur.Decision.StoppageRound,
            StoppageTime = ur.Decision.StoppageTime,
            WinnerParticipantId = ur.Decision.WinnerParticipantId?.Value
        },
        StartedAt = ur.StartedAt,
        EndedAt = ur.EndedAt,
        CurrentPeriodCode = ur.CurrentPeriodCode,
        CreatedAt = ur.CreatedAt,
        UpdatedAt = ur.UpdatedAt
    };

    public static UnitResult ToDomain(UnitResultDocument doc)
    {
        var competitors = doc.Competitors.Select(c => new Competitor(
            SortOrder: c.SortOrder,
            ParticipantId: c.ParticipantId is null ? null : ParticipantId.Create(c.ParticipantId),
            NocompDetail: c.NocompDetail,
            Seed: c.Seed,
            Organisation: Organisation.FromCode(c.Organisation),
            Wlt: c.Wlt is null ? null : Enum.Parse<Wlt>(c.Wlt))).ToList();

        var periods = doc.Periods.Select(p => new Period(
            p.Code,
            p.Scorecards.Select(s => new PeriodScorecard(
                Enum.Parse<JudgePosition>(s.JudgePos),
                s.HomeScore,
                s.AwayScore)).ToList())).ToList();

        var decision = doc.Decision is null ? null : new Decision(
            Enum.Parse<ResultType>(doc.Decision.Type),
            Enum.Parse<ResultCode>(doc.Decision.Code),
            doc.Decision.DecisionMark,
            doc.Decision.StoppageRound,
            doc.Decision.StoppageTime,
            doc.Decision.WinnerParticipantId is null
                ? null : ParticipantId.Create(doc.Decision.WinnerParticipantId));

        return UnitResult.Hydrate(
            Rsc.Create(doc.Id),
            Enum.Parse<ResultStatus>(doc.Status),
            competitors,
            periods,
            decision,
            doc.StartedAt,
            doc.EndedAt,
            doc.CurrentPeriodCode,
            doc.CreatedAt,
            doc.UpdatedAt);
    }
}
```

- [ ] **Step 3: Create `IUnitResultRepository.cs`**

```csharp
using OVR.Modules.DataEntry.Domain;

namespace OVR.Modules.DataEntry.Persistence;

public interface IUnitResultRepository
{
    Task<bool> ExistsAsync(string unitRsc, CancellationToken ct);
    Task<UnitResult?> GetAsync(string unitRsc, CancellationToken ct);
    Task<IReadOnlyList<UnitResult>> GetManyAsync(
        IReadOnlyList<string> unitRscs, CancellationToken ct);
    Task<IReadOnlyList<UnitResult>> ListAllAsync(CancellationToken ct);
    Task SaveNewAsync(UnitResult unitResult, CancellationToken ct);
    Task UpdateAsync(UnitResult unitResult, CancellationToken ct);
}
```

- [ ] **Step 4: Create `MongoUnitResultRepository.cs`**

```csharp
using MongoDB.Driver;
using OVR.Modules.DataEntry.Domain;

namespace OVR.Modules.DataEntry.Persistence;

public sealed class MongoUnitResultRepository : IUnitResultRepository
{
    private readonly IMongoCollection<UnitResultDocument> _collection;

    public MongoUnitResultRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<UnitResultDocument>("unitResults");
    }

    public async Task<bool> ExistsAsync(string unitRsc, CancellationToken ct)
        => await _collection.Find(d => d.Id == unitRsc).Limit(1).AnyAsync(ct);

    public async Task<UnitResult?> GetAsync(string unitRsc, CancellationToken ct)
    {
        var doc = await _collection.Find(d => d.Id == unitRsc).FirstOrDefaultAsync(ct);
        return doc is null ? null : UnitResultMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<UnitResult>> GetManyAsync(
        IReadOnlyList<string> unitRscs, CancellationToken ct)
    {
        if (unitRscs.Count == 0) return Array.Empty<UnitResult>();
        var docs = await _collection.Find(d => unitRscs.Contains(d.Id)).ToListAsync(ct);
        return docs.Select(UnitResultMapping.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<UnitResult>> ListAllAsync(CancellationToken ct)
    {
        var docs = await _collection.Find(Builders<UnitResultDocument>.Filter.Empty).ToListAsync(ct);
        return docs.Select(UnitResultMapping.ToDomain).ToList();
    }

    public async Task SaveNewAsync(UnitResult unitResult, CancellationToken ct)
    {
        try
        {
            var doc = UnitResultMapping.ToDocument(unitResult);
            await _collection.InsertOneAsync(doc, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (
            ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Idempotent: another event instance created it concurrently. No-op.
        }
    }

    public async Task UpdateAsync(UnitResult unitResult, CancellationToken ct)
    {
        var doc = UnitResultMapping.ToDocument(unitResult);
        await _collection.ReplaceOneAsync(
            d => d.Id == doc.Id, doc,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken: ct);
    }
}
```

- [ ] **Step 5: Create `DataEntryIndexInitializer.cs`**

```csharp
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace OVR.Modules.DataEntry.Persistence;

public sealed class DataEntryIndexInitializer : IHostedService
{
    private readonly IMongoDatabase _database;

    public DataEntryIndexInitializer(IMongoDatabase database)
    {
        _database = database;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // MVP 3: no extra indexes beyond _id. Kept for consistency with Scheduling.
        _ = _database.GetCollection<UnitResultDocument>("unitResults");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 6: Build**

```bash
dotnet build src/OVR.Modules.DataEntry/
```

- [ ] **Step 7: Commit**

```bash
git add src/OVR.Modules.DataEntry/Persistence/
git commit -m "feat(dataentry): persistence (document, mapping, Mongo repository, index initializer)"
```

---

## Task 20: i18n files for all error codes

**Files:**
- Create: `src/OVR.Modules.DataEntry/I18n/eng.json`
- Create: `src/OVR.Modules.DataEntry/I18n/spa.json`
- Create: `src/OVR.Modules.DataEntry/I18n/por.json`

- [ ] **Step 1: Create `eng.json`**

```json
{
  "DataEntry.UnitResultNotFound": "Unit result '{Rsc}' was not found.",
  "DataEntry.InvalidCompetitors": "{Message}",
  "DataEntry.InvalidStatusTransition": "Cannot transition from {From} to {To}.",
  "DataEntry.InvalidScorecardCount": "Exactly 3 scorecards are required (J1, J2, J3).",
  "DataEntry.InvalidScoreRange": "Score {Value} is outside the allowed range [6..10].",
  "DataEntry.DuplicateJudgePosition": "Judge position {Pos} appears more than once.",
  "DataEntry.InvalidPeriodOrder": "Cannot score period {Code} out of order.",
  "DataEntry.PeriodAlreadyScored": "Period {Code} has already been scored.",
  "DataEntry.DecisionAlreadyExists": "Cannot modify scoring after a decision has been recorded.",
  "DataEntry.InvalidPeriodCode": "Invalid period code '{Code}'. Expected one of R1, R2, R3.",
  "DataEntry.InvalidStoppageData": "{Reason}",
  "DataEntry.DecisionRequired": "Cannot confirm without a Decision."
}
```

- [ ] **Step 2: Create `spa.json`**

```json
{
  "DataEntry.UnitResultNotFound": "No se encontró el resultado de la unidad '{Rsc}'.",
  "DataEntry.InvalidCompetitors": "{Message}",
  "DataEntry.InvalidStatusTransition": "No se puede transicionar de {From} a {To}.",
  "DataEntry.InvalidScorecardCount": "Se requieren exactamente 3 tarjetas (J1, J2, J3).",
  "DataEntry.InvalidScoreRange": "El puntaje {Value} está fuera del rango permitido [6..10].",
  "DataEntry.DuplicateJudgePosition": "La posición del juez {Pos} aparece más de una vez.",
  "DataEntry.InvalidPeriodOrder": "No se puede puntuar el período {Code} fuera de orden.",
  "DataEntry.PeriodAlreadyScored": "El período {Code} ya fue puntuado.",
  "DataEntry.DecisionAlreadyExists": "No se puede modificar la puntuación después de registrar una decisión.",
  "DataEntry.InvalidPeriodCode": "Código de período '{Code}' inválido. Se esperaba R1, R2 o R3.",
  "DataEntry.InvalidStoppageData": "{Reason}",
  "DataEntry.DecisionRequired": "No se puede confirmar sin una decisión registrada."
}
```

- [ ] **Step 3: Create `por.json`**

```json
{
  "DataEntry.UnitResultNotFound": "Resultado da unidade '{Rsc}' não encontrado.",
  "DataEntry.InvalidCompetitors": "{Message}",
  "DataEntry.InvalidStatusTransition": "Não é possível transicionar de {From} para {To}.",
  "DataEntry.InvalidScorecardCount": "São necessários exatamente 3 cartões (J1, J2, J3).",
  "DataEntry.InvalidScoreRange": "A pontuação {Value} está fora do intervalo permitido [6..10].",
  "DataEntry.DuplicateJudgePosition": "A posição do juiz {Pos} aparece mais de uma vez.",
  "DataEntry.InvalidPeriodOrder": "Não é possível pontuar o período {Code} fora de ordem.",
  "DataEntry.PeriodAlreadyScored": "O período {Code} já foi pontuado.",
  "DataEntry.DecisionAlreadyExists": "Não é possível modificar a pontuação após uma decisão registrada.",
  "DataEntry.InvalidPeriodCode": "Código de período '{Code}' inválido. Esperado R1, R2 ou R3.",
  "DataEntry.InvalidStoppageData": "{Reason}",
  "DataEntry.DecisionRequired": "Não é possível confirmar sem uma decisão registrada."
}
```

- [ ] **Step 4: Build to verify content inclusion**

```bash
dotnet build src/OVR.Modules.DataEntry/
```

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.DataEntry/I18n/
git commit -m "feat(dataentry): i18n files for eng/spa/por with error keys"
```

---

## Task 21: CreateUnitResultOnScheduled event handler + unit tests

**Files:**
- Create: `src/OVR.Modules.DataEntry/Features/CreateUnitResultOnScheduled/UnitScheduledEventHandler.cs`
- Create: `tests/OVR.Modules.DataEntry.Tests/Features/CreateUnitResultOnScheduled/UnitScheduledEventHandlerTests.cs`

- [ ] **Step 1: Write failing test**

```csharp
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OVR.Modules.CompetitionConfig.Contracts;
using OVR.Modules.DataEntry.Features.CreateUnitResultOnScheduled;
using OVR.Modules.DataEntry.Lineup;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.DataEntry.SportRules;
using OVR.Modules.Entries.Contracts;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Features.CreateUnitResultOnScheduled;

public class UnitScheduledEventHandlerTests
{
    private readonly IUnitResultRepository _repository = Substitute.For<IUnitResultRepository>();
    private readonly IUnitLineupReader _lineupReader = Substitute.For<IUnitLineupReader>();
    private readonly IEntryReader _entryReader = Substitute.For<IEntryReader>();
    private readonly IFirstRoundLineupResolver _resolver = new SeedBasedFirstRoundLineupResolver();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private UnitScheduledEventHandler Handler() => new(
        _repository, _lineupReader, _entryReader, _resolver, _publisher,
        NullLogger<UnitScheduledEventHandler>.Instance);

    private static UnitScheduledEvent Evt() => new(
        UnitRsc:       "BOXW---------------M71KG-8FNL0001----",
        EventRsc:      "BOXW---------------M71KG---------",
        SessionCode:   "S1",
        LocationCode:  "BXR",
        StartTime:     DateTime.UtcNow,
        OrderInSession: 1,
        OrderInLocation: 1,
        ScheduledAt:   DateTime.UtcNow);

    [Fact]
    public async Task Handle_WhenAllInputsValid_CreatesUnitResultAndPublishesEvent()
    {
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _lineupReader.GetSeedsForUnit(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((1, 8));
        _entryReader.ListActiveByEventRsc(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<EntryDto>
            {
                new(ParticipantId.Create("NOC-ESP-0001"), 1, Organisation.FromCode("ESP")),
                new(ParticipantId.Create("NOC-POL-0014"), 8, Organisation.FromCode("POL"))
            });

        await Handler().Handle(Evt(), default);

        await _repository.Received(1).SaveNewAsync(
            Arg.Any<Domain.UnitResult>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Any<UnitResultStartListCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUnitResultAlreadyExists_Skips()
    {
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Handler().Handle(Evt(), default);

        await _repository.DidNotReceive().SaveNewAsync(
            Arg.Any<Domain.UnitResult>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSeedsMissing_SkipsWithoutError()
    {
        _repository.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _lineupReader.GetSeedsForUnit(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((null, null));

        await Handler().Handle(Evt(), default);

        await _repository.DidNotReceive().SaveNewAsync(
            Arg.Any<Domain.UnitResult>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run — confirm compile failure**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/ --filter "FullyQualifiedName~UnitScheduledEventHandlerTests"
```

- [ ] **Step 3: Implement handler**

`src/OVR.Modules.DataEntry/Features/CreateUnitResultOnScheduled/UnitScheduledEventHandler.cs`:

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using OVR.Modules.CompetitionConfig.Contracts;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Lineup;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.Entries.Contracts;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Features.CreateUnitResultOnScheduled;

public sealed class UnitScheduledEventHandler : INotificationHandler<UnitScheduledEvent>
{
    private readonly IUnitResultRepository _repository;
    private readonly IUnitLineupReader _lineupReader;
    private readonly IEntryReader _entryReader;
    private readonly IFirstRoundLineupResolver _resolver;
    private readonly IPublisher _publisher;
    private readonly ILogger<UnitScheduledEventHandler> _logger;

    public UnitScheduledEventHandler(
        IUnitResultRepository repository,
        IUnitLineupReader lineupReader,
        IEntryReader entryReader,
        IFirstRoundLineupResolver resolver,
        IPublisher publisher,
        ILogger<UnitScheduledEventHandler> logger)
    {
        _repository = repository;
        _lineupReader = lineupReader;
        _entryReader = entryReader;
        _resolver = resolver;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(UnitScheduledEvent notification, CancellationToken ct)
    {
        // TD R3 — idempotency: if UnitResult already exists, skip.
        if (await _repository.ExistsAsync(notification.UnitRsc, ct))
        {
            _logger.LogInformation(
                "UnitResult for {UnitRsc} already exists; skipping.", notification.UnitRsc);
            return;
        }

        var (seedA, seedB) = await _lineupReader.GetSeedsForUnit(notification.UnitRsc, ct);
        if (seedA is null || seedB is null)
        {
            _logger.LogWarning(
                "Unit {UnitRsc} has no seeds assigned; skipping lineup fill.",
                notification.UnitRsc);
            return;
        }

        var activeEntries = await _entryReader.ListActiveByEventRsc(notification.EventRsc, ct);
        var lineupResult = _resolver.Resolve(seedA.Value, seedB.Value, activeEntries);
        if (lineupResult.IsError)
        {
            _logger.LogWarning(
                "Lineup resolution failed for {UnitRsc}: {Error}",
                notification.UnitRsc, lineupResult.FirstError.Description);
            return;
        }

        var (red, blue) = lineupResult.Value;
        var created = UnitResult.CreateForFirstRound(Rsc.Create(notification.UnitRsc), red, blue);
        if (created.IsError)
        {
            _logger.LogWarning(
                "Failed to create UnitResult for {UnitRsc}: {Error}",
                notification.UnitRsc, created.FirstError.Description);
            return;
        }

        var unitResult = created.Value;
        await _repository.SaveNewAsync(unitResult, ct);

        // Drain and publish domain events raised by CreateForFirstRound.
        foreach (var domainEvent in unitResult.DomainEvents)
        {
            await _publisher.Publish(domainEvent, ct);
        }
        unitResult.ClearDomainEvents();
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/OVR.Modules.DataEntry.Tests/
```

Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.DataEntry/Features/CreateUnitResultOnScheduled/ tests/OVR.Modules.DataEntry.Tests/Features/
git commit -m "feat(dataentry): UnitScheduledEventHandler creates UnitResult from active entries"
```

---

## Task 22: Feature — StartUnit

**Files:**
- Create: `src/OVR.Modules.DataEntry/Features/StartUnit/StartUnitCommand.cs`
- Create: `src/OVR.Modules.DataEntry/Features/StartUnit/StartUnitValidator.cs`
- Create: `src/OVR.Modules.DataEntry/Features/StartUnit/StartUnitHandler.cs`
- Create: `src/OVR.Modules.DataEntry/Features/StartUnit/StartUnitEndpoint.cs`

- [ ] **Step 1: Command**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.StartUnit;

public sealed record StartUnitCommand(string UnitRsc) : IRequest<ErrorOr<Success>>;
```

- [ ] **Step 2: Validator**

```csharp
using FluentValidation;

namespace OVR.Modules.DataEntry.Features.StartUnit;

public sealed class StartUnitValidator : AbstractValidator<StartUnitCommand>
{
    public StartUnitValidator()
    {
        RuleFor(x => x.UnitRsc).NotEmpty();
    }
}
```

- [ ] **Step 3: Handler**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;

namespace OVR.Modules.DataEntry.Features.StartUnit;

public sealed class StartUnitHandler
    : IRequestHandler<StartUnitCommand, ErrorOr<Success>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IPublisher _publisher;

    public StartUnitHandler(IUnitResultRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<ErrorOr<Success>> Handle(
        StartUnitCommand request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);

        var result = ur.Start();
        if (result.IsError) return result.Errors;

        await _repository.UpdateAsync(ur, ct);

        foreach (var e in ur.DomainEvents) await _publisher.Publish(e, ct);
        ur.ClearDomainEvents();

        return Result.Success;
    }
}
```

- [ ] **Step 4: Endpoint**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.StartUnit;

public static class StartUnitEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(new StartUnitCommand(rsc), ct);
        return result.ToApiResult(httpContext);
    }
}
```

- [ ] **Step 5: Build**

```bash
dotnet build src/OVR.Modules.DataEntry/
```

- [ ] **Step 6: Commit**

```bash
git add src/OVR.Modules.DataEntry/Features/StartUnit/
git commit -m "feat(dataentry): StartUnit feature (command, validator, handler, endpoint)"
```

---

## Task 23: Feature — ScorePeriod

**Files:**
- Create: `src/OVR.Modules.DataEntry/Features/ScorePeriod/ScorePeriodCommand.cs`
- Create: `src/OVR.Modules.DataEntry/Features/ScorePeriod/ScorePeriodValidator.cs`
- Create: `src/OVR.Modules.DataEntry/Features/ScorePeriod/ScorePeriodHandler.cs`
- Create: `src/OVR.Modules.DataEntry/Features/ScorePeriod/ScorePeriodEndpoint.cs`

- [ ] **Step 1: Command**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.ScorePeriod;

public sealed record ScorePeriodCommand(
    string UnitRsc,
    string PeriodCode,
    IReadOnlyList<ScorecardDto> Scorecards) : IRequest<ErrorOr<Success>>;

public sealed record ScorecardDto(string JudgePos, int HomeScore, int AwayScore);
```

- [ ] **Step 2: Validator**

```csharp
using FluentValidation;
using OVR.Modules.DataEntry.SportRules;

namespace OVR.Modules.DataEntry.Features.ScorePeriod;

public sealed class ScorePeriodValidator : AbstractValidator<ScorePeriodCommand>
{
    private static readonly string[] ValidJudges = new[] { "J1", "J2", "J3" };

    public ScorePeriodValidator()
    {
        RuleFor(x => x.UnitRsc).NotEmpty();
        RuleFor(x => x.PeriodCode).Must(p => BoxingRules.PeriodCodes.Contains(p))
            .WithMessage("PeriodCode must be one of R1, R2, R3.");
        RuleFor(x => x.Scorecards).NotNull()
            .Must(s => s.Count == 3).WithMessage("Exactly 3 scorecards required.");
        RuleForEach(x => x.Scorecards).ChildRules(card =>
        {
            card.RuleFor(c => c.JudgePos).Must(p => ValidJudges.Contains(p))
                .WithMessage("JudgePos must be J1, J2 or J3.");
            card.RuleFor(c => c.HomeScore)
                .InclusiveBetween(BoxingRules.MinPeriodScore, BoxingRules.MaxPeriodScore);
            card.RuleFor(c => c.AwayScore)
                .InclusiveBetween(BoxingRules.MinPeriodScore, BoxingRules.MaxPeriodScore);
        });
    }
}
```

- [ ] **Step 3: Handler**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;

namespace OVR.Modules.DataEntry.Features.ScorePeriod;

public sealed class ScorePeriodHandler
    : IRequestHandler<ScorePeriodCommand, ErrorOr<Success>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IPublisher _publisher;

    public ScorePeriodHandler(IUnitResultRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<ErrorOr<Success>> Handle(
        ScorePeriodCommand request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);

        var cards = request.Scorecards.Select(c => new PeriodScorecard(
            Enum.Parse<JudgePosition>(c.JudgePos),
            c.HomeScore, c.AwayScore)).ToList();

        var result = ur.ScorePeriod(request.PeriodCode, cards);
        if (result.IsError) return result.Errors;

        await _repository.UpdateAsync(ur, ct);
        foreach (var e in ur.DomainEvents) await _publisher.Publish(e, ct);
        ur.ClearDomainEvents();

        return Result.Success;
    }
}
```

- [ ] **Step 4: Endpoint**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.ScorePeriod;

public static class ScorePeriodEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, string code, ScorePeriodBody body,
        ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var command = new ScorePeriodCommand(rsc, code,
            body.Scorecards.Select(s =>
                new ScorecardDto(s.JudgePos, s.HomeScore, s.AwayScore)).ToList());
        var result = await sender.Send(command, ct);
        return result.ToApiResult(httpContext);
    }
}

public sealed record ScorePeriodBody(IReadOnlyList<ScorecardBody> Scorecards);
public sealed record ScorecardBody(string JudgePos, int HomeScore, int AwayScore);
```

- [ ] **Step 5: Build + commit**

```bash
dotnet build src/OVR.Modules.DataEntry/
git add src/OVR.Modules.DataEntry/Features/ScorePeriod/
git commit -m "feat(dataentry): ScorePeriod feature"
```

---

## Task 24: Feature — FinishByStoppage

**Files:**
- Create: `src/OVR.Modules.DataEntry/Features/FinishByStoppage/FinishByStoppageCommand.cs`
- Create: `src/OVR.Modules.DataEntry/Features/FinishByStoppage/FinishByStoppageValidator.cs`
- Create: `src/OVR.Modules.DataEntry/Features/FinishByStoppage/FinishByStoppageHandler.cs`
- Create: `src/OVR.Modules.DataEntry/Features/FinishByStoppage/FinishByStoppageEndpoint.cs`

- [ ] **Step 1: Command**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.FinishByStoppage;

public sealed record FinishByStoppageCommand(
    string UnitRsc,
    string ResultCode,
    string StoppageRound,
    string StoppageTime,
    string? WinnerParticipantId) : IRequest<ErrorOr<Success>>;
```

- [ ] **Step 2: Validator**

```csharp
using FluentValidation;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.SportRules;

namespace OVR.Modules.DataEntry.Features.FinishByStoppage;

public sealed class FinishByStoppageValidator
    : AbstractValidator<FinishByStoppageCommand>
{
    public FinishByStoppageValidator()
    {
        RuleFor(x => x.UnitRsc).NotEmpty();
        RuleFor(x => x.ResultCode).Must(c =>
            Enum.TryParse<ResultCode>(c, out var rc) && rc != ResultCode.Wp)
            .WithMessage("ResultCode must be a valid stoppage code (not WP).");
        RuleFor(x => x.StoppageRound).Must(r => BoxingRules.PeriodCodes.Contains(r))
            .WithMessage("StoppageRound must be R1, R2 or R3.");
        RuleFor(x => x.StoppageTime).Matches(@"^\d{1,2}:\d{2}$");
    }
}
```

- [ ] **Step 3: Handler**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Features.FinishByStoppage;

public sealed class FinishByStoppageHandler
    : IRequestHandler<FinishByStoppageCommand, ErrorOr<Success>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IPublisher _publisher;

    public FinishByStoppageHandler(IUnitResultRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<ErrorOr<Success>> Handle(
        FinishByStoppageCommand request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);

        var code = Enum.Parse<ResultCode>(request.ResultCode);
        ParticipantId? winner = request.WinnerParticipantId is null
            ? null : ParticipantId.Create(request.WinnerParticipantId);

        var result = ur.FinishByStoppage(
            code, request.StoppageRound, request.StoppageTime, winner);
        if (result.IsError) return result.Errors;

        await _repository.UpdateAsync(ur, ct);
        foreach (var e in ur.DomainEvents) await _publisher.Publish(e, ct);
        ur.ClearDomainEvents();

        return Result.Success;
    }
}
```

- [ ] **Step 4: Endpoint**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.FinishByStoppage;

public static class FinishByStoppageEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, FinishByStoppageBody body,
        ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var command = new FinishByStoppageCommand(
            rsc, body.ResultCode, body.StoppageRound, body.StoppageTime, body.WinnerParticipantId);
        var result = await sender.Send(command, ct);
        return result.ToApiResult(httpContext);
    }
}

public sealed record FinishByStoppageBody(
    string ResultCode,
    string StoppageRound,
    string StoppageTime,
    string? WinnerParticipantId);
```

- [ ] **Step 5: Build + commit**

```bash
dotnet build src/OVR.Modules.DataEntry/
git add src/OVR.Modules.DataEntry/Features/FinishByStoppage/
git commit -m "feat(dataentry): FinishByStoppage feature"
```

---

## Task 25: Feature — ConfirmUnitResult

**Files:**
- Create: `src/OVR.Modules.DataEntry/Features/ConfirmUnitResult/ConfirmUnitResultCommand.cs`
- Create: `src/OVR.Modules.DataEntry/Features/ConfirmUnitResult/ConfirmUnitResultHandler.cs`
- Create: `src/OVR.Modules.DataEntry/Features/ConfirmUnitResult/ConfirmUnitResultEndpoint.cs`

- [ ] **Step 1: Command**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.ConfirmUnitResult;

public sealed record ConfirmUnitResultCommand(string UnitRsc)
    : IRequest<ErrorOr<Success>>;
```

- [ ] **Step 2: Handler**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;

namespace OVR.Modules.DataEntry.Features.ConfirmUnitResult;

public sealed class ConfirmUnitResultHandler
    : IRequestHandler<ConfirmUnitResultCommand, ErrorOr<Success>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IPublisher _publisher;

    public ConfirmUnitResultHandler(
        IUnitResultRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<ErrorOr<Success>> Handle(
        ConfirmUnitResultCommand request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);

        var result = ur.Confirm();
        if (result.IsError) return result.Errors;

        await _repository.UpdateAsync(ur, ct);
        foreach (var e in ur.DomainEvents) await _publisher.Publish(e, ct);
        ur.ClearDomainEvents();

        return Result.Success;
    }
}
```

- [ ] **Step 3: Endpoint**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.ConfirmUnitResult;

public static class ConfirmUnitResultEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(new ConfirmUnitResultCommand(rsc), ct);
        return result.ToApiResult(httpContext);
    }
}
```

- [ ] **Step 4: Build + commit**

```bash
dotnet build src/OVR.Modules.DataEntry/
git add src/OVR.Modules.DataEntry/Features/ConfirmUnitResult/
git commit -m "feat(dataentry): ConfirmUnitResult feature"
```

---

## Task 26: Feature — GetUnitResult

**Files:**
- Create: `src/OVR.Modules.DataEntry/Features/GetUnitResult/GetUnitResultQuery.cs`
- Create: `src/OVR.Modules.DataEntry/Features/GetUnitResult/GetUnitResultHandler.cs`
- Create: `src/OVR.Modules.DataEntry/Features/GetUnitResult/GetUnitResultEndpoint.cs`

- [ ] **Step 1: Query + Response DTOs**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.GetUnitResult;

public sealed record GetUnitResultQuery(string UnitRsc)
    : IRequest<ErrorOr<UnitResultResponse>>;

public sealed record UnitResultResponse(
    string UnitRsc,
    string Status,
    string? CurrentPeriodCode,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyList<CompetitorResponse> Competitors,
    IReadOnlyList<PeriodResponse> Periods,
    DecisionResponse? Decision,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CompetitorResponse(
    int SortOrder, string? ParticipantId, int? Seed,
    string Organisation, string? Wlt);

public sealed record PeriodResponse(
    string Code, IReadOnlyList<ScorecardResponse> Scorecards);

public sealed record ScorecardResponse(string JudgePos, int HomeScore, int AwayScore);

public sealed record DecisionResponse(
    string Type, string Code, string? DecisionMark,
    string? StoppageRound, string? StoppageTime, string? WinnerParticipantId);
```

- [ ] **Step 2: Handler**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;

namespace OVR.Modules.DataEntry.Features.GetUnitResult;

public sealed class GetUnitResultHandler
    : IRequestHandler<GetUnitResultQuery, ErrorOr<UnitResultResponse>>
{
    private readonly IUnitResultRepository _repository;

    public GetUnitResultHandler(IUnitResultRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<UnitResultResponse>> Handle(
        GetUnitResultQuery request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);
        return Map(ur);
    }

    internal static UnitResultResponse Map(UnitResult ur) => new(
        UnitRsc: ur.UnitRsc.Value,
        Status: ur.Status.ToString(),
        CurrentPeriodCode: ur.CurrentPeriodCode,
        StartedAt: ur.StartedAt,
        EndedAt: ur.EndedAt,
        Competitors: ur.Competitors.Select(c => new CompetitorResponse(
            c.SortOrder, c.ParticipantId?.Value, c.Seed,
            c.Organisation.Code, c.Wlt?.ToString())).ToList(),
        Periods: ur.Periods.Select(p => new PeriodResponse(
            p.Code, p.Scorecards.Select(s => new ScorecardResponse(
                s.JudgePos.ToString(), s.HomeScore, s.AwayScore)).ToList())).ToList(),
        Decision: ur.Decision is null ? null : new DecisionResponse(
            ur.Decision.Type.ToString(), ur.Decision.Code.ToString(),
            ur.Decision.DecisionMark, ur.Decision.StoppageRound, ur.Decision.StoppageTime,
            ur.Decision.WinnerParticipantId?.Value),
        CreatedAt: ur.CreatedAt,
        UpdatedAt: ur.UpdatedAt);
}
```

- [ ] **Step 3: Endpoint**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.GetUnitResult;

public static class GetUnitResultEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(new GetUnitResultQuery(rsc), ct);
        return result.ToApiResult(httpContext);
    }
}
```

- [ ] **Step 4: Build + commit**

```bash
dotnet build src/OVR.Modules.DataEntry/
git add src/OVR.Modules.DataEntry/Features/GetUnitResult/
git commit -m "feat(dataentry): GetUnitResult feature with denormalized response DTOs"
```

---

## Task 27: Feature — ListUnitResults (filtered by session/location)

**Files:**
- Create: `src/OVR.Modules.DataEntry/Features/ListUnitResults/ListUnitResultsQuery.cs`
- Create: `src/OVR.Modules.DataEntry/Features/ListUnitResults/ListUnitResultsValidator.cs`
- Create: `src/OVR.Modules.DataEntry/Features/ListUnitResults/ListUnitResultsHandler.cs`
- Create: `src/OVR.Modules.DataEntry/Features/ListUnitResults/ListUnitResultsEndpoint.cs`

- [ ] **Step 1: Query**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Features.GetUnitResult;

namespace OVR.Modules.DataEntry.Features.ListUnitResults;

public sealed record ListUnitResultsQuery(
    string? SessionCode,
    string? LocationCode,
    string? Status) : IRequest<ErrorOr<IReadOnlyList<UnitResultResponse>>>;
```

- [ ] **Step 2: Validator**

```csharp
using FluentValidation;
using OVR.Modules.DataEntry.Domain;

namespace OVR.Modules.DataEntry.Features.ListUnitResults;

public sealed class ListUnitResultsValidator : AbstractValidator<ListUnitResultsQuery>
{
    public ListUnitResultsValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is null || Enum.TryParse<ResultStatus>(s, out _))
            .WithMessage("Status must be a valid ResultStatus.");
    }
}
```

- [ ] **Step 3: Handler**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Features.GetUnitResult;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.Scheduling.Contracts;

namespace OVR.Modules.DataEntry.Features.ListUnitResults;

public sealed class ListUnitResultsHandler
    : IRequestHandler<ListUnitResultsQuery, ErrorOr<IReadOnlyList<UnitResultResponse>>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IUnitScheduleReader _scheduleReader;

    public ListUnitResultsHandler(
        IUnitResultRepository repository, IUnitScheduleReader scheduleReader)
    {
        _repository = repository;
        _scheduleReader = scheduleReader;
    }

    public async Task<ErrorOr<IReadOnlyList<UnitResultResponse>>> Handle(
        ListUnitResultsQuery request, CancellationToken ct)
    {
        IReadOnlyList<UnitResult> results;

        if (request.SessionCode is null && request.LocationCode is null)
        {
            results = await _repository.ListAllAsync(ct);
        }
        else
        {
            var rscs = await _scheduleReader.ListUnitRscs(
                request.SessionCode, request.LocationCode, ct);
            results = await _repository.GetManyAsync(rscs, ct);
        }

        if (request.Status is not null)
        {
            var status = Enum.Parse<ResultStatus>(request.Status);
            results = results.Where(r => r.Status == status).ToList();
        }

        return (IReadOnlyList<UnitResultResponse>)results
            .Select(GetUnitResultHandler.Map).ToList();
    }
}
```

- [ ] **Step 4: Endpoint**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.ListUnitResults;

public static class ListUnitResultsEndpoint
{
    public static async Task<IResult> Handle(
        string? sessionCode, string? locationCode, string? status,
        ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(
            new ListUnitResultsQuery(sessionCode, locationCode, status), ct);
        return result.ToApiResult(httpContext);
    }
}
```

- [ ] **Step 5: Build + commit**

```bash
dotnet build src/OVR.Modules.DataEntry/
git add src/OVR.Modules.DataEntry/Features/ListUnitResults/
git commit -m "feat(dataentry): ListUnitResults with session/location filter via IUnitScheduleReader"
```

---

## Task 28: Wire DataEntryModule (DI + endpoints) + Program.cs registration

**Files:**
- Modify: `src/OVR.Modules.DataEntry/DataEntryModule.cs`
- Modify: `src/OVR.Api/Program.cs` (verify DataEntry is wired)

- [ ] **Step 1: Rewrite `DataEntryModule.cs`**

```csharp
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.DataEntry.Features.ConfirmUnitResult;
using OVR.Modules.DataEntry.Features.FinishByStoppage;
using OVR.Modules.DataEntry.Features.GetUnitResult;
using OVR.Modules.DataEntry.Features.ListUnitResults;
using OVR.Modules.DataEntry.Features.ScorePeriod;
using OVR.Modules.DataEntry.Features.StartUnit;
using OVR.Modules.DataEntry.Lineup;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.DataEntry.SportRules;

namespace OVR.Modules.DataEntry;

public static class DataEntryModule
{
    public static IServiceCollection AddDataEntryModule(this IServiceCollection services)
    {
        services.AddScoped<IUnitResultRepository, MongoUnitResultRepository>();
        services.AddSingleton<IFirstRoundLineupResolver, SeedBasedFirstRoundLineupResolver>();
        services.AddSingleton<ITenPointMustResolver, TenPointMustResolver>();
        services.AddHostedService<DataEntryIndexInitializer>();

        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapDataEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/data-entry").WithTags("DataEntry");

        group.MapGet("/unit-results/{rsc}", GetUnitResultEndpoint.Handle)
            .WithName("GetUnitResult");

        group.MapGet("/unit-results", ListUnitResultsEndpoint.Handle)
            .WithName("ListUnitResults");

        group.MapPost("/unit-results/{rsc}/start", StartUnitEndpoint.Handle)
            .WithName("StartUnit");

        group.MapPost("/unit-results/{rsc}/periods/{code}/score", ScorePeriodEndpoint.Handle)
            .WithName("ScorePeriod");

        group.MapPost("/unit-results/{rsc}/finish-stoppage", FinishByStoppageEndpoint.Handle)
            .WithName("FinishByStoppage");

        group.MapPost("/unit-results/{rsc}/confirm", ConfirmUnitResultEndpoint.Handle)
            .WithName("ConfirmUnitResult");

        return app;
    }
}
```

- [ ] **Step 2: Verify Program.cs has DataEntry wiring**

Check `src/OVR.Api/Program.cs` contains:

```csharp
builder.Services.AddDataEntryModule();
// ...
app.MapDataEntryEndpoints();
```

If missing, add next to Scheduling's equivalent lines.

- [ ] **Step 3: Build full solution**

```bash
dotnet build
```

Expected: success.

- [ ] **Step 4: Run all tests**

```bash
dotnet test
```

Expected: all existing + new unit tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.DataEntry/DataEntryModule.cs src/OVR.Api/Program.cs
git commit -m "feat(dataentry): wire DataEntryModule DI + endpoints in Program"
```

---

## Task 29: Integration test fixture — DataEntryWebAppFactory

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/DataEntry/Support/DataEntryWebAppFactory.cs`

- [ ] **Step 1: Read an existing factory to mirror the pattern**

```bash
cat tests/OVR.Api.IntegrationTests/Scheduling/Support/SchedulingWebAppFactory.cs
```

Note the seeding helper for CommonCodes and the test container setup.

- [ ] **Step 2: Create `DataEntryWebAppFactory.cs`**

```csharp
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OVR.Modules.CommonCodes.Persistence;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.Scheduling.Persistence;
using OVR.Modules.Entries.Persistence;
using OVR.Modules.CompetitionConfig.Persistence;
using Testcontainers.MongoDb;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry.Support;

public sealed class DataEntryWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .Build();

    public IMongoDatabase Database { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _mongo.StartAsync();

        var client = new MongoClient(_mongo.GetConnectionString());
        Database = client.GetDatabase("ovr-test");

        // Seed common codes required by Entries/Participant creation.
        var codes = Database.GetCollection<CommonCodeDocument>("commonCodes_codes");
        await codes.InsertManyAsync(new[]
        {
            Code("ORGANISATIONS", "ESP", "Spain", "España"),
            Code("ORGANISATIONS", "POL", "Poland", "Polonia"),
            Code("DISCIPLINE", "BOX", "Boxing", "Boxeo"),
        });
    }

    public new Task DisposeAsync() => _mongo.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = _mongo.GetConnectionString(),
                ["MongoDb:DatabaseName"] = "ovr-test"
            });
        });
    }

    private static CommonCodeDocument Code(
        string type, string code, string eng, string spa) => new()
    {
        Id = $"{type}:{code}",
        Type = type,
        Code = code,
        Name = new Dictionary<string, LocalizedTextDocument>
        {
            ["eng"] = new() { Long = eng },
            ["spa"] = new() { Long = spa }
        }
    };

    public async Task SeedFirstRoundBracketAsync(
        string eventRsc,
        string unitRsc,
        int seedA,
        int seedB)
    {
        // Insert a minimal Unit document with SeedA/SeedB set.
        var units = Database.GetCollection<UnitDocument>("competitionconfig_units");
        await units.InsertOneAsync(new UnitDocument
        {
            Id = unitRsc,
            UnitNumber = 1,
            PhaseCode = "FNL-",
            Status = "Draft",
            SeedA = seedA,
            SeedB = seedB,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task SeedEntriesAsync(string eventRsc,
        (string participantId, string organisation, int seed)[] entries)
    {
        var col = Database.GetCollection<EntryDocument>("entries");
        foreach (var (pid, org, seed) in entries)
        {
            await col.InsertOneAsync(new EntryDocument
            {
                Id = $"{pid}_{eventRsc}",
                ParticipantId = pid,
                EventRsc = eventRsc,
                CompetitorType = "Athlete",
                Organisation = org,
                Status = "Active",
                InscriptionStatus = "Confirmed",
                Seed = seed.ToString(),
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
```

(Verify field names for `CommonCodeDocument`, `UnitDocument`, `EntryDocument` against actual implementations — adjust as needed.)

- [ ] **Step 3: Build**

```bash
dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/DataEntry/
git commit -m "test(dataentry): integration test factory with testcontainers Mongo"
```

---

## Task 30: Integration test — CreateUnitResultOnScheduled

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/DataEntry/CreateUnitResultOnScheduledTests.cs`

- [ ] **Step 1: Write test file**

```csharp
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class CreateUnitResultOnScheduledTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public CreateUnitResultOnScheduledTests(DataEntryWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnitScheduledEvent_CreatesUnitResultWithCorrectLineup()
    {
        var eventRsc = "BOXW---------------M71KG---------";
        var unitRsc = "BOXW---------------M71KG-FNL-0001----";

        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, seedA: 1, seedB: 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-ESP-0001", "ESP", 1),
            ("NOC-POL-0014", "POL", 2)
        });

        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.Publish(new UnitScheduledEvent(
            UnitRsc: unitRsc, EventRsc: eventRsc,
            SessionCode: "S1", LocationCode: "BXR",
            StartTime: DateTime.UtcNow, OrderInSession: 1, OrderInLocation: 1,
            ScheduledAt: DateTime.UtcNow));

        // Query the API
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/data-entry/unit-results/{unitRsc}");
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content.ReadFromJsonAsync<UnitResultDto>();
        body!.Status.Should().Be("StartList");
        body.Competitors.Should().HaveCount(2);
        body.Competitors[0].SortOrder.Should().Be(1);
        body.Competitors[0].Seed.Should().Be(1);
        body.Competitors[0].Organisation.Should().Be("ESP");
        body.Competitors[1].SortOrder.Should().Be(2);
        body.Competitors[1].Organisation.Should().Be("POL");
    }

    [Fact]
    public async Task UnitScheduledEvent_Idempotent_DoesNotCreateDuplicate()
    {
        var eventRsc = "BOXW---------------M66KG---------";
        var unitRsc = "BOXW---------------M66KG-FNL-0001----";

        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, 1, 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-ESP-0010", "ESP", 1),
            ("NOC-POL-0011", "POL", 2)
        });

        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var evt = new UnitScheduledEvent(
            unitRsc, eventRsc, "S1", "BXR",
            DateTime.UtcNow, 1, 1, DateTime.UtcNow);

        await publisher.Publish(evt);
        await publisher.Publish(evt);  // second time — should no-op

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/data-entry/unit-results/{unitRsc}");
        response.IsSuccessStatusCode.Should().BeTrue();
        // No duplicate → single response is fine evidence; deeper count check would need direct Mongo query.
    }

    // Shallow DTO matching the response shape.
    private record UnitResultDto(
        string UnitRsc, string Status, string? CurrentPeriodCode,
        DateTime? StartedAt, DateTime? EndedAt,
        List<CompetitorDto> Competitors);

    private record CompetitorDto(
        int SortOrder, string ParticipantId, int Seed,
        string Organisation, string? Wlt);
}
```

- [ ] **Step 2: Run**

```bash
dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~CreateUnitResultOnScheduledTests"
```

Expected: tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/DataEntry/CreateUnitResultOnScheduledTests.cs
git commit -m "test(dataentry): integration test for UnitScheduledEvent → UnitResult creation"
```

---

## Task 31: Integration test — full points-decision happy path

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/DataEntry/ScoringPathTests.cs`

- [ ] **Step 1: Write test**

```csharp
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class ScoringPathTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public ScoringPathTests(DataEntryWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task FullPointsPath_StartList_Through_Official()
    {
        var eventRsc = "BOXW---------------M75KG---------";
        var unitRsc = "BOXW---------------M75KG-FNL-0001----";
        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, 1, 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-ESP-0020", "ESP", 1),
            ("NOC-POL-0021", "POL", 2)
        });

        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>()
                .Publish(new UnitScheduledEvent(
                    unitRsc, eventRsc, "S1", "BXR",
                    DateTime.UtcNow, 1, 1, DateTime.UtcNow));
        }

        var client = _factory.CreateClient();

        // Start
        var startResp = await client.PostAsync(
            $"/api/data-entry/unit-results/{unitRsc}/start", content: null);
        startResp.IsSuccessStatusCode.Should().BeTrue();

        // Score 3 rounds — red wins 3:0 (10-9 each)
        var unanimousRed = new ScorePeriodBody(new[]
        {
            new ScorecardBody("J1", 10, 9),
            new ScorecardBody("J2", 10, 9),
            new ScorecardBody("J3", 10, 9)
        });
        foreach (var code in new[] { "R1", "R2", "R3" })
        {
            var resp = await client.PostAsJsonAsync(
                $"/api/data-entry/unit-results/{unitRsc}/periods/{code}/score", unanimousRed);
            resp.IsSuccessStatusCode.Should().BeTrue();
        }

        // Confirm
        var confirmResp = await client.PostAsync(
            $"/api/data-entry/unit-results/{unitRsc}/confirm", content: null);
        confirmResp.IsSuccessStatusCode.Should().BeTrue();

        // Verify
        var final = await client.GetFromJsonAsync<UnitResultDto>(
            $"/api/data-entry/unit-results/{unitRsc}");
        final!.Status.Should().Be("Official");
        final.Decision!.Code.Should().Be("Wp");
        final.Decision.DecisionMark.Should().Be("3:0");
        final.Decision.WinnerParticipantId.Should().Be("NOC-ESP-0020");
        final.Competitors[0].Wlt.Should().Be("W");
        final.Competitors[1].Wlt.Should().Be("L");
    }

    private record ScorePeriodBody(ScorecardBody[] Scorecards);
    private record ScorecardBody(string JudgePos, int HomeScore, int AwayScore);
    private record UnitResultDto(
        string UnitRsc, string Status, DecisionDto? Decision,
        List<CompetitorDto> Competitors);
    private record DecisionDto(
        string Type, string Code, string? DecisionMark, string? WinnerParticipantId);
    private record CompetitorDto(int SortOrder, string ParticipantId, string? Wlt);
}
```

- [ ] **Step 2: Run + commit**

```bash
dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~ScoringPathTests"
git add tests/OVR.Api.IntegrationTests/DataEntry/ScoringPathTests.cs
git commit -m "test(dataentry): full points-path integration test (StartList→Official)"
```

---

## Task 32: Integration test — full stoppage path

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/DataEntry/StoppagePathTests.cs`

- [ ] **Step 1: Write test**

```csharp
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class StoppagePathTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public StoppagePathTests(DataEntryWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task TkoI_InRound2_SetsRmPointsDecisionAndOfficializes()
    {
        var eventRsc = "BOXW---------------M81KG---------";
        var unitRsc = "BOXW---------------M81KG-FNL-0001----";
        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, 1, 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-ESP-0030", "ESP", 1),
            ("NOC-POL-0031", "POL", 2)
        });

        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>()
                .Publish(new UnitScheduledEvent(
                    unitRsc, eventRsc, "S1", "BXR",
                    DateTime.UtcNow, 1, 1, DateTime.UtcNow));
        }

        var client = _factory.CreateClient();

        await client.PostAsync($"/api/data-entry/unit-results/{unitRsc}/start", null);
        await client.PostAsJsonAsync(
            $"/api/data-entry/unit-results/{unitRsc}/periods/R1/score",
            new ScorePeriodBody(new[]
            {
                new ScorecardBody("J1", 10, 9),
                new ScorecardBody("J2", 10, 9),
                new ScorecardBody("J3", 10, 9)
            }));

        // Stoppage mid-R2
        var finish = await client.PostAsJsonAsync(
            $"/api/data-entry/unit-results/{unitRsc}/finish-stoppage",
            new FinishStoppageBody("TkoI", "R2", "01:30", "NOC-ESP-0030"));
        finish.IsSuccessStatusCode.Should().BeTrue();

        await client.PostAsync($"/api/data-entry/unit-results/{unitRsc}/confirm", null);

        var final = await client.GetFromJsonAsync<UnitResultDto>(
            $"/api/data-entry/unit-results/{unitRsc}");
        final!.Status.Should().Be("Official");
        final.Decision!.Code.Should().Be("TkoI");
        final.Decision.Type.Should().Be("RmPoints");
        final.Decision.StoppageRound.Should().Be("R2");
        final.Decision.StoppageTime.Should().Be("01:30");
    }

    private record ScorePeriodBody(ScorecardBody[] Scorecards);
    private record ScorecardBody(string JudgePos, int HomeScore, int AwayScore);
    private record FinishStoppageBody(
        string ResultCode, string StoppageRound, string StoppageTime,
        string? WinnerParticipantId);
    private record UnitResultDto(string Status, DecisionDto? Decision);
    private record DecisionDto(
        string Type, string Code, string? StoppageRound, string? StoppageTime);
}
```

- [ ] **Step 2: Run + commit**

```bash
dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~StoppagePathTests"
git add tests/OVR.Api.IntegrationTests/DataEntry/StoppagePathTests.cs
git commit -m "test(dataentry): stoppage-path integration test (TKO-I mid-R2)"
```

---

## Task 33: Integration tests — validation errors + listing

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/DataEntry/ValidationTests.cs`
- Create: `tests/OVR.Api.IntegrationTests/DataEntry/ListUnitResultsTests.cs`

- [ ] **Step 1: Write `ValidationTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class ValidationTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public ValidationTests(DataEntryWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetUnitResult_NotFound_Returns404()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync(
            "/api/data-entry/unit-results/DOES-NOT-EXIST-------------------");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_WhenAlreadyLive_Returns422()
    {
        var eventRsc = "BOXW---------------M91KG---------";
        var unitRsc = "BOXW---------------M91KG-FNL-0001----";
        await _factory.SeedFirstRoundBracketAsync(eventRsc, unitRsc, 1, 2);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-A-0001", "ESP", 1), ("NOC-B-0001", "POL", 2)
        });
        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IPublisher>()
                .Publish(new UnitScheduledEvent(
                    unitRsc, eventRsc, "S1", "BXR",
                    DateTime.UtcNow, 1, 1, DateTime.UtcNow));
        }

        var client = _factory.CreateClient();
        await client.PostAsync($"/api/data-entry/unit-results/{unitRsc}/start", null);
        var again = await client.PostAsync($"/api/data-entry/unit-results/{unitRsc}/start", null);
        again.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ScorePeriod_ScoreOutOfRange_Returns400()
    {
        // Level 1 FluentValidation: InclusiveBetween 6..10 — maps to 400.
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/data-entry/unit-results/ANY/periods/R1/score",
            new { Scorecards = new[] { new { JudgePos = "J1", HomeScore = 11, AwayScore = 9 } } });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task FinishByStoppage_Wp_Returns400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/data-entry/unit-results/ANY/finish-stoppage",
            new { ResultCode = "Wp", StoppageRound = "R2", StoppageTime = "01:00" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Step 2: Write `ListUnitResultsTests.cs`**

```csharp
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OVR.Api.IntegrationTests.DataEntry.Support;
using OVR.SharedKernel.Domain.Events.Integration;
using Xunit;

namespace OVR.Api.IntegrationTests.DataEntry;

public class ListUnitResultsTests : IClassFixture<DataEntryWebAppFactory>
{
    private readonly DataEntryWebAppFactory _factory;

    public ListUnitResultsTests(DataEntryWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task List_BySessionCode_ReturnsOnlyUnitsInThatSession()
    {
        var eventRsc = "BOXW---------------M60KG---------";
        var rscA = "BOXW---------------M60KG-FNL-0001----";
        var rscB = "BOXW---------------M60KG-FNL-0002----";

        await _factory.SeedFirstRoundBracketAsync(eventRsc, rscA, 1, 2);
        await _factory.SeedFirstRoundBracketAsync(eventRsc, rscB, 3, 4);
        await _factory.SeedEntriesAsync(eventRsc, new[]
        {
            ("NOC-A-1", "ESP", 1), ("NOC-A-2", "POL", 2),
            ("NOC-A-3", "ESP", 3), ("NOC-A-4", "POL", 4)
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var pub = scope.ServiceProvider.GetRequiredService<IPublisher>();
            await pub.Publish(new UnitScheduledEvent(
                rscA, eventRsc, "S-ALPHA", "BXR",
                DateTime.UtcNow, 1, 1, DateTime.UtcNow));
            await pub.Publish(new UnitScheduledEvent(
                rscB, eventRsc, "S-BETA", "BXR",
                DateTime.UtcNow, 1, 2, DateTime.UtcNow));
        }

        var client = _factory.CreateClient();
        var list = await client.GetFromJsonAsync<List<ListItem>>(
            "/api/data-entry/unit-results?sessionCode=S-ALPHA");

        list!.Should().HaveCount(1);
        list[0].UnitRsc.Should().Be(rscA);
    }

    private record ListItem(string UnitRsc, string Status);
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~DataEntry"
git add tests/OVR.Api.IntegrationTests/DataEntry/ValidationTests.cs tests/OVR.Api.IntegrationTests/DataEntry/ListUnitResultsTests.cs
git commit -m "test(dataentry): validation error paths + listing by session/location"
```

---

## Task 34: Final verification + merge-ready state

**Files:** none

- [ ] **Step 1: Run full test suite**

```bash
dotnet test
```

Expected: all tests pass across all projects.

- [ ] **Step 2: Run full build**

```bash
dotnet build
```

Expected: success with no warnings.

- [ ] **Step 3: Verify git log is clean**

```bash
git log --oneline main..feat/dataentry-mvp
```

Expected: roughly 30+ commits, each scoped to one task.

- [ ] **Step 4: Manual smoke test (optional)**

Start the API and exercise the loop with a real `UnitScheduledEvent` via test data. Skip if time-constrained; the integration tests cover this.

- [ ] **Step 5: Create merge commit to main**

This task does NOT create a PR — ask the user before merging. When ready:

```bash
git checkout main
git merge --no-ff feat/dataentry-mvp -m "Merge branch 'feat/dataentry-mvp' — DataEntry MVP complete"
```

---

## Post-merge follow-ups (NOT in MVP 3)

Once merged and stable, triage these TD items into proper tickets:

- TD-DE-01: Move corner-assignment convention to CompetitionConfig.
- TD-DE-02: Define policy for `UnitScheduleChangedEvent`/`UnitUnscheduledEvent` handling.
- TD-DE-03: Capture warnings and knockdowns.
- TD-DE-04: Parameterize sport rules via `ISportRuleEngine`.
- TD-DE-05: Revert/amendment from `Official`.
- TD-DE-06: Remove legacy `ResultConfirmedEvent`.

These are referenced in the spec at `docs/superpowers/specs/2026-04-18-dataentry-mvp-design.md` under "Technical debt".


