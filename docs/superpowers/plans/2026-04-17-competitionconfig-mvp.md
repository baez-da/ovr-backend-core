# CompetitionConfig MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build CompetitionConfig module MVP exposing `POST /events` and `POST /events/{rsc}/generate-structure` endpoints that create Event instances and generate single-elimination bracket structure (Phases + empty Units) for boxing.

**Architecture:** Vertical-slice module with Event + Phase entities inside the Event aggregate, Unit as separate aggregate, `BracketGenerator` as domain service, two MongoDB collections (`competitionconfig_events`, `competitionconfig_units`), `EventStructureGeneratedEvent` integration event on SharedKernel for downstream consumers.

**Tech Stack:** .NET 10, C# 14, MediatR 12.4 (CQRS + pipeline), FluentValidation 11.11, ErrorOr 2.0, MongoDB.Driver 3.4, xUnit + FluentAssertions + NSubstitute + Testcontainers.MongoDb.

**Spec reference:** `docs/superpowers/specs/2026-04-17-competitionconfig-mvp-design.md`

---

## File Structure Map

### New files

```
src/OVR.Modules.CompetitionConfig/
├── Domain/
│   ├── CompetitionFormat.cs          # enum
│   ├── PhaseCodes.cs                 # ODF standard constants
│   ├── Phase.cs                      # entity inside Event
│   ├── Unit.cs                       # aggregate
│   └── BracketGenerator.cs           # domain service + BracketPlan + PhaseSpec records
├── Features/
│   ├── CreateEvent/
│   │   ├── CreateEventCommand.cs
│   │   ├── CreateEventValidator.cs
│   │   ├── CreateEventHandler.cs
│   │   └── CreateEventEndpoint.cs
│   └── GenerateEventStructure/
│       ├── GenerateEventStructureCommand.cs
│       ├── GenerateEventStructureValidator.cs
│       ├── GenerateEventStructureHandler.cs
│       └── GenerateEventStructureEndpoint.cs
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

src/OVR.SharedKernel/Domain/Events/Integration/
└── EventStructureGeneratedEvent.cs   # NEW integration event

tests/OVR.Modules.CompetitionConfig.Tests/       # NEW project
├── Domain/
│   ├── BracketGeneratorTests.cs
│   ├── EventAggregateTests.cs
│   ├── UnitAggregateTests.cs
│   └── PhaseTests.cs
├── Features/
│   ├── CreateEvent/
│   │   └── CreateEventHandlerTests.cs
│   └── GenerateEventStructure/
│       └── GenerateEventStructureHandlerTests.cs
└── OVR.Modules.CompetitionConfig.Tests.csproj

tests/OVR.Api.IntegrationTests/CompetitionConfig/
├── Support/
│   └── CompetitionConfigWebAppFactory.cs
├── CreateEventEndpointTests.cs
└── GenerateEventStructureEndpointTests.cs
```

### Modified files

- `src/OVR.Modules.CompetitionConfig/Domain/Event.cs` — expand aggregate
- `src/OVR.Modules.CompetitionConfig/CompetitionConfigModule.cs` — DI + endpoints
- `src/OVR.Modules.CompetitionConfig/OVR.Modules.CompetitionConfig.csproj` — add packages + I18n content
- `src/OVR.Modules.CommonCodes/Contracts/CommonCodeTypes.cs` — add `Event` constant
- `src/OVR.Api/Program.cs` — register MediatR assembly (already done) + add FluentValidation assembly
- `OVR.sln` — add test project

### Deleted files

- `src/OVR.Modules.CompetitionConfig/Domain/Discipline.cs` — dead scaffolding

---

## Task 1: Cleanup dead code + add Event CC constant

**Files:**
- Delete: `src/OVR.Modules.CompetitionConfig/Domain/Discipline.cs`
- Modify: `src/OVR.Modules.CommonCodes/Contracts/CommonCodeTypes.cs`

- [ ] **Step 1: Delete `Discipline.cs`**

```bash
rm src/OVR.Modules.CompetitionConfig/Domain/Discipline.cs
```

- [ ] **Step 2: Add `Event` constant to `CommonCodeTypes`**

Edit `src/OVR.Modules.CommonCodes/Contracts/CommonCodeTypes.cs` to add one line:

```csharp
public static class CommonCodeTypes
{
    public const string Organisation = "ORGANISATIONS";
    public const string Discipline = "DISCIPLINE";
    public const string DisciplineFunction = "DISCIPLINE_FUNCTION";
    public const string FunctionCategory = "FUNCTION_CATEGORY";
    public const string Country = "COUNTRY";
    public const string PersonGender = "PERSON_GENDER";
    public const string Sport = "SPORT";
    public const string Event = "EVENT";        // NEW
}
```

- [ ] **Step 3: Build to verify nothing broke**

Run: `dotnet build`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore(competitionconfig): remove dead Discipline stub and add Event to CommonCodeTypes"
```

---

## Task 2: `CompetitionFormat` enum and `PhaseCodes` constants

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Domain/CompetitionFormat.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Domain/PhaseCodes.cs`

- [ ] **Step 1: Create `CompetitionFormat.cs`**

```csharp
namespace OVR.Modules.CompetitionConfig.Domain;

public enum CompetitionFormat
{
    SingleElimination = 1
}
```

- [ ] **Step 2: Create `PhaseCodes.cs`**

```csharp
namespace OVR.Modules.CompetitionConfig.Domain;

public static class PhaseCodes
{
    // Knockouts (used in MVP for single-elimination)
    public const string R128 = "R128";
    public const string R64 = "R64-";
    public const string R32 = "R32-";
    public const string EighthFinals = "8FNL";    // Round of 16
    public const string QuarterFinals = "QFNL";
    public const string SemiFinals = "SFNL";
    public const string Final = "FNL-";

    // Reference constants for future use
    public const string Preliminaries = "PREL";
    public const string Qualification = "QUAL";
    public const string Heat = "HEAT";
    public const string LuckyLoser = "LL--";
    public const string Repechage = "REP-";
}
```

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Domain/CompetitionFormat.cs src/OVR.Modules.CompetitionConfig/Domain/PhaseCodes.cs
git commit -m "feat(competitionconfig): add CompetitionFormat enum and PhaseCodes constants"
```

---

## Task 3: Create test project scaffolding

**Files:**
- Create: `tests/OVR.Modules.CompetitionConfig.Tests/OVR.Modules.CompetitionConfig.Tests.csproj`
- Create: `tests/OVR.Modules.CompetitionConfig.Tests/Usings.cs` (implicit usings for xUnit)

- [ ] **Step 1: Create `OVR.Modules.CompetitionConfig.Tests.csproj`**

Content:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\OVR.Modules.CompetitionConfig\OVR.Modules.CompetitionConfig.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add project to solution**

Run: `dotnet sln add tests/OVR.Modules.CompetitionConfig.Tests/OVR.Modules.CompetitionConfig.Tests.csproj`

- [ ] **Step 3: Build solution**

Run: `dotnet build`
Expected: succeeds (empty test project).

- [ ] **Step 4: Commit**

```bash
git add tests/OVR.Modules.CompetitionConfig.Tests/ OVR.sln
git commit -m "test(competitionconfig): add test project scaffold"
```

---

## Task 4: `BracketGenerator` — failing tests first

**Files:**
- Create: `tests/OVR.Modules.CompetitionConfig.Tests/Domain/BracketGeneratorTests.cs`

- [ ] **Step 1: Write failing tests for `BracketGenerator.Generate`**

Create `tests/OVR.Modules.CompetitionConfig.Tests/Domain/BracketGeneratorTests.cs`:

```csharp
using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class BracketGeneratorTests
{
    private readonly BracketGenerator _generator = new();

    [Fact]
    public void Generate_WithSize2_ReturnsSinglePhaseWithOneUnit()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 2, startUnitNumber: 1);

        plan.Phases.Should().HaveCount(1);
        plan.Phases[0].Code.Should().Be(PhaseCodes.Final);
        plan.Phases[0].Order.Should().Be(0);
        plan.Phases[0].UnitCount.Should().Be(1);
        plan.UnitLocalSegments.Should().HaveCount(1);
        plan.UnitLocalSegments[0].Should().Be("FNL-0001----");
    }

    [Fact]
    public void Generate_WithSize4_Returns_SFNL_FNL_WithCorrectUnitCounts()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.Phases.Select(p => p.UnitCount).Should().Equal(2, 1);
        plan.UnitLocalSegments.Should().HaveCount(3);
        plan.UnitLocalSegments.Should().ContainInOrder(
            "SFNL0001----", "SFNL0002----", "FNL-0003----");
    }

    [Fact]
    public void Generate_WithSize8_Returns_QFNL_SFNL_FNL()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 8, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(
            PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.Phases.Select(p => p.UnitCount).Should().Equal(4, 2, 1);
        plan.UnitLocalSegments.Should().HaveCount(7);
    }

    [Fact]
    public void Generate_WithSize16_Returns_8FNL_QFNL_SFNL_FNL()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 16, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(
            PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals,
            PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.Phases.Select(p => p.UnitCount).Should().Equal(8, 4, 2, 1);
        plan.UnitLocalSegments.Should().HaveCount(15);
        plan.UnitLocalSegments[0].Should().Be("8FNL0001----");
        plan.UnitLocalSegments[^1].Should().Be("FNL-0015----");
    }

    [Fact]
    public void Generate_WithSize32_Returns_R32_through_FNL()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 32, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(
            PhaseCodes.R32, PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals,
            PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.Phases.Select(p => p.UnitCount).Should().Equal(16, 8, 4, 2, 1);
        plan.UnitLocalSegments.Should().HaveCount(31);
    }

    [Fact]
    public void Generate_WithSize13_RoundsUpToM16_WithSamePhases()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 13, startUnitNumber: 1);

        plan.Phases.Select(p => p.Code).Should().Equal(
            PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals,
            PhaseCodes.SemiFinals, PhaseCodes.Final);
        plan.UnitLocalSegments.Should().HaveCount(15);
    }

    [Fact]
    public void Generate_WithSize33_RoundsUpToM64()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 33, startUnitNumber: 1);

        plan.Phases[0].Code.Should().Be(PhaseCodes.R64);
        plan.UnitLocalSegments.Should().HaveCount(63);
    }

    [Fact]
    public void Generate_StartingAtUnitNumber5_FirstUnitSegmentStartsWith_0005()
    {
        var plan = _generator.Generate(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 5);

        plan.UnitLocalSegments[0].Should().Be("SFNL0005----");
        plan.UnitLocalSegments[^1].Should().Be("FNL-0007----");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(129)]
    [InlineData(500)]
    public void Generate_WithOutOfRangeSize_Throws(int size)
    {
        Action act = () => _generator.Generate(CompetitionFormat.SingleElimination, size, startUnitNumber: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Generate_WithUnsupportedFormat_Throws()
    {
        Action act = () => _generator.Generate((CompetitionFormat)999, size: 16, startUnitNumber: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

- [ ] **Step 2: Run to verify tests fail (compilation error expected — BracketGenerator doesn't exist yet)**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/`
Expected: compilation error `BracketGenerator` not found.

- [ ] **Step 3: Commit failing tests**

```bash
git add tests/OVR.Modules.CompetitionConfig.Tests/Domain/BracketGeneratorTests.cs
git commit -m "test(competitionconfig): add failing BracketGenerator tests"
```

---

## Task 5: Implement `BracketGenerator`

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Domain/BracketGenerator.cs`

- [ ] **Step 1: Implement `BracketGenerator` with records**

```csharp
namespace OVR.Modules.CompetitionConfig.Domain;

public sealed record PhaseSpec(string Code, int Order, int UnitCount);

public sealed record BracketPlan(
    IReadOnlyList<PhaseSpec> Phases,
    IReadOnlyList<string> UnitLocalSegments);

public sealed class BracketGenerator
{
    private const int MinSize = 2;
    private const int MaxSize = 128;

    public BracketPlan Generate(CompetitionFormat format, int size, int startUnitNumber)
    {
        if (size < MinSize || size > MaxSize)
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                $"Size must be between {MinSize} and {MaxSize}.");

        if (format != CompetitionFormat.SingleElimination)
            throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Only SingleElimination is supported in MVP.");

        var m = SmallestPowerOf2AtLeast(size);
        var phaseCodes = PhasesForBracketSize(m);

        var phases = new List<PhaseSpec>();
        var segments = new List<string>();
        var unitCounter = startUnitNumber;

        for (var i = 0; i < phaseCodes.Length; i++)
        {
            var phaseUnitCount = m >> (i + 1); // M / 2^(i+1)
            phases.Add(new PhaseSpec(phaseCodes[i], i, phaseUnitCount));

            for (var u = 0; u < phaseUnitCount; u++)
            {
                var unitBlock = $"{unitCounter:D4}--";
                segments.Add($"{phaseCodes[i]}{unitBlock}");
                unitCounter++;
            }
        }

        return new BracketPlan(phases, segments);
    }

    private static int SmallestPowerOf2AtLeast(int n)
    {
        var p = 1;
        while (p < n) p <<= 1;
        return p;
    }

    private static string[] PhasesForBracketSize(int m) => m switch
    {
        2 => [PhaseCodes.Final],
        4 => [PhaseCodes.SemiFinals, PhaseCodes.Final],
        8 => [PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        16 => [PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        32 => [PhaseCodes.R32, PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        64 => [PhaseCodes.R64, PhaseCodes.R32, PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        128 => [PhaseCodes.R128, PhaseCodes.R64, PhaseCodes.R32, PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        _ => throw new ArgumentOutOfRangeException(nameof(m), m, "Unsupported bracket size.")
    };
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/`
Expected: all 12 tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Domain/BracketGenerator.cs
git commit -m "feat(competitionconfig): implement BracketGenerator for single-elimination"
```

---

## Task 6: `Phase` entity

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Domain/Phase.cs`
- Create: `tests/OVR.Modules.CompetitionConfig.Tests/Domain/PhaseTests.cs`

- [ ] **Step 1: Write failing test**

Create `tests/OVR.Modules.CompetitionConfig.Tests/Domain/PhaseTests.cs`:

```csharp
using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class PhaseTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties()
    {
        var phase = Phase.CreateInternal(PhaseCodes.EighthFinals, order: 0, unitCount: 8);

        phase.Id.Should().Be(PhaseCodes.EighthFinals);
        phase.Code.Should().Be(PhaseCodes.EighthFinals);
        phase.Order.Should().Be(0);
        phase.UnitCount.Should().Be(8);
    }

    [Fact]
    public void Create_WithNegativeOrder_Throws()
    {
        Action act = () => Phase.CreateInternal(PhaseCodes.Final, order: -1, unitCount: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithZeroOrNegativeUnitCount_Throws()
    {
        Action act = () => Phase.CreateInternal(PhaseCodes.Final, order: 0, unitCount: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~PhaseTests"`
Expected: compile error — `Phase` not found.

- [ ] **Step 3: Implement `Phase`**

Create `src/OVR.Modules.CompetitionConfig/Domain/Phase.cs`:

```csharp
using OVR.SharedKernel.Domain.Primitives;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Phase : Entity<string>
{
    public string Code { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public int UnitCount { get; private set; }

    private Phase() { }

    // Public for tests; intended for internal use by Event aggregate only.
    public static Phase CreateInternal(string code, int order, int unitCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentOutOfRangeException.ThrowIfNegative(order);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitCount);

        return new Phase { Id = code, Code = code, Order = order, UnitCount = unitCount };
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~PhaseTests"`
Expected: 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Domain/Phase.cs tests/OVR.Modules.CompetitionConfig.Tests/Domain/PhaseTests.cs
git commit -m "feat(competitionconfig): add Phase entity with validation"
```

---

## Task 7: `Unit` aggregate

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Domain/Unit.cs`
- Create: `tests/OVR.Modules.CompetitionConfig.Tests/Domain/UnitAggregateTests.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/OVR.Modules.CompetitionConfig.Tests/Domain/UnitAggregateTests.cs`:

```csharp
using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class UnitAggregateTests
{
    [Fact]
    public void Create_FromUnitLevelRsc_DerivesEventRscPhaseCodeAndUnitNumber()
    {
        var rsc = Rsc.Create("BOXM57KG--------------8FNL0001----");

        var unit = Unit.Create(rsc);

        unit.Id.Should().Be("BOXM57KG--------------8FNL0001----");
        unit.Rsc.Value.Should().Be("BOXM57KG--------------8FNL0001----");
        unit.EventRsc.Value.Should().Be("BOXM57KG--------------------------");
        unit.PhaseCode.Should().Be("8FNL");
        unit.UnitNumber.Should().Be(1);
        unit.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithUnitNumber15_ParsesCorrectly()
    {
        var rsc = Rsc.Create("BOXM57KG--------------FNL-0015----");

        var unit = Unit.Create(rsc);

        unit.UnitNumber.Should().Be(15);
        unit.PhaseCode.Should().Be("FNL-");
    }

    [Fact]
    public void Create_FromEventLevelRsc_Throws()
    {
        var rsc = Rsc.Create("BOXM57KG--------------------------");

        Action act = () => Unit.Create(rsc);

        act.Should().Throw<ArgumentException>().WithMessage("*Unit*");
    }

    [Fact]
    public void Create_FromPhaseLevelRsc_Throws()
    {
        // Phase level: discipline+gender+event+phase, unit/sub dashes.
        var rsc = Rsc.Create("BOXM57KG--------------8FNL--------");

        Action act = () => Unit.Create(rsc);

        act.Should().Throw<ArgumentException>().WithMessage("*Unit*");
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~UnitAggregateTests"`
Expected: compile error — `Unit` not found.

- [ ] **Step 3: Implement `Unit`**

Create `src/OVR.Modules.CompetitionConfig/Domain/Unit.cs`:

```csharp
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Unit : AggregateRoot<string>
{
    public Rsc Rsc { get; private set; } = null!;
    public Rsc EventRsc { get; private set; } = null!;
    public string PhaseCode { get; private set; } = string.Empty;
    public int UnitNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Unit() { }

    public static Unit Create(Rsc rsc)
    {
        ArgumentNullException.ThrowIfNull(rsc);

        if (!rsc.IsAtLevel(RscLevel.Unit))
            throw new ArgumentException(
                $"RSC must be at Unit level, got {rsc.Level}: '{rsc.Value}'.",
                nameof(rsc));

        var eventRsc = Rsc.Create(rsc.AtEventLevel());
        var unitNumberStr = rsc.Unit.TrimEnd('-');
        var unitNumber = int.Parse(unitNumberStr);

        return new Unit
        {
            Id = rsc.Value,
            Rsc = rsc,
            EventRsc = eventRsc,
            PhaseCode = rsc.Phase,
            UnitNumber = unitNumber,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~UnitAggregateTests"`
Expected: all 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Domain/Unit.cs tests/OVR.Modules.CompetitionConfig.Tests/Domain/UnitAggregateTests.cs
git commit -m "feat(competitionconfig): add Unit aggregate"
```

---

## Task 8: `EventStructureGeneratedEvent` integration event

**Files:**
- Create: `src/OVR.SharedKernel/Domain/Events/Integration/EventStructureGeneratedEvent.cs`

- [ ] **Step 1: Create the integration event**

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record EventStructureGeneratedEvent(
    string EventRsc,
    string Format,
    int Size,
    IReadOnlyList<PhaseInfo> Phases,
    IReadOnlyList<string> UnitRscs,
    DateTime GeneratedAt) : DomainEventBase;

public sealed record PhaseInfo(string Code, int Order, int UnitCount);
```

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/OVR.SharedKernel/Domain/Events/Integration/EventStructureGeneratedEvent.cs
git commit -m "feat(sharedkernel): add EventStructureGeneratedEvent integration event"
```

---

## Task 9: Expand `Event` aggregate — failing tests

**Files:**
- Create: `tests/OVR.Modules.CompetitionConfig.Tests/Domain/EventAggregateTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class EventAggregateTests
{
    private readonly BracketGenerator _generator = new();

    private static Event CreateBoxing57Kg() =>
        Event.Create(
            rsc: Rsc.Create("BOXM57KG--------------------------"),
            discipline: "BOX",
            gender: Gender.Create("M"),
            eventCode: "57KG",
            modifier: null,
            name: "Men's 57kg");

    [Fact]
    public void Create_SetsPropertiesAndDefaults()
    {
        var evt = CreateBoxing57Kg();

        evt.Id.Should().Be("BOXM57KG--------------------------");
        evt.Discipline.Should().Be("BOX");
        evt.EventCode.Should().Be("57KG");
        evt.Modifier.Should().BeNull();
        evt.Name.Should().Be("Men's 57kg");
        evt.Format.Should().BeNull();
        evt.Size.Should().BeNull();
        evt.Phases.Should().BeEmpty();
        evt.StructureGeneratedAt.Should().BeNull();
    }

    [Fact]
    public void GenerateStructure_Size16_SetsFormatSizeAndPhases()
    {
        var evt = CreateBoxing57Kg();

        var result = evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 16, startUnitNumber: 1, _generator);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(15);
        evt.Format.Should().Be(CompetitionFormat.SingleElimination);
        evt.Size.Should().Be(16);
        evt.Phases.Should().HaveCount(4);
        evt.Phases.Select(p => p.Code).Should().Equal("8FNL", "QFNL", "SFNL", "FNL-");
        evt.StructureGeneratedAt.Should().NotBeNull();
    }

    [Fact]
    public void GenerateStructure_ProducesCorrectUnitRscs()
    {
        var evt = CreateBoxing57Kg();

        var result = evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1, _generator);

        result.Value.Select(r => r.Value).Should().Equal(
            "BOXM57KG--------------SFNL0001----",
            "BOXM57KG--------------SFNL0002----",
            "BOXM57KG--------------FNL-0003----");
    }

    [Fact]
    public void GenerateStructure_RaisesDomainEvent()
    {
        var evt = CreateBoxing57Kg();

        evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1, _generator);

        var raised = evt.DomainEvents.OfType<EventStructureGeneratedEvent>().SingleOrDefault();
        raised.Should().NotBeNull();
        raised!.EventRsc.Should().Be("BOXM57KG--------------------------");
        raised.Format.Should().Be("SingleElimination");
        raised.Size.Should().Be(4);
        raised.Phases.Should().HaveCount(2);
        raised.UnitRscs.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateStructure_CalledTwice_ReturnsError()
    {
        var evt = CreateBoxing57Kg();
        evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 4, startUnitNumber: 1, _generator);

        var result = evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 8, startUnitNumber: 1, _generator);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.StructureAlreadyGenerated");
    }

    [Fact]
    public void GenerateStructure_WithSizeOutOfRange_ReturnsInvalidSize()
    {
        var evt = CreateBoxing57Kg();

        var result = evt.GenerateStructure(CompetitionFormat.SingleElimination, size: 200, startUnitNumber: 1, _generator);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.InvalidSize");
    }

    [Fact]
    public void GenerateStructure_WithUnsupportedFormat_ReturnsUnsupportedFormat()
    {
        var evt = CreateBoxing57Kg();

        var result = evt.GenerateStructure((CompetitionFormat)99, size: 16, startUnitNumber: 1, _generator);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.UnsupportedFormat");
    }
}
```

- [ ] **Step 2: Run to verify fail**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~EventAggregateTests"`
Expected: compile errors — missing members like `Discipline`, `GenerateStructure`, etc.

- [ ] **Step 3: Commit failing tests**

```bash
git add tests/OVR.Modules.CompetitionConfig.Tests/Domain/EventAggregateTests.cs
git commit -m "test(competitionconfig): add failing Event aggregate tests"
```

---

## Task 10: Implement `CompetitionConfigErrors`

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Errors/CompetitionConfigErrors.cs`

- [ ] **Step 1: Create errors file**

```csharp
using ErrorOr;

namespace OVR.Modules.CompetitionConfig.Errors;

public static class CompetitionConfigErrors
{
    public static Error InvalidDiscipline(string code) =>
        Error.Validation(
            "CompetitionConfig.InvalidDiscipline",
            "Discipline code is not in the common codes catalog.",
            new Dictionary<string, object> { ["discipline"] = code });

    public static Error InvalidEventCode(string code) =>
        Error.Validation(
            "CompetitionConfig.InvalidEventCode",
            "Event code is not in the common codes catalog.",
            new Dictionary<string, object> { ["eventCode"] = code });

    public static Error EventAlreadyExists(string rsc) =>
        Error.Conflict(
            "CompetitionConfig.EventAlreadyExists",
            "An event with this RSC already exists.",
            new Dictionary<string, object> { ["rsc"] = rsc });

    public static Error EventNotFound(string rsc) =>
        Error.NotFound(
            "CompetitionConfig.EventNotFound",
            "Event not found.",
            new Dictionary<string, object> { ["rsc"] = rsc });

    public static Error StructureAlreadyGenerated(string rsc) =>
        Error.Conflict(
            "CompetitionConfig.StructureAlreadyGenerated",
            "Structure was already generated for this event.",
            new Dictionary<string, object> { ["rsc"] = rsc });

    public static Error UnsupportedFormat(string format) =>
        Error.Validation(
            "CompetitionConfig.UnsupportedFormat",
            "Competition format not supported in this version.",
            new Dictionary<string, object> { ["format"] = format });

    public static Error InvalidSize(int size) =>
        Error.Validation(
            "CompetitionConfig.InvalidSize",
            "Size must be between 2 and 128.",
            new Dictionary<string, object> { ["size"] = size });
}
```

- [ ] **Step 2: Update CompetitionConfig csproj to reference ErrorOr**

Edit `src/OVR.Modules.CompetitionConfig/OVR.Modules.CompetitionConfig.csproj` so it has the same package references as Entries:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="ErrorOr" />
    <PackageReference Include="MongoDB.Driver" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\OVR.SharedKernel\OVR.SharedKernel.csproj" />
    <ProjectReference Include="..\OVR.Modules.CommonCodes\OVR.Modules.CommonCodes.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="I18n\**" Link="I18n.CompetitionConfig\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Build**

Run: `dotnet build src/OVR.Modules.CompetitionConfig/`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Errors/ src/OVR.Modules.CompetitionConfig/OVR.Modules.CompetitionConfig.csproj
git commit -m "feat(competitionconfig): add typed errors and wire up packages"
```

---

## Task 11: Implement expanded `Event` aggregate

**Files:**
- Modify: `src/OVR.Modules.CompetitionConfig/Domain/Event.cs`

- [ ] **Step 1: Replace `Event.cs` body with expanded aggregate**

```csharp
using ErrorOr;
using OVR.Modules.CompetitionConfig.Errors;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Event : AggregateRoot<string>
{
    public Rsc Rsc { get; private set; } = null!;
    public string Discipline { get; private set; } = string.Empty;
    public Gender Gender { get; private set; } = null!;
    public string EventCode { get; private set; } = string.Empty;
    public string? Modifier { get; private set; }
    public string Name { get; private set; } = string.Empty;
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
        string name)
    {
        ArgumentNullException.ThrowIfNull(rsc);
        ArgumentException.ThrowIfNullOrWhiteSpace(discipline);
        ArgumentNullException.ThrowIfNull(gender);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Event
        {
            Id = rsc.Value,
            Rsc = rsc,
            Discipline = discipline,
            Gender = gender,
            EventCode = eventCode,
            Modifier = modifier,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }

    public ErrorOr<IReadOnlyList<Rsc>> GenerateStructure(
        CompetitionFormat format,
        int size,
        int startUnitNumber,
        BracketGenerator generator)
    {
        if (Format.HasValue)
            return CompetitionConfigErrors.StructureAlreadyGenerated(Id);

        if (format != CompetitionFormat.SingleElimination)
            return CompetitionConfigErrors.UnsupportedFormat(format.ToString());

        if (size < 2 || size > 128)
            return CompetitionConfigErrors.InvalidSize(size);

        var plan = generator.Generate(format, size, startUnitNumber);

        _phases.AddRange(plan.Phases.Select(s => Phase.CreateInternal(s.Code, s.Order, s.UnitCount)));
        Format = format;
        Size = size;
        StructureGeneratedAt = DateTime.UtcNow;

        var eventPrefix = Rsc.Value[..22];
        var unitRscs = plan.UnitLocalSegments
            .Select(seg => Rsc.Create(eventPrefix + seg))
            .ToList();

        RaiseDomainEvent(new EventStructureGeneratedEvent(
            EventRsc: Id,
            Format: format.ToString(),
            Size: size,
            Phases: plan.Phases.Select(p => new PhaseInfo(p.Code, p.Order, p.UnitCount)).ToList(),
            UnitRscs: unitRscs.Select(r => r.Value).ToList(),
            GeneratedAt: StructureGeneratedAt.Value));

        return unitRscs;
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~EventAggregateTests"`
Expected: all 7 tests pass.

- [ ] **Step 3: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Domain/Event.cs
git commit -m "feat(competitionconfig): expand Event aggregate with GenerateStructure"
```

---

## Task 12: Persistence — `EventDocument`, `EventMapping`, `IEventRepository`

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/EventDocument.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/EventMapping.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/IEventRepository.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/MongoEventRepository.cs`

- [ ] **Step 1: Create `EventDocument.cs`**

```csharp
using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.CompetitionConfig.Persistence;

public sealed class EventDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string? Modifier { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Format { get; set; }
    public int? Size { get; set; }
    public List<PhaseSubDocument> Phases { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? StructureGeneratedAt { get; set; }
}

public sealed class PhaseSubDocument
{
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public int UnitCount { get; set; }
}
```

- [ ] **Step 2: Create `EventMapping.cs`**

```csharp
using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Persistence;

internal static class EventMapping
{
    public static EventDocument ToDocument(Event @event) => new()
    {
        Id = @event.Id,
        Discipline = @event.Discipline,
        Gender = @event.Gender.Value,
        EventCode = @event.EventCode,
        Modifier = @event.Modifier,
        Name = @event.Name,
        Format = @event.Format?.ToString(),
        Size = @event.Size,
        Phases = @event.Phases
            .Select(p => new PhaseSubDocument { Code = p.Code, Order = p.Order, UnitCount = p.UnitCount })
            .ToList(),
        CreatedAt = @event.CreatedAt,
        StructureGeneratedAt = @event.StructureGeneratedAt
    };

    public static Event ToDomain(EventDocument doc)
    {
        var rsc = Rsc.Create(doc.Id);
        var gender = Gender.Create(doc.Gender);
        var evt = Event.Create(rsc, doc.Discipline, gender, doc.EventCode, doc.Modifier, doc.Name);

        if (doc.Format is not null && doc.Size.HasValue)
        {
            EventHydration.HydrateStructure(
                evt,
                Enum.Parse<CompetitionFormat>(doc.Format),
                doc.Size.Value,
                doc.Phases.Select(p => (p.Code, p.Order, p.UnitCount)).ToList(),
                doc.StructureGeneratedAt!.Value);
        }

        return evt;
    }
}
```

- [ ] **Step 3: Add hydration helper inside the CompetitionConfig.Domain namespace**

We need a way to rehydrate an `Event` from storage without going through the generation flow. Add a static helper that uses `InternalsVisibleTo` or a dedicated method. Simplest: add an internal static method on `Event` itself.

Replace the contents of `src/OVR.Modules.CompetitionConfig/Domain/Event.cs` by appending the following method (inside the `Event` class, just before the closing brace):

```csharp
    internal void HydrateFromStorage(
        CompetitionFormat format,
        int size,
        IReadOnlyList<(string Code, int Order, int UnitCount)> phases,
        DateTime structureGeneratedAt)
    {
        Format = format;
        Size = size;
        _phases.Clear();
        _phases.AddRange(phases.Select(p => Phase.CreateInternal(p.Code, p.Order, p.UnitCount)));
        StructureGeneratedAt = structureGeneratedAt;
    }
```

And create `src/OVR.Modules.CompetitionConfig/Domain/EventHydration.cs`:

```csharp
namespace OVR.Modules.CompetitionConfig.Domain;

internal static class EventHydration
{
    public static void HydrateStructure(
        Event @event,
        CompetitionFormat format,
        int size,
        IReadOnlyList<(string Code, int Order, int UnitCount)> phases,
        DateTime structureGeneratedAt)
    {
        @event.HydrateFromStorage(format, size, phases, structureGeneratedAt);
    }
}
```

This separates hydration from creation while keeping all writes to the aggregate private.

- [ ] **Step 4: Create `IEventRepository.cs`**

```csharp
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Persistence;

public interface IEventRepository
{
    Task<Event?> GetByRscAsync(string eventRsc, CancellationToken ct = default);
    Task AddAsync(Event @event, CancellationToken ct = default);
    Task UpdateAsync(Event @event, CancellationToken ct = default);
}
```

- [ ] **Step 5: Create `MongoEventRepository.cs`**

```csharp
using MongoDB.Driver;
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Persistence;

internal sealed class MongoEventRepository(IMongoDatabase database) : IEventRepository
{
    private IMongoCollection<EventDocument> Collection =>
        database.GetCollection<EventDocument>("competitionconfig_events");

    public async Task<Event?> GetByRscAsync(string eventRsc, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == eventRsc).FirstOrDefaultAsync(ct);
        return doc is null ? null : EventMapping.ToDomain(doc);
    }

    public async Task AddAsync(Event @event, CancellationToken ct = default)
    {
        var doc = EventMapping.ToDocument(@event);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Event @event, CancellationToken ct = default)
    {
        var doc = EventMapping.ToDocument(@event);
        await Collection.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
    }
}
```

- [ ] **Step 6: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 7: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Persistence/ src/OVR.Modules.CompetitionConfig/Domain/Event.cs src/OVR.Modules.CompetitionConfig/Domain/EventHydration.cs
git commit -m "feat(competitionconfig): add EventDocument, mapping, and Mongo repository"
```

---

## Task 13: Persistence — `UnitDocument`, `UnitMapping`, `IUnitRepository`

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/UnitDocument.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/UnitMapping.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/IUnitRepository.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Persistence/MongoUnitRepository.cs`

- [ ] **Step 1: Create `UnitDocument.cs`**

```csharp
using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.CompetitionConfig.Persistence;

public sealed class UnitDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string EventRsc { get; set; } = string.Empty;
    public string PhaseCode { get; set; } = string.Empty;
    public int UnitNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 2: Create `UnitMapping.cs`**

```csharp
using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Persistence;

internal static class UnitMapping
{
    public static UnitDocument ToDocument(Unit unit) => new()
    {
        Id = unit.Id,
        EventRsc = unit.EventRsc.Value,
        PhaseCode = unit.PhaseCode,
        UnitNumber = unit.UnitNumber,
        CreatedAt = unit.CreatedAt
    };

    public static Unit ToDomain(UnitDocument doc)
    {
        var rsc = Rsc.Create(doc.Id);
        return Unit.Create(rsc);
    }
}
```

- [ ] **Step 3: Create `IUnitRepository.cs`**

```csharp
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Persistence;

public interface IUnitRepository
{
    Task<Unit?> GetByRscAsync(string unitRsc, CancellationToken ct = default);
    Task<IReadOnlyList<Unit>> ListByEventAsync(string eventRsc, CancellationToken ct = default);
    Task AddManyAsync(IEnumerable<Unit> units, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create `MongoUnitRepository.cs`**

```csharp
using MongoDB.Driver;
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Persistence;

internal sealed class MongoUnitRepository(IMongoDatabase database) : IUnitRepository
{
    private IMongoCollection<UnitDocument> Collection =>
        database.GetCollection<UnitDocument>("competitionconfig_units");

    public async Task<Unit?> GetByRscAsync(string unitRsc, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == unitRsc).FirstOrDefaultAsync(ct);
        return doc is null ? null : UnitMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<Unit>> ListByEventAsync(string eventRsc, CancellationToken ct = default)
    {
        var docs = await Collection
            .Find(d => d.EventRsc == eventRsc)
            .SortBy(d => d.PhaseCode).ThenBy(d => d.UnitNumber)
            .ToListAsync(ct);
        return docs.Select(UnitMapping.ToDomain).ToList();
    }

    public async Task AddManyAsync(IEnumerable<Unit> units, CancellationToken ct = default)
    {
        var docs = units.Select(UnitMapping.ToDocument).ToList();
        if (docs.Count == 0) return;
        await Collection.InsertManyAsync(
            docs,
            new InsertManyOptions { IsOrdered = false },
            ct);
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 6: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Persistence/
git commit -m "feat(competitionconfig): add Unit persistence layer"
```

---

## Task 14: `CreateEvent` feature — command + validator

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Features/CreateEvent/CreateEventCommand.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Features/CreateEvent/CreateEventValidator.cs`

- [ ] **Step 1: Create `CreateEventCommand.cs`**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.CompetitionConfig.Features.CreateEvent;

public sealed record CreateEventCommand(
    string Discipline,
    string Gender,
    string EventCode,
    string? Modifier,
    string Name) : IRequest<ErrorOr<CreateEventResponse>>;

public sealed record CreateEventResponse(
    string Rsc,
    string Name,
    DateTime CreatedAt);
```

- [ ] **Step 2: Create `CreateEventValidator.cs`**

```csharp
using FluentValidation;

namespace OVR.Modules.CompetitionConfig.Features.CreateEvent;

public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Discipline)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Discipline must be 3 uppercase letters.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .Must(g => g is "M" or "W" or "X")
            .WithMessage("Gender must be M, W, or X.");

        RuleFor(x => x.EventCode)
            .NotEmpty()
            .Length(1, 8)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("EventCode must be 1..8 uppercase alphanumeric chars.");

        RuleFor(x => x.Modifier)
            .Length(1, 10)
            .Matches("^[A-Z0-9]+$")
            .When(x => x.Modifier is not null)
            .WithMessage("Modifier must be 1..10 uppercase alphanumeric chars when provided.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(1, 80);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Features/CreateEvent/CreateEventCommand.cs src/OVR.Modules.CompetitionConfig/Features/CreateEvent/CreateEventValidator.cs
git commit -m "feat(competitionconfig): add CreateEvent command and validator"
```

---

## Task 15: `CreateEvent` handler — failing tests + implementation

**Files:**
- Create: `tests/OVR.Modules.CompetitionConfig.Tests/Features/CreateEvent/CreateEventHandlerTests.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Features/CreateEvent/CreateEventHandler.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using MediatR;
using NSubstitute;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Features.CreateEvent;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.SharedKernel.Contracts;

namespace OVR.Modules.CompetitionConfig.Tests.Features.CreateEvent;

public class CreateEventHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ICommonCodeCache _cache = Substitute.For<ICommonCodeCache>();
    private readonly CreateEventHandler _handler;

    public CreateEventHandlerTests()
    {
        _handler = new CreateEventHandler(_repository, _publisher, _cache);
    }

    private void SetupValidCodes()
    {
        _cache.Exists(CommonCodeTypes.Discipline, "BOX").Returns(true);
        _cache.Exists(CommonCodeTypes.Event, "57KG").Returns(true);
    }

    private static CreateEventCommand ValidCommand() =>
        new("BOX", "M", "57KG", null, "Men's 57kg");

    [Fact]
    public async Task Handle_ValidCommand_Returns201LikeResponseAndPersists()
    {
        SetupValidCodes();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Rsc.Should().Be("BOXM57KG--------------------------");
        result.Value.Name.Should().Be("Men's 57kg");
        await _repository.Received(1).AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidDiscipline_ReturnsError()
    {
        _cache.Exists(CommonCodeTypes.Discipline, "BOX").Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.InvalidDiscipline");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidEventCode_ReturnsError()
    {
        _cache.Exists(CommonCodeTypes.Discipline, "BOX").Returns(true);
        _cache.Exists(CommonCodeTypes.Event, "57KG").Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.InvalidEventCode");
    }

    [Fact]
    public async Task Handle_DuplicateRsc_ReturnsConflict()
    {
        SetupValidCodes();
        var existing = Event.Create(
            OVR.SharedKernel.Domain.ValueObjects.Rsc.Create("BOXM57KG--------------------------"),
            "BOX",
            OVR.SharedKernel.Domain.ValueObjects.Gender.Create("M"),
            "57KG",
            null,
            "Men's 57kg");
        _repository.GetByRscAsync("BOXM57KG--------------------------", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.EventAlreadyExists");
    }
}
```

- [ ] **Step 2: Run to verify fail (compile error)**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~CreateEventHandlerTests"`
Expected: compile error — `CreateEventHandler` not found.

- [ ] **Step 3: Implement `CreateEventHandler.cs`**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Errors;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.SharedKernel.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Features.CreateEvent;

public sealed class CreateEventHandler(
    IEventRepository repository,
    IPublisher publisher,
    ICommonCodeCache cache)
    : IRequestHandler<CreateEventCommand, ErrorOr<CreateEventResponse>>
{
    public async Task<ErrorOr<CreateEventResponse>> Handle(
        CreateEventCommand request,
        CancellationToken ct)
    {
        if (!cache.Exists(CommonCodeTypes.Discipline, request.Discipline))
            return CompetitionConfigErrors.InvalidDiscipline(request.Discipline);

        if (!cache.Exists(CommonCodeTypes.Event, request.EventCode))
            return CompetitionConfigErrors.InvalidEventCode(request.EventCode);

        var rscString =
            request.Discipline
            + request.Gender
            + request.EventCode.PadRight(8, '-')
            + (request.Modifier?.PadRight(10, '-') ?? new string('-', 10))
            + new string('-', 12);

        var rsc = Rsc.Create(rscString);
        var gender = Gender.Create(request.Gender);

        var existing = await repository.GetByRscAsync(rsc.Value, ct);
        if (existing is not null)
            return CompetitionConfigErrors.EventAlreadyExists(rsc.Value);

        var evt = Event.Create(rsc, request.Discipline, gender, request.EventCode, request.Modifier, request.Name);
        await repository.AddAsync(evt, ct);

        foreach (var e in evt.DomainEvents)
            await publisher.Publish(e, ct);
        evt.ClearDomainEvents();

        return new CreateEventResponse(evt.Id, evt.Name, evt.CreatedAt);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~CreateEventHandlerTests"`
Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Features/CreateEvent/CreateEventHandler.cs tests/OVR.Modules.CompetitionConfig.Tests/Features/CreateEvent/
git commit -m "feat(competitionconfig): implement CreateEvent handler"
```

---

## Task 16: `CreateEvent` endpoint

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Features/CreateEvent/CreateEventEndpoint.cs`

- [ ] **Step 1: Create endpoint**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Api;

namespace OVR.Modules.CompetitionConfig.Features.CreateEvent;

public static class CreateEventEndpoint
{
    public static async Task<IResult> Handle(
        CreateEventCommand command,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult($"/api/competition-config/events/{result.Value?.Rsc}", httpContext);
    }
}
```

Note: `OVR.SharedKernel.Api` should provide `ToCreatedResult`. If the exact namespace differs, check `CreateEntryEndpoint.cs` for reference and use the same using.

- [ ] **Step 2: Verify extension method namespace**

Run: `grep -rn "ToCreatedResult\|ToApiResult" src/OVR.SharedKernel/`
If the namespace is different from `OVR.SharedKernel.Api`, update the `using` in step 1.

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Features/CreateEvent/CreateEventEndpoint.cs
git commit -m "feat(competitionconfig): add CreateEvent endpoint"
```

---

## Task 17: `GenerateEventStructure` feature — command + validator

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Features/GenerateEventStructure/GenerateEventStructureCommand.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Features/GenerateEventStructure/GenerateEventStructureValidator.cs`

- [ ] **Step 1: Create `GenerateEventStructureCommand.cs`**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;

public sealed record GenerateEventStructureCommand(
    string EventRsc,
    string Format,
    int Size,
    int StartUnitNumber = 1) : IRequest<ErrorOr<GenerateEventStructureResponse>>;

public sealed record GenerateEventStructureResponse(
    string EventRsc,
    string Format,
    int Size,
    IReadOnlyList<GenerateEventStructurePhase> Phases,
    IReadOnlyList<string> UnitRscs);

public sealed record GenerateEventStructurePhase(
    string Code,
    int Order,
    int UnitCount);
```

- [ ] **Step 2: Create `GenerateEventStructureValidator.cs`**

```csharp
using FluentValidation;

namespace OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;

public sealed class GenerateEventStructureValidator : AbstractValidator<GenerateEventStructureCommand>
{
    public GenerateEventStructureValidator()
    {
        RuleFor(x => x.EventRsc)
            .NotEmpty()
            .Length(34);

        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => f is "SingleElimination")
            .WithMessage("Format must be SingleElimination in MVP.");

        RuleFor(x => x.Size)
            .InclusiveBetween(2, 128);

        RuleFor(x => x.StartUnitNumber)
            .InclusiveBetween(1, 9999);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Features/GenerateEventStructure/
git commit -m "feat(competitionconfig): add GenerateEventStructure command and validator"
```

---

## Task 18: `GenerateEventStructure` handler — failing tests + implementation

**Files:**
- Create: `tests/OVR.Modules.CompetitionConfig.Tests/Features/GenerateEventStructure/GenerateEventStructureHandlerTests.cs`
- Create: `src/OVR.Modules.CompetitionConfig/Features/GenerateEventStructure/GenerateEventStructureHandler.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using MediatR;
using NSubstitute;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Tests.Features.GenerateEventStructure;

public class GenerateEventStructureHandlerTests
{
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IUnitRepository _unitRepo = Substitute.For<IUnitRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly BracketGenerator _generator = new();
    private readonly GenerateEventStructureHandler _handler;

    public GenerateEventStructureHandlerTests()
    {
        _handler = new GenerateEventStructureHandler(_eventRepo, _unitRepo, _publisher, _generator);
    }

    private Event ExistingEventWithoutStructure()
    {
        return Event.Create(
            Rsc.Create("BOXM57KG--------------------------"),
            "BOX",
            Gender.Create("M"),
            "57KG",
            null,
            "Men's 57kg");
    }

    [Fact]
    public async Task Handle_ValidRequest_Returns15UnitsAndPublishesEvent()
    {
        var evt = ExistingEventWithoutStructure();
        _eventRepo.GetByRscAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(evt);

        var result = await _handler.Handle(
            new GenerateEventStructureCommand(evt.Id, "SingleElimination", 16),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.UnitRscs.Should().HaveCount(15);
        result.Value.Phases.Should().HaveCount(4);
        await _unitRepo.Received(1).AddManyAsync(Arg.Is<IEnumerable<Unit>>(u => u.Count() == 15), Arg.Any<CancellationToken>());
        await _eventRepo.Received(1).UpdateAsync(evt, Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(Arg.Any<EventStructureGeneratedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EventNotFound_Returns404Error()
    {
        _eventRepo.GetByRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Event?)null);

        var result = await _handler.Handle(
            new GenerateEventStructureCommand("BOXM99KG--------------------------", "SingleElimination", 16),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.EventNotFound");
    }

    [Fact]
    public async Task Handle_StructureAlreadyGenerated_Returns409Error()
    {
        var evt = ExistingEventWithoutStructure();
        evt.GenerateStructure(CompetitionFormat.SingleElimination, 4, 1, _generator);
        evt.ClearDomainEvents();
        _eventRepo.GetByRscAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(evt);

        var result = await _handler.Handle(
            new GenerateEventStructureCommand(evt.Id, "SingleElimination", 16),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CompetitionConfig.StructureAlreadyGenerated");
    }

    [Fact]
    public async Task Handle_Size13_RoundsUpAndReturns15Units()
    {
        var evt = ExistingEventWithoutStructure();
        _eventRepo.GetByRscAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(evt);

        var result = await _handler.Handle(
            new GenerateEventStructureCommand(evt.Id, "SingleElimination", 13),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.UnitRscs.Should().HaveCount(15);
        result.Value.Size.Should().Be(13);
    }
}
```

- [ ] **Step 2: Verify tests fail**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~GenerateEventStructureHandlerTests"`
Expected: compile error.

- [ ] **Step 3: Implement `GenerateEventStructureHandler.cs`**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Errors;
using OVR.Modules.CompetitionConfig.Persistence;

namespace OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;

public sealed class GenerateEventStructureHandler(
    IEventRepository eventRepository,
    IUnitRepository unitRepository,
    IPublisher publisher,
    BracketGenerator generator)
    : IRequestHandler<GenerateEventStructureCommand, ErrorOr<GenerateEventStructureResponse>>
{
    public async Task<ErrorOr<GenerateEventStructureResponse>> Handle(
        GenerateEventStructureCommand request,
        CancellationToken ct)
    {
        var evt = await eventRepository.GetByRscAsync(request.EventRsc, ct);
        if (evt is null)
            return CompetitionConfigErrors.EventNotFound(request.EventRsc);

        if (!Enum.TryParse<CompetitionFormat>(request.Format, out var format))
            return CompetitionConfigErrors.UnsupportedFormat(request.Format);

        var structureResult = evt.GenerateStructure(format, request.Size, request.StartUnitNumber, generator);
        if (structureResult.IsError)
            return structureResult.Errors;

        var units = structureResult.Value.Select(Unit.Create).ToList();
        await unitRepository.AddManyAsync(units, ct);
        await eventRepository.UpdateAsync(evt, ct);

        foreach (var e in evt.DomainEvents)
            await publisher.Publish(e, ct);
        evt.ClearDomainEvents();

        return new GenerateEventStructureResponse(
            EventRsc: evt.Id,
            Format: evt.Format!.Value.ToString(),
            Size: evt.Size!.Value,
            Phases: evt.Phases
                .Select(p => new GenerateEventStructurePhase(p.Code, p.Order, p.UnitCount))
                .ToList(),
            UnitRscs: structureResult.Value.Select(r => r.Value).ToList());
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/OVR.Modules.CompetitionConfig.Tests/ --filter "FullyQualifiedName~GenerateEventStructureHandlerTests"`
Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Features/GenerateEventStructure/GenerateEventStructureHandler.cs tests/OVR.Modules.CompetitionConfig.Tests/Features/GenerateEventStructure/
git commit -m "feat(competitionconfig): implement GenerateEventStructure handler"
```

---

## Task 19: `GenerateEventStructure` endpoint

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/Features/GenerateEventStructure/GenerateEventStructureEndpoint.cs`

- [ ] **Step 1: Create endpoint**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Api;

namespace OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;

public static class GenerateEventStructureEndpoint
{
    public static async Task<IResult> Handle(
        string rsc,
        GenerateEventStructureBody body,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var command = new GenerateEventStructureCommand(
            rsc, body.Format, body.Size, body.StartUnitNumber);

        var result = await sender.Send(command, ct);
        return result.ToApiResult(httpContext);
    }
}

public sealed record GenerateEventStructureBody(
    string Format,
    int Size,
    int StartUnitNumber = 1);
```

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/Features/GenerateEventStructure/GenerateEventStructureEndpoint.cs
git commit -m "feat(competitionconfig): add GenerateEventStructure endpoint"
```

---

## Task 20: I18n files

**Files:**
- Create: `src/OVR.Modules.CompetitionConfig/I18n/eng.json`
- Create: `src/OVR.Modules.CompetitionConfig/I18n/spa.json`
- Create: `src/OVR.Modules.CompetitionConfig/I18n/por.json`

- [ ] **Step 1: Create `eng.json`**

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

- [ ] **Step 2: Create `spa.json`**

```json
{
  "CompetitionConfig.InvalidDiscipline": "La disciplina '{{discipline}}' no está registrada.",
  "CompetitionConfig.InvalidEventCode": "El código de evento '{{eventCode}}' no está registrado.",
  "CompetitionConfig.EventAlreadyExists": "Ya existe un evento con RSC '{{rsc}}'.",
  "CompetitionConfig.EventNotFound": "El evento '{{rsc}}' no fue encontrado.",
  "CompetitionConfig.StructureAlreadyGenerated": "La estructura para el evento '{{rsc}}' ya fue generada.",
  "CompetitionConfig.UnsupportedFormat": "El formato de competición '{{format}}' no es soportado.",
  "CompetitionConfig.InvalidSize": "El tamaño {{size}} está fuera del rango (2..128)."
}
```

- [ ] **Step 3: Create `por.json`**

```json
{
  "CompetitionConfig.InvalidDiscipline": "A disciplina '{{discipline}}' não está registrada.",
  "CompetitionConfig.InvalidEventCode": "O código de evento '{{eventCode}}' não está registrado.",
  "CompetitionConfig.EventAlreadyExists": "Já existe um evento com RSC '{{rsc}}'.",
  "CompetitionConfig.EventNotFound": "O evento '{{rsc}}' não foi encontrado.",
  "CompetitionConfig.StructureAlreadyGenerated": "A estrutura do evento '{{rsc}}' já foi gerada.",
  "CompetitionConfig.UnsupportedFormat": "O formato de competição '{{format}}' não é suportado.",
  "CompetitionConfig.InvalidSize": "O tamanho {{size}} está fora do intervalo (2..128)."
}
```

- [ ] **Step 4: Build (to confirm Content Include in csproj picks them up)**

Run: `dotnet build src/OVR.Modules.CompetitionConfig/`
Expected: succeeds and output folder contains `I18n.CompetitionConfig/{eng,spa,por}.json`.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/I18n/
git commit -m "feat(competitionconfig): add i18n translations for all error messages"
```

---

## Task 21: Wire up `CompetitionConfigModule`

**Files:**
- Modify: `src/OVR.Modules.CompetitionConfig/CompetitionConfigModule.cs`

- [ ] **Step 1: Replace `CompetitionConfigModule.cs` contents**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Features.CreateEvent;
using OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;
using OVR.Modules.CompetitionConfig.Persistence;

namespace OVR.Modules.CompetitionConfig;

public static class CompetitionConfigModule
{
    public static IServiceCollection AddCompetitionConfigModule(this IServiceCollection services)
    {
        services.AddScoped<IEventRepository, MongoEventRepository>();
        services.AddScoped<IUnitRepository, MongoUnitRepository>();
        services.AddSingleton<BracketGenerator>();
        return services;
    }

    public static IEndpointRouteBuilder MapCompetitionConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/competition-config")
            .WithTags("CompetitionConfig");

        group.MapPost("/events", CreateEventEndpoint.Handle)
            .WithName("CreateEvent");

        group.MapPost("/events/{rsc}/generate-structure", GenerateEventStructureEndpoint.Handle)
            .WithName("GenerateEventStructure");

        return app;
    }
}
```

- [ ] **Step 2: Confirm Program.cs registers MediatR + FluentValidation for this module**

Run: `grep -n "CompetitionConfigModule" src/OVR.Api/Program.cs`
Expected output shows `AddCompetitionConfigModule()`, `MapCompetitionConfigEndpoints()`, and the module's assembly listed among MediatR and FluentValidation assemblies. The code already references `typeof(CompetitionConfigModule).Assembly` in Program.cs, so no change should be needed — just verify.

If FluentValidation registration is missing the assembly, add it next to the others in the `AddValidatorsFromAssemblies` call.

- [ ] **Step 3: Build + run solution**

```bash
dotnet build
```

Expected: succeeds.

- [ ] **Step 4: Run a smoke test**

Start MongoDB: `docker compose --profile db up -d`

Run API: `dotnet run --project src/OVR.Api`

Make a manual test request:

```bash
curl -X POST http://localhost:5000/api/competition-config/events \
  -H 'Content-Type: application/json' \
  -d '{"discipline":"BOX","gender":"M","eventCode":"57KG","modifier":null,"name":"Men 57kg"}'
```

Expected: `201 Created` with a Location header and body `{rsc, name, createdAt}`.

Note: this may return 400 if `BOX` and `57KG` aren't in CC yet. Seed them or accept the error as expected behavior — the test confirms the pipeline is wired.

Stop the API (Ctrl+C).

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.CompetitionConfig/CompetitionConfigModule.cs
git commit -m "feat(competitionconfig): wire up module DI and endpoints"
```

---

## Task 22: Integration tests — setup `CompetitionConfigWebAppFactory`

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/CompetitionConfig/Support/CompetitionConfigWebAppFactory.cs`

- [ ] **Step 1: Create the fixture/factory**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OVR.Modules.CommonCodes.Persistence;
using Testcontainers.MongoDb;

namespace OVR.Api.IntegrationTests.CompetitionConfig.Support;

public sealed class CompetitionConfigWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:8")
        .Build();

    public string ConnectionString => _mongo.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _mongo.StartAsync();
        await SeedCommonCodesAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongo.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = _mongo.GetConnectionString(),
                ["Mongo:Database"] = "ovr_integration_tests"
            });
        });
    }

    private async Task SeedCommonCodesAsync()
    {
        var client = new MongoClient(_mongo.GetConnectionString());
        var db = client.GetDatabase("ovr_integration_tests");
        var collection = db.GetCollection<CommonCodeDocument>("common_codes");

        var seed = new List<CommonCodeDocument>
        {
            new() { Id = "DISCIPLINE:BOX", Type = "DISCIPLINE", Code = "BOX", Order = 1,
                Name = new() { ["eng"] = new() { Long = "Boxing" } }, Attributes = [] },
            new() { Id = "EVENT:57KG", Type = "EVENT", Code = "57KG", Order = 1,
                Name = new() { ["eng"] = new() { Long = "Men's 57kg" } }, Attributes = [] },
        };

        await collection.InsertManyAsync(seed);
    }
}
```

- [ ] **Step 2: Verify `Program.cs` reads `Mongo:ConnectionString` / `Mongo:Database`**

Run: `grep -n "Mongo:ConnectionString\|IMongoClient\|IMongoDatabase" src/OVR.Api/Program.cs`
If the config keys differ, update the `ConfigureAppConfiguration` in the factory accordingly.

- [ ] **Step 3: Verify `Program` is `public partial class Program { }` (required for WebApplicationFactory<Program>)**

Run: `grep -n "public partial class Program" src/OVR.Api/Program.cs`
If missing, add at the end of Program.cs:

```csharp
public partial class Program { }
```

- [ ] **Step 4: Build**

Run: `dotnet build tests/OVR.Api.IntegrationTests/`
Expected: succeeds.

- [ ] **Step 5: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/CompetitionConfig/
git commit -m "test(competitionconfig): add WebAppFactory for integration tests"
```

---

## Task 23: Integration tests — `CreateEventEndpointTests`

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/CompetitionConfig/CreateEventEndpointTests.cs`

- [ ] **Step 1: Write the integration tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.CompetitionConfig.Support;

namespace OVR.Api.IntegrationTests.CompetitionConfig;

public class CreateEventEndpointTests : IClassFixture<CompetitionConfigWebAppFactory>
{
    private readonly HttpClient _client;

    public CreateEventEndpointTests(CompetitionConfigWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_ValidPayload_Returns201()
    {
        var body = new
        {
            discipline = "BOX",
            gender = "M",
            eventCode = "57KG",
            modifier = (string?)null,
            name = "Men's 57kg"
        };

        var response = await _client.PostAsJsonAsync("/api/competition-config/events", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString()
            .Should().Contain("BOXM57KG");
    }

    [Fact]
    public async Task POST_UnknownDiscipline_Returns400()
    {
        var body = new
        {
            discipline = "ZZZ",
            gender = "M",
            eventCode = "57KG",
            modifier = (string?)null,
            name = "Men's 57kg"
        };

        var response = await _client.PostAsJsonAsync("/api/competition-config/events", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body2 = await response.Content.ReadAsStringAsync();
        body2.Should().Contain("CompetitionConfig.InvalidDiscipline");
    }

    [Fact]
    public async Task POST_DuplicateRsc_Returns409()
    {
        var body = new
        {
            discipline = "BOX",
            gender = "M",
            eventCode = "57KG",
            modifier = (string?)null,
            name = "Men's 57kg (duplicate)"
        };

        await _client.PostAsJsonAsync("/api/competition-config/events", body);
        var second = await _client.PostAsJsonAsync("/api/competition-config/events", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_MissingGender_Returns400FromValidator()
    {
        var body = new
        {
            discipline = "BOX",
            gender = "",
            eventCode = "57KG",
            modifier = (string?)null,
            name = "x"
        };

        var response = await _client.PostAsJsonAsync("/api/competition-config/events", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

Note: Tests assume each `IClassFixture<CompetitionConfigWebAppFactory>` gets a fresh fixture; if duplicates interfere, add a cleanup step in test setup.

- [ ] **Step 2: Run integration tests**

Run: `dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~CreateEventEndpointTests"`
Expected: 4 tests pass (may take ~30s for Mongo container startup).

- [ ] **Step 3: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/CompetitionConfig/CreateEventEndpointTests.cs
git commit -m "test(competitionconfig): add integration tests for CreateEvent endpoint"
```

---

## Task 24: Integration tests — `GenerateEventStructureEndpointTests`

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/CompetitionConfig/GenerateEventStructureEndpointTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.CompetitionConfig.Support;

namespace OVR.Api.IntegrationTests.CompetitionConfig;

public class GenerateEventStructureEndpointTests : IClassFixture<CompetitionConfigWebAppFactory>
{
    private readonly HttpClient _client;

    public GenerateEventStructureEndpointTests(CompetitionConfigWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> CreateBoxEventAsync(string eventCode = "57KG")
    {
        var body = new { discipline = "BOX", gender = "M", eventCode, modifier = (string?)null, name = $"Men's {eventCode}" };
        var response = await _client.PostAsJsonAsync("/api/competition-config/events", body);
        response.EnsureSuccessStatusCode();
        return response.Headers.Location!.Segments.Last();
    }

    [Fact]
    public async Task POST_Size16_Returns200And15Units()
    {
        var rsc = await CreateBoxEventAsync("57KG");
        var body = new { format = "SingleElimination", size = 16, startUnitNumber = 1 };

        var response = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{rsc}/generate-structure", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("\"size\":16");
        payload.Should().Contain("8FNL0001----");
        payload.Should().Contain("FNL-0015----");
    }

    [Fact]
    public async Task POST_Size13_Returns200And15Units()
    {
        var rsc = await CreateBoxEventAsync("60KG");
        var body = new { format = "SingleElimination", size = 13, startUnitNumber = 1 };

        var response = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{rsc}/generate-structure", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("\"size\":13");
    }

    [Fact]
    public async Task POST_AlreadyGenerated_Returns409()
    {
        var rsc = await CreateBoxEventAsync("63KG");
        var body = new { format = "SingleElimination", size = 8, startUnitNumber = 1 };
        await _client.PostAsJsonAsync($"/api/competition-config/events/{rsc}/generate-structure", body);

        var second = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{rsc}/generate-structure", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_MissingEvent_Returns404()
    {
        var body = new { format = "SingleElimination", size = 8, startUnitNumber = 1 };
        var fakeRsc = "BOXM99KG--------------------------";

        var response = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{fakeRsc}/generate-structure", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Size1_Returns400FromValidator()
    {
        var rsc = await CreateBoxEventAsync("66KG");
        var body = new { format = "SingleElimination", size = 1, startUnitNumber = 1 };

        var response = await _client.PostAsJsonAsync(
            $"/api/competition-config/events/{rsc}/generate-structure", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

Note: each test creates a new Event with a unique eventCode to avoid cross-test interference. Seed the `57KG`, `60KG`, `63KG`, `66KG` codes in the factory's `SeedCommonCodesAsync`:

Update `CompetitionConfigWebAppFactory.SeedCommonCodesAsync` to seed all these event codes:

```csharp
new() { Id = "EVENT:57KG", Type = "EVENT", Code = "57KG", Order = 1,
    Name = new() { ["eng"] = new() { Long = "Men's 57kg" } }, Attributes = [] },
new() { Id = "EVENT:60KG", Type = "EVENT", Code = "60KG", Order = 2,
    Name = new() { ["eng"] = new() { Long = "Men's 60kg" } }, Attributes = [] },
new() { Id = "EVENT:63KG", Type = "EVENT", Code = "63KG", Order = 3,
    Name = new() { ["eng"] = new() { Long = "Men's 63kg" } }, Attributes = [] },
new() { Id = "EVENT:66KG", Type = "EVENT", Code = "66KG", Order = 4,
    Name = new() { ["eng"] = new() { Long = "Men's 66kg" } }, Attributes = [] },
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~GenerateEventStructureEndpointTests"`
Expected: 5 tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/CompetitionConfig/
git commit -m "test(competitionconfig): add integration tests for GenerateEventStructure endpoint"
```

---

## Task 25: Final verification

- [ ] **Step 1: Run full test suite**

Run: `dotnet test`
Expected: all tests pass, including the pre-existing suites.

- [ ] **Step 2: Smoke test the full flow via API**

Start dependencies:
```bash
docker compose --profile db up -d
dotnet run --project src/OVR.Api &
sleep 5
```

Seed BOX and 57KG in common codes (manually via Mongo shell or via the import endpoint if available). Then:

```bash
# 1) Create event
curl -i -X POST http://localhost:5000/api/competition-config/events \
  -H 'Content-Type: application/json' \
  -d '{"discipline":"BOX","gender":"M","eventCode":"57KG","modifier":null,"name":"Men 57kg"}'

# 2) Generate structure for 16 entries
RSC='BOXM57KG--------------------------'
curl -i -X POST "http://localhost:5000/api/competition-config/events/${RSC}/generate-structure" \
  -H 'Content-Type: application/json' \
  -d '{"format":"SingleElimination","size":16,"startUnitNumber":1}'
```

Expected: 201 then 200 with 4 phases and 15 unit RSCs.

Kill the API: `kill %1`

- [ ] **Step 3: Final commit (if anything was patched during verification)**

```bash
git status
# if anything changed:
git add -A
git commit -m "chore: final verification tweaks"
```

- [ ] **Step 4: Merge ready — the CompetitionConfig MVP is complete.**

---

## Self-Review Checklist (executed while writing the plan)

**1. Spec coverage:**

| Spec section | Task(s) |
|---|---|
| Delete `Discipline.cs` | Task 1 |
| `CompetitionFormat` enum | Task 2 |
| `PhaseCodes` constants | Task 2 |
| `BracketGenerator` | Tasks 4, 5 |
| `Phase` entity | Task 6 |
| `Unit` aggregate | Task 7 |
| `EventStructureGeneratedEvent` | Task 8 |
| `Event` aggregate expansion | Tasks 9, 11 |
| `CompetitionConfigErrors` | Task 10 |
| Event persistence | Task 12 |
| Unit persistence | Task 13 |
| `CreateEvent` command/validator | Task 14 |
| `CreateEvent` handler | Task 15 |
| `CreateEvent` endpoint | Task 16 |
| `GenerateEventStructure` command/validator | Task 17 |
| `GenerateEventStructure` handler | Task 18 |
| `GenerateEventStructure` endpoint | Task 19 |
| I18n files | Task 20 |
| Module DI + endpoint mapping | Task 21 |
| Integration tests fixture | Task 22 |
| CreateEvent integration tests | Task 23 |
| GenerateEventStructure integration tests | Task 24 |
| Final verification | Task 25 |

All spec sections are covered.

**2. Placeholder scan:** No "TBD", "TODO", "similar to X" patterns. All code blocks contain real, complete code.

**3. Type consistency:** Names are consistent across tasks — `Event`, `Phase`, `Unit`, `BracketGenerator`, `BracketPlan`, `PhaseSpec`, `CompetitionFormat`, `PhaseCodes`, `EventStructureGeneratedEvent`, `PhaseInfo`, `IEventRepository`, `IUnitRepository`, `MongoEventRepository`, `MongoUnitRepository`, `CreateEventCommand`, `CreateEventHandler`, `CreateEventEndpoint`, `CreateEventResponse`, `GenerateEventStructureCommand`, `GenerateEventStructureHandler`, `GenerateEventStructureEndpoint`, `GenerateEventStructureResponse`, `GenerateEventStructurePhase`, `GenerateEventStructureBody`, `CompetitionConfigErrors`, `CommonCodeTypes.Event`, `CompetitionConfigWebAppFactory`.

**4. Deliverable check:** After Task 25 the operator can `POST /api/competition-config/events` and `POST /api/competition-config/events/{rsc}/generate-structure` as specified. Integration tests verify end-to-end with real Mongo. Domain event `EventStructureGeneratedEvent` is dispatched via MediatR and available for downstream modules in MVP 2+.
