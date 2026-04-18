# Scheduling MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build Scheduling module MVP exposing 5 endpoints: create Sessions, schedule/reschedule/unschedule Units, and query units scheduled at a location+date. Emit 3 integration events for downstream modules (DataEntry/DataDistribution in future MVPs).

**Architecture:** Vertical-slice module with `Session` aggregate (metadata-only, identified by SessionCode) and `UnitSchedule` aggregate (identified by UnitRsc), `ScheduleCollisionDetector` domain service enforcing `(LocationCode, StartTime)` uniqueness, two MongoDB collections (`scheduling_sessions`, `scheduling_unit_schedules`), three integration events (`UnitScheduledEvent`, `UnitScheduleChangedEvent`, `UnitUnscheduledEvent`) on SharedKernel.

**Tech Stack:** .NET 10, C# 14, MediatR 12.4, FluentValidation 11.11, ErrorOr 2.0, MongoDB.Driver 3.4, xUnit + FluentAssertions + NSubstitute + Testcontainers.MongoDb.

**Spec reference:** `docs/superpowers/specs/2026-04-17-scheduling-mvp-design.md`

**Starting branch:** `feat/scheduling-mvp` (to be created from `main` before Task 1).

---

## File Structure Map

### New files

```
src/OVR.Modules.Scheduling/
├── Domain/
│   ├── Session.cs                        # aggregate (metadata)
│   ├── UnitSchedule.cs                   # aggregate (assignment)
│   ├── ScheduleStatus.cs                 # enum
│   └── ScheduleCollisionDetector.cs      # domain service + interface
├── Features/
│   ├── CreateSession/
│   │   ├── CreateSessionCommand.cs
│   │   ├── CreateSessionValidator.cs
│   │   ├── CreateSessionHandler.cs
│   │   └── CreateSessionEndpoint.cs
│   ├── ScheduleUnit/
│   │   ├── ScheduleUnitCommand.cs
│   │   ├── ScheduleUnitValidator.cs
│   │   ├── ScheduleUnitHandler.cs
│   │   └── ScheduleUnitEndpoint.cs
│   ├── RescheduleUnit/
│   │   ├── RescheduleUnitCommand.cs
│   │   ├── RescheduleUnitValidator.cs
│   │   ├── RescheduleUnitHandler.cs
│   │   └── RescheduleUnitEndpoint.cs
│   ├── UnscheduleUnit/
│   │   ├── UnscheduleUnitCommand.cs
│   │   ├── UnscheduleUnitHandler.cs
│   │   └── UnscheduleUnitEndpoint.cs
│   └── ListUnitsByLocation/
│       ├── ListUnitsByLocationQuery.cs
│       ├── ListUnitsByLocationValidator.cs
│       ├── ListUnitsByLocationHandler.cs
│       └── ListUnitsByLocationEndpoint.cs
├── Persistence/
│   ├── SessionDocument.cs
│   ├── SessionMapping.cs
│   ├── ISessionRepository.cs
│   ├── MongoSessionRepository.cs
│   ├── UnitScheduleDocument.cs
│   ├── UnitScheduleMapping.cs
│   ├── IUnitScheduleRepository.cs
│   └── MongoUnitScheduleRepository.cs
├── Errors/
│   └── SchedulingErrors.cs
└── I18n/
    ├── eng.json
    ├── spa.json
    └── por.json

src/OVR.SharedKernel/Domain/Events/Integration/
├── UnitScheduledEvent.cs                 # NEW
└── UnitUnscheduledEvent.cs               # NEW

tests/OVR.Modules.Scheduling.Tests/       # NEW project
├── Domain/
│   ├── SessionAggregateTests.cs
│   ├── UnitScheduleAggregateTests.cs
│   └── ScheduleCollisionDetectorTests.cs
├── Features/
│   ├── CreateSession/CreateSessionHandlerTests.cs
│   ├── ScheduleUnit/ScheduleUnitHandlerTests.cs
│   ├── RescheduleUnit/RescheduleUnitHandlerTests.cs
│   ├── UnscheduleUnit/UnscheduleUnitHandlerTests.cs
│   └── ListUnitsByLocation/ListUnitsByLocationHandlerTests.cs
└── OVR.Modules.Scheduling.Tests.csproj

tests/OVR.Api.IntegrationTests/Scheduling/
├── Support/SchedulingWebAppFactory.cs
├── CreateSessionEndpointTests.cs
├── ScheduleUnitEndpointTests.cs
├── RescheduleUnitEndpointTests.cs
├── UnscheduleUnitEndpointTests.cs
└── ListUnitsByLocationEndpointTests.cs
```

### Modified files

- `src/OVR.Modules.Scheduling/SchedulingModule.cs` — wire DI + endpoints (currently stub)
- `src/OVR.Modules.Scheduling/OVR.Modules.Scheduling.csproj` — add I18n content + CommonCodes reference
- `src/OVR.SharedKernel/Domain/Events/Integration/UnitScheduleChangedEvent.cs` — REWRITE (replace 3-field record with full snapshot)
- `src/OVR.SharedKernel/Constants/WellKnownCodeTypes.cs` — add `Location` constant
- `src/OVR.Api/Program.cs` — ensure Scheduling assembly registered for MediatR + FluentValidation
- `OvrBackendCore.slnx` — add test project

### Deleted files

- `src/OVR.Modules.Scheduling/Domain/Unit.cs` — scaffolding collides with CompetitionConfig.Domain.Unit
- `src/OVR.Modules.Scheduling/Domain/UnitStatus.cs` — mixes schedule + result states

---

## Task 1: Create feature branch + cleanup wrong stubs + add Location constant

**Files:**
- Delete: `src/OVR.Modules.Scheduling/Domain/Unit.cs`
- Delete: `src/OVR.Modules.Scheduling/Domain/UnitStatus.cs`
- Modify: `src/OVR.SharedKernel/Constants/WellKnownCodeTypes.cs`

- [ ] **Step 1: Create feature branch from main**

```bash
git checkout main
git checkout -b feat/scheduling-mvp
```

- [ ] **Step 2: Delete the wrong stubs**

```bash
rm src/OVR.Modules.Scheduling/Domain/Unit.cs
rm src/OVR.Modules.Scheduling/Domain/UnitStatus.cs
```

- [ ] **Step 3: Add `Location` to `WellKnownCodeTypes`**

Edit `src/OVR.SharedKernel/Constants/WellKnownCodeTypes.cs`. Add one line (after `Venue`):

```csharp
namespace OVR.SharedKernel.Constants;

public static class WellKnownCodeTypes
{
    public const string Country = "COUNTRY";
    public const string Sport = "SPORT";
    public const string Discipline = "DISCIPLINE";
    public const string DisciplineGender = "DISCIPLINE_GENDER";
    public const string Event = "EVENT";
    public const string Venue = "VENUES";
    public const string Location = "LOCATION";               // NEW
    public const string Cluster = "CLUSTER";
    public const string Organisation = "ORGANISATIONS";
    public const string OrganisationType = "ORGANISATION_TYPE";
    public const string NocParticipant = "NOC_PARTICIPANTS";
    public const string Continent = "CONTINENT";
    public const string PersonGender = "PERSON_GENDER";
    public const string CompetitionCode = "COMPETITION_CODE";
    public const string Language = "LANGUAGE";
}
```

- [ ] **Step 4: Build to verify nothing broke**

Run: `dotnet build`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.Scheduling/Domain/Unit.cs src/OVR.Modules.Scheduling/Domain/UnitStatus.cs src/OVR.SharedKernel/Constants/WellKnownCodeTypes.cs
git commit -m "chore(scheduling): remove wrong Unit/UnitStatus stubs and add Location code type"
```

---

## Task 2: Update Scheduling csproj with packages + CommonCodes reference

**Files:**
- Modify: `src/OVR.Modules.Scheduling/OVR.Modules.Scheduling.csproj`

- [ ] **Step 1: Replace csproj contents**

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
    <Content Include="I18n\**" Link="I18n.Scheduling\%(RecursiveDir)%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: succeeds (may show warnings about missing I18n folder — that's fine, folder will be created in Task 14).

- [ ] **Step 3: Commit**

```bash
git add src/OVR.Modules.Scheduling/OVR.Modules.Scheduling.csproj
git commit -m "chore(scheduling): wire packages and project references"
```

---

## Task 3: SharedKernel — add new integration events + rewrite UnitScheduleChangedEvent

**Files:**
- Create: `src/OVR.SharedKernel/Domain/Events/Integration/UnitScheduledEvent.cs`
- Create: `src/OVR.SharedKernel/Domain/Events/Integration/UnitUnscheduledEvent.cs`
- Modify: `src/OVR.SharedKernel/Domain/Events/Integration/UnitScheduleChangedEvent.cs`

- [ ] **Step 1: Create `UnitScheduledEvent.cs`**

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitScheduledEvent(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    DateTime ScheduledAt) : DomainEventBase;
```

- [ ] **Step 2: Create `UnitUnscheduledEvent.cs`**

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitUnscheduledEvent(
    string UnitRsc,
    string EventRsc,
    DateTime UnscheduledAt) : DomainEventBase;
```

- [ ] **Step 3: Replace `UnitScheduleChangedEvent.cs` contents**

```csharp
namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitScheduleChangedEvent(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string? Reason,
    DateTime ChangedAt) : DomainEventBase;
```

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: succeeds (the old `UnitScheduleChangedEvent` had no consumers — the rewrite should not break anything).

- [ ] **Step 5: Commit**

```bash
git add src/OVR.SharedKernel/Domain/Events/Integration/UnitScheduledEvent.cs src/OVR.SharedKernel/Domain/Events/Integration/UnitUnscheduledEvent.cs src/OVR.SharedKernel/Domain/Events/Integration/UnitScheduleChangedEvent.cs
git commit -m "feat(sharedkernel): add UnitScheduled/Unscheduled events and rewrite UnitScheduleChangedEvent"
```

---

## Task 4: Create test project scaffolding

**Files:**
- Create: `tests/OVR.Modules.Scheduling.Tests/OVR.Modules.Scheduling.Tests.csproj`

- [ ] **Step 1: Create `OVR.Modules.Scheduling.Tests.csproj`**

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
    <ProjectReference Include="..\..\src\OVR.Modules.Scheduling\OVR.Modules.Scheduling.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add project to solution**

Run: `dotnet sln add tests/OVR.Modules.Scheduling.Tests/OVR.Modules.Scheduling.Tests.csproj`

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add tests/OVR.Modules.Scheduling.Tests/ OvrBackendCore.slnx
git commit -m "test(scheduling): add test project scaffold"
```

---

## Task 5: `ScheduleStatus` enum

**Files:**
- Create: `src/OVR.Modules.Scheduling/Domain/ScheduleStatus.cs`

- [ ] **Step 1: Create the enum**

```csharp
namespace OVR.Modules.Scheduling.Domain;

public enum ScheduleStatus
{
    Scheduled = 1
    // CANCELLED, RESCHEDULED, POSTPONED, UNSCHEDULED — future MVPs
}
```

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/OVR.Modules.Scheduling/Domain/ScheduleStatus.cs
git commit -m "feat(scheduling): add ScheduleStatus enum"
```

---

## Task 6: `Session` aggregate — failing tests + implementation

**Files:**
- Create: `tests/OVR.Modules.Scheduling.Tests/Domain/SessionAggregateTests.cs`
- Create: `src/OVR.Modules.Scheduling/Domain/Session.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/OVR.Modules.Scheduling.Tests/Domain/SessionAggregateTests.cs`:

```csharp
using FluentAssertions;
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Tests.Domain;

public class SessionAggregateTests
{
    private static Session CreateValid(
        string code = "BOX01",
        string venueCode = "ABC",
        string name = "Boxing Session 1",
        DateTime? startDate = null,
        DateTime? endDate = null,
        TimeSpan? leadin = null)
    {
        var start = startDate ?? new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);
        var end = endDate ?? start.AddHours(4);
        return Session.Create(code, venueCode, name, start, end, leadin);
    }

    [Fact]
    public void Create_WithValidInputs_SetsProperties()
    {
        var session = CreateValid(leadin: TimeSpan.FromMinutes(5));

        session.Id.Should().Be("BOX01");
        session.Code.Should().Be("BOX01");
        session.VenueCode.Should().Be("ABC");
        session.Name.Should().Be("Boxing Session 1");
        session.Leadin.Should().Be(TimeSpan.FromMinutes(5));
        session.StartDate.Should().Be(new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc));
        session.EndDate.Should().Be(new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc));
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullLeadin_AllowsIt()
    {
        var session = CreateValid(leadin: null);

        session.Leadin.Should().BeNull();
    }

    [Fact]
    public void Create_WithEndDateBeforeStartDate_Throws()
    {
        var start = new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(-1);

        Action act = () => CreateValid(startDate: start, endDate: end);

        act.Should().Throw<ArgumentException>().WithMessage("*EndDate*");
    }

    [Fact]
    public void Create_WithEndDateEqualToStartDate_Throws()
    {
        var start = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);

        Action act = () => CreateValid(startDate: start, endDate: start);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyCode_Throws()
    {
        Action act = () => CreateValid(code: "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidVenueLength_Throws()
    {
        Action act = () => CreateValid(venueCode: "AB");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNegativeLeadin_Throws()
    {
        Action act = () => CreateValid(leadin: TimeSpan.FromMinutes(-1));

        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Run tests to verify fail (compile error)**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~SessionAggregateTests"`
Expected: compile error — `Session` not found.

- [ ] **Step 3: Implement `Session.cs`**

```csharp
using OVR.SharedKernel.Domain.Primitives;

namespace OVR.Modules.Scheduling.Domain;

public sealed class Session : AggregateRoot<string>
{
    public string Code { get; private set; } = string.Empty;
    public string VenueCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public TimeSpan? Leadin { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Session() { }

    public static Session Create(
        string code,
        string venueCode,
        string name,
        DateTime startDate,
        DateTime endDate,
        TimeSpan? leadin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(venueCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (venueCode.Length != 3)
            throw new ArgumentException(
                $"VenueCode must be exactly 3 characters, got '{venueCode}'.",
                nameof(venueCode));

        if (endDate <= startDate)
            throw new ArgumentException(
                $"EndDate ({endDate:O}) must be strictly greater than StartDate ({startDate:O}).",
                nameof(endDate));

        if (leadin.HasValue && leadin.Value < TimeSpan.Zero)
            throw new ArgumentException(
                $"Leadin must be non-negative, got {leadin.Value}.",
                nameof(leadin));

        return new Session
        {
            Id = code,
            Code = code,
            VenueCode = venueCode,
            Name = name,
            StartDate = startDate,
            EndDate = endDate,
            Leadin = leadin,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~SessionAggregateTests"`
Expected: all 7 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.Scheduling/Domain/Session.cs tests/OVR.Modules.Scheduling.Tests/Domain/SessionAggregateTests.cs
git commit -m "feat(scheduling): add Session aggregate with validation"
```

---

## Task 7: `UnitSchedule` aggregate — failing tests + implementation

**Files:**
- Create: `tests/OVR.Modules.Scheduling.Tests/Domain/UnitScheduleAggregateTests.cs`
- Create: `src/OVR.Modules.Scheduling/Domain/UnitSchedule.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using OVR.Modules.Scheduling.Domain;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Tests.Domain;

public class UnitScheduleAggregateTests
{
    private static readonly DateTime StartTime =
        new(2026, 4, 20, 10, 15, 0, DateTimeKind.Utc);

    private static UnitSchedule CreateValid(string unitRsc = "BOXM57KG--------------8FNL0001----") =>
        UnitSchedule.Create(
            unitRsc: Rsc.Create(unitRsc),
            sessionCode: "BOX01",
            locationCode: "RGA",
            startTime: StartTime,
            orderInSession: 1,
            orderInLocation: 1);

    [Fact]
    public void Create_FromUnitLevelRsc_DerivesEventRsc()
    {
        var schedule = CreateValid();

        schedule.Id.Should().Be("BOXM57KG--------------8FNL0001----");
        schedule.UnitRsc.Value.Should().Be("BOXM57KG--------------8FNL0001----");
        schedule.EventRsc.Value.Should().Be("BOXM57KG--------------------------");
        schedule.SessionCode.Should().Be("BOX01");
        schedule.LocationCode.Should().Be("RGA");
        schedule.StartTime.Should().Be(StartTime);
        schedule.OrderInSession.Should().Be(1);
        schedule.OrderInLocation.Should().Be(1);
        schedule.Status.Should().Be(ScheduleStatus.Scheduled);
        schedule.ScheduledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        schedule.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_FromNonUnitLevelRsc_Throws()
    {
        var eventRsc = Rsc.Create("BOXM57KG--------------------------");

        Action act = () => UnitSchedule.Create(eventRsc, "BOX01", "RGA", StartTime, 1, 1);

        act.Should().Throw<ArgumentException>().WithMessage("*Unit*");
    }

    [Fact]
    public void Create_RaisesUnitScheduledEvent_WithCorrectPayload()
    {
        var schedule = CreateValid();

        var raised = schedule.DomainEvents.OfType<UnitScheduledEvent>().SingleOrDefault();
        raised.Should().NotBeNull();
        raised!.UnitRsc.Should().Be("BOXM57KG--------------8FNL0001----");
        raised.EventRsc.Should().Be("BOXM57KG--------------------------");
        raised.SessionCode.Should().Be("BOX01");
        raised.LocationCode.Should().Be("RGA");
        raised.StartTime.Should().Be(StartTime);
        raised.OrderInSession.Should().Be(1);
        raised.OrderInLocation.Should().Be(1);
    }

    [Fact]
    public void Create_WithZeroOrderInSession_Throws()
    {
        Action act = () => UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0001----"),
            "BOX01", "RGA", StartTime, orderInSession: 0, orderInLocation: 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithShortLocationCode_Throws()
    {
        Action act = () => UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0001----"),
            "BOX01", "RG", StartTime, 1, 1);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reschedule_WithNewValues_UpdatesFieldsAndRaisesChangedEvent()
    {
        var schedule = CreateValid();
        schedule.ClearDomainEvents();
        var newTime = StartTime.AddHours(2);

        schedule.Reschedule(
            newSessionCode: "BOX02",
            newLocationCode: "RGB",
            newStartTime: newTime,
            newOrderInSession: 3,
            newOrderInLocation: 2,
            reason: "weather delay");

        schedule.SessionCode.Should().Be("BOX02");
        schedule.LocationCode.Should().Be("RGB");
        schedule.StartTime.Should().Be(newTime);
        schedule.OrderInSession.Should().Be(3);
        schedule.OrderInLocation.Should().Be(2);
        schedule.UpdatedAt.Should().NotBeNull();

        var raised = schedule.DomainEvents.OfType<UnitScheduleChangedEvent>().SingleOrDefault();
        raised.Should().NotBeNull();
        raised!.SessionCode.Should().Be("BOX02");
        raised.LocationCode.Should().Be("RGB");
        raised.Reason.Should().Be("weather delay");
    }

    [Fact]
    public void Reschedule_WithNullReason_StillEmitsEvent()
    {
        var schedule = CreateValid();
        schedule.ClearDomainEvents();

        schedule.Reschedule("BOX01", "RGA", StartTime.AddMinutes(30), 1, 1, reason: null);

        var raised = schedule.DomainEvents.OfType<UnitScheduleChangedEvent>().Single();
        raised.Reason.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run tests — expect compile failure**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~UnitScheduleAggregateTests"`
Expected: compile error — `UnitSchedule` not found.

- [ ] **Step 3: Implement `UnitSchedule.cs`**

```csharp
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Domain;

public sealed class UnitSchedule : AggregateRoot<string>
{
    public Rsc UnitRsc { get; private set; } = null!;
    public Rsc EventRsc { get; private set; } = null!;
    public string SessionCode { get; private set; } = string.Empty;
    public string LocationCode { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public int OrderInSession { get; private set; }
    public int OrderInLocation { get; private set; }
    public ScheduleStatus Status { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UnitSchedule() { }

    public static UnitSchedule Create(
        Rsc unitRsc,
        string sessionCode,
        string locationCode,
        DateTime startTime,
        int orderInSession,
        int orderInLocation)
    {
        ArgumentNullException.ThrowIfNull(unitRsc);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationCode);

        if (!unitRsc.IsAtLevel(RscLevel.Unit))
            throw new ArgumentException(
                $"RSC must be at Unit level, got {unitRsc.Level}: '{unitRsc.Value}'.",
                nameof(unitRsc));

        if (locationCode.Length != 3)
            throw new ArgumentException(
                $"LocationCode must be exactly 3 characters, got '{locationCode}'.",
                nameof(locationCode));

        if (orderInSession < 1)
            throw new ArgumentException(
                $"OrderInSession must be >= 1, got {orderInSession}.",
                nameof(orderInSession));

        if (orderInLocation < 1)
            throw new ArgumentException(
                $"OrderInLocation must be >= 1, got {orderInLocation}.",
                nameof(orderInLocation));

        var eventRsc = Rsc.Create(unitRsc.AtEventLevel());
        var now = DateTime.UtcNow;

        var schedule = new UnitSchedule
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            EventRsc = eventRsc,
            SessionCode = sessionCode,
            LocationCode = locationCode,
            StartTime = startTime,
            OrderInSession = orderInSession,
            OrderInLocation = orderInLocation,
            Status = ScheduleStatus.Scheduled,
            ScheduledAt = now
        };

        schedule.RaiseDomainEvent(new UnitScheduledEvent(
            UnitRsc: unitRsc.Value,
            EventRsc: eventRsc.Value,
            SessionCode: sessionCode,
            LocationCode: locationCode,
            StartTime: startTime,
            OrderInSession: orderInSession,
            OrderInLocation: orderInLocation,
            ScheduledAt: now));

        return schedule;
    }

    public void Reschedule(
        string newSessionCode,
        string newLocationCode,
        DateTime newStartTime,
        int newOrderInSession,
        int newOrderInLocation,
        string? reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newSessionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(newLocationCode);

        if (newLocationCode.Length != 3)
            throw new ArgumentException(
                $"LocationCode must be exactly 3 characters, got '{newLocationCode}'.",
                nameof(newLocationCode));

        if (newOrderInSession < 1)
            throw new ArgumentException(
                $"OrderInSession must be >= 1, got {newOrderInSession}.",
                nameof(newOrderInSession));

        if (newOrderInLocation < 1)
            throw new ArgumentException(
                $"OrderInLocation must be >= 1, got {newOrderInLocation}.",
                nameof(newOrderInLocation));

        if (Status != ScheduleStatus.Scheduled)
            throw new InvalidOperationException(
                $"Cannot reschedule a unit in status '{Status}'.");

        SessionCode = newSessionCode;
        LocationCode = newLocationCode;
        StartTime = newStartTime;
        OrderInSession = newOrderInSession;
        OrderInLocation = newOrderInLocation;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UnitScheduleChangedEvent(
            UnitRsc: UnitRsc.Value,
            EventRsc: EventRsc.Value,
            SessionCode: newSessionCode,
            LocationCode: newLocationCode,
            StartTime: newStartTime,
            OrderInSession: newOrderInSession,
            OrderInLocation: newOrderInLocation,
            Reason: reason,
            ChangedAt: UpdatedAt.Value));
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~UnitScheduleAggregateTests"`
Expected: all 7 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.Scheduling/Domain/UnitSchedule.cs tests/OVR.Modules.Scheduling.Tests/Domain/UnitScheduleAggregateTests.cs
git commit -m "feat(scheduling): add UnitSchedule aggregate with Create and Reschedule"
```

---

## Task 8: `SchedulingErrors` typed errors

**Files:**
- Create: `src/OVR.Modules.Scheduling/Errors/SchedulingErrors.cs`

- [ ] **Step 1: Create the errors file**

```csharp
using ErrorOr;

namespace OVR.Modules.Scheduling.Errors;

public static class SchedulingErrors
{
    public static Error InvalidVenue(string code) =>
        Error.Validation(
            "Scheduling.InvalidVenue",
            "Venue code is not in the common codes catalog.",
            new Dictionary<string, object> { ["venueCode"] = code });

    public static Error SessionAlreadyExists(string code) =>
        Error.Conflict(
            "Scheduling.SessionAlreadyExists",
            "A session with this code already exists.",
            new Dictionary<string, object> { ["sessionCode"] = code });

    public static Error SessionNotFound(string code) =>
        Error.NotFound(
            "Scheduling.SessionNotFound",
            "Session not found.",
            new Dictionary<string, object> { ["sessionCode"] = code });

    public static Error StartTimeOutsideSession(
        DateTime startTime, DateTime sessionStart, DateTime sessionEnd) =>
        Error.Validation(
            "Scheduling.StartTimeOutsideSession",
            "StartTime is outside the session's date range.",
            new Dictionary<string, object>
            {
                ["startTime"] = startTime,
                ["sessionStart"] = sessionStart,
                ["sessionEnd"] = sessionEnd
            });

    public static Error UnitAlreadyScheduled(string unitRsc) =>
        Error.Conflict(
            "Scheduling.UnitAlreadyScheduled",
            "This unit is already scheduled. Use reschedule instead.",
            new Dictionary<string, object> { ["unitRsc"] = unitRsc });

    public static Error LocationTimeOccupied(
        string locationCode, DateTime startTime, string conflictingUnitRsc) =>
        Error.Conflict(
            "Scheduling.LocationTimeOccupied",
            "Another unit is already scheduled at this location and time.",
            new Dictionary<string, object>
            {
                ["locationCode"] = locationCode,
                ["startTime"] = startTime,
                ["conflictingUnit"] = conflictingUnitRsc
            });

    public static Error UnitScheduleNotFound(string unitRsc) =>
        Error.NotFound(
            "Scheduling.UnitScheduleNotFound",
            "Unit schedule not found.",
            new Dictionary<string, object> { ["unitRsc"] = unitRsc });
}
```

- [ ] **Step 2: Build**

Run: `dotnet build src/OVR.Modules.Scheduling/`
Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/OVR.Modules.Scheduling/Errors/SchedulingErrors.cs
git commit -m "feat(scheduling): add typed errors for all failure modes"
```

---

## Task 9: Session persistence layer

**Files:**
- Create: `src/OVR.Modules.Scheduling/Persistence/SessionDocument.cs`
- Create: `src/OVR.Modules.Scheduling/Persistence/SessionMapping.cs`
- Create: `src/OVR.Modules.Scheduling/Persistence/ISessionRepository.cs`
- Create: `src/OVR.Modules.Scheduling/Persistence/MongoSessionRepository.cs`

- [ ] **Step 1: Create `SessionDocument.cs`**

```csharp
using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Scheduling.Persistence;

public sealed class SessionDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string VenueCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan? Leadin { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 2: Create `SessionMapping.cs`**

```csharp
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

internal static class SessionMapping
{
    public static SessionDocument ToDocument(Session session) => new()
    {
        Id = session.Id,
        VenueCode = session.VenueCode,
        Name = session.Name,
        StartDate = session.StartDate,
        EndDate = session.EndDate,
        Leadin = session.Leadin,
        CreatedAt = session.CreatedAt
    };

    public static Session ToDomain(SessionDocument doc) =>
        Session.Create(doc.Id, doc.VenueCode, doc.Name, doc.StartDate, doc.EndDate, doc.Leadin);
}
```

Note: `ToDomain` reuses `Create` (which re-runs validation and re-sets `CreatedAt = UtcNow`). For MVP this is acceptable — the CreatedAt drift between persisted and hydrated is small and doesn't affect behavior. If needed later, add an `internal Hydrate` method similar to CompetitionConfig MVP. For now, KISS.

- [ ] **Step 3: Create `ISessionRepository.cs`**

```csharp
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

public interface ISessionRepository
{
    Task<Session?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create `MongoSessionRepository.cs`**

```csharp
using MongoDB.Driver;
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

internal sealed class MongoSessionRepository(IMongoDatabase database) : ISessionRepository
{
    private IMongoCollection<SessionDocument> Collection =>
        database.GetCollection<SessionDocument>("scheduling_sessions");

    public async Task<Session?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == code).FirstOrDefaultAsync(ct);
        return doc is null ? null : SessionMapping.ToDomain(doc);
    }

    public async Task AddAsync(Session session, CancellationToken ct = default)
    {
        var doc = SessionMapping.ToDocument(session);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 6: Commit**

```bash
git add src/OVR.Modules.Scheduling/Persistence/SessionDocument.cs src/OVR.Modules.Scheduling/Persistence/SessionMapping.cs src/OVR.Modules.Scheduling/Persistence/ISessionRepository.cs src/OVR.Modules.Scheduling/Persistence/MongoSessionRepository.cs
git commit -m "feat(scheduling): add Session persistence layer"
```

---

## Task 10: UnitSchedule persistence layer

**Files:**
- Create: `src/OVR.Modules.Scheduling/Persistence/UnitScheduleDocument.cs`
- Create: `src/OVR.Modules.Scheduling/Persistence/UnitScheduleMapping.cs`
- Create: `src/OVR.Modules.Scheduling/Persistence/IUnitScheduleRepository.cs`
- Create: `src/OVR.Modules.Scheduling/Persistence/MongoUnitScheduleRepository.cs`
- Modify: `src/OVR.Modules.Scheduling/Domain/UnitSchedule.cs` (add `internal Hydrate` to rebuild from storage without raising events)

- [ ] **Step 1: Add `internal Hydrate` helper to `UnitSchedule.cs`**

Append the following method to the `UnitSchedule` class (just before the closing brace):

```csharp
    internal static UnitSchedule Hydrate(
        Rsc unitRsc,
        Rsc eventRsc,
        string sessionCode,
        string locationCode,
        DateTime startTime,
        int orderInSession,
        int orderInLocation,
        ScheduleStatus status,
        DateTime scheduledAt,
        DateTime? updatedAt)
    {
        return new UnitSchedule
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            EventRsc = eventRsc,
            SessionCode = sessionCode,
            LocationCode = locationCode,
            StartTime = startTime,
            OrderInSession = orderInSession,
            OrderInLocation = orderInLocation,
            Status = status,
            ScheduledAt = scheduledAt,
            UpdatedAt = updatedAt
        };
    }
```

Hydrate bypasses the invariants because the data already exists in storage — we trust it. No domain event is raised.

- [ ] **Step 2: Create `UnitScheduleDocument.cs`**

```csharp
using MongoDB.Bson.Serialization.Attributes;

namespace OVR.Modules.Scheduling.Persistence;

public sealed class UnitScheduleDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public string EventRsc { get; set; } = string.Empty;
    public string SessionCode { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int OrderInSession { get; set; }
    public int OrderInLocation { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

- [ ] **Step 3: Create `UnitScheduleMapping.cs`**

```csharp
using OVR.Modules.Scheduling.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Persistence;

internal static class UnitScheduleMapping
{
    public static UnitScheduleDocument ToDocument(UnitSchedule schedule) => new()
    {
        Id = schedule.Id,
        EventRsc = schedule.EventRsc.Value,
        SessionCode = schedule.SessionCode,
        LocationCode = schedule.LocationCode,
        StartTime = schedule.StartTime,
        OrderInSession = schedule.OrderInSession,
        OrderInLocation = schedule.OrderInLocation,
        Status = schedule.Status.ToString(),
        ScheduledAt = schedule.ScheduledAt,
        UpdatedAt = schedule.UpdatedAt
    };

    public static UnitSchedule ToDomain(UnitScheduleDocument doc)
    {
        var unitRsc = Rsc.Create(doc.Id);
        var eventRsc = Rsc.Create(doc.EventRsc);
        var status = Enum.Parse<ScheduleStatus>(doc.Status, ignoreCase: true);

        return UnitSchedule.Hydrate(
            unitRsc, eventRsc, doc.SessionCode, doc.LocationCode,
            doc.StartTime, doc.OrderInSession, doc.OrderInLocation,
            status, doc.ScheduledAt, doc.UpdatedAt);
    }
}
```

- [ ] **Step 4: Create `IUnitScheduleRepository.cs`**

```csharp
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

public interface IUnitScheduleRepository
{
    Task<UnitSchedule?> GetByUnitRscAsync(string unitRsc, CancellationToken ct = default);
    Task<UnitSchedule?> FindByLocationAndTimeAsync(
        string locationCode, DateTime startTime, CancellationToken ct = default);
    Task<IReadOnlyList<UnitSchedule>> ListByLocationAndDateAsync(
        string locationCode, DateOnly date, CancellationToken ct = default);
    Task AddAsync(UnitSchedule schedule, CancellationToken ct = default);
    Task UpdateAsync(UnitSchedule schedule, CancellationToken ct = default);
    Task DeleteAsync(string unitRsc, CancellationToken ct = default);
}
```

- [ ] **Step 5: Create `MongoUnitScheduleRepository.cs`**

```csharp
using MongoDB.Driver;
using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

internal sealed class MongoUnitScheduleRepository(IMongoDatabase database) : IUnitScheduleRepository
{
    private IMongoCollection<UnitScheduleDocument> Collection =>
        database.GetCollection<UnitScheduleDocument>("scheduling_unit_schedules");

    public async Task<UnitSchedule?> GetByUnitRscAsync(string unitRsc, CancellationToken ct = default)
    {
        var doc = await Collection.Find(d => d.Id == unitRsc).FirstOrDefaultAsync(ct);
        return doc is null ? null : UnitScheduleMapping.ToDomain(doc);
    }

    public async Task<UnitSchedule?> FindByLocationAndTimeAsync(
        string locationCode, DateTime startTime, CancellationToken ct = default)
    {
        var doc = await Collection
            .Find(d => d.LocationCode == locationCode && d.StartTime == startTime)
            .FirstOrDefaultAsync(ct);
        return doc is null ? null : UnitScheduleMapping.ToDomain(doc);
    }

    public async Task<IReadOnlyList<UnitSchedule>> ListByLocationAndDateAsync(
        string locationCode, DateOnly date, CancellationToken ct = default)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var docs = await Collection
            .Find(d => d.LocationCode == locationCode
                && d.StartTime >= dayStart
                && d.StartTime < dayEnd)
            .SortBy(d => d.StartTime)
            .ToListAsync(ct);
        return docs.Select(UnitScheduleMapping.ToDomain).ToList();
    }

    public async Task AddAsync(UnitSchedule schedule, CancellationToken ct = default)
    {
        var doc = UnitScheduleMapping.ToDocument(schedule);
        await Collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(UnitSchedule schedule, CancellationToken ct = default)
    {
        var doc = UnitScheduleMapping.ToDocument(schedule);
        await Collection.ReplaceOneAsync(d => d.Id == doc.Id, doc, cancellationToken: ct);
    }

    public async Task DeleteAsync(string unitRsc, CancellationToken ct = default)
    {
        await Collection.DeleteOneAsync(d => d.Id == unitRsc, ct);
    }
}
```

- [ ] **Step 6: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 7: Commit**

```bash
git add src/OVR.Modules.Scheduling/Persistence/UnitScheduleDocument.cs src/OVR.Modules.Scheduling/Persistence/UnitScheduleMapping.cs src/OVR.Modules.Scheduling/Persistence/IUnitScheduleRepository.cs src/OVR.Modules.Scheduling/Persistence/MongoUnitScheduleRepository.cs src/OVR.Modules.Scheduling/Domain/UnitSchedule.cs
git commit -m "feat(scheduling): add UnitSchedule persistence layer with Hydrate helper"
```

---

## Task 11: `ScheduleCollisionDetector` domain service

**Files:**
- Create: `src/OVR.Modules.Scheduling/Domain/ScheduleCollisionDetector.cs`
- Create: `tests/OVR.Modules.Scheduling.Tests/Domain/ScheduleCollisionDetectorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using ErrorOr;
using FluentAssertions;
using NSubstitute;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Tests.Domain;

public class ScheduleCollisionDetectorTests
{
    private readonly IUnitScheduleRepository _repo = Substitute.For<IUnitScheduleRepository>();
    private readonly ScheduleCollisionDetector _detector;
    private static readonly DateTime StartTime =
        new(2026, 4, 20, 10, 15, 0, DateTimeKind.Utc);

    public ScheduleCollisionDetectorTests()
    {
        _detector = new ScheduleCollisionDetector(_repo);
    }

    [Fact]
    public async Task EnsureNoCollision_NoOtherUnit_ReturnsSuccess()
    {
        _repo.FindByLocationAndTimeAsync("RGA", StartTime, Arg.Any<CancellationToken>())
            .Returns((UnitSchedule?)null);

        var result = await _detector.EnsureNoCollisionAsync("RGA", StartTime, null, CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureNoCollision_SameLocationAndTime_ReturnsLocationTimeOccupied()
    {
        var other = UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0002----"),
            "BOX01", "RGA", StartTime, 1, 1);
        _repo.FindByLocationAndTimeAsync("RGA", StartTime, Arg.Any<CancellationToken>())
            .Returns(other);

        var result = await _detector.EnsureNoCollisionAsync("RGA", StartTime, null, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.LocationTimeOccupied");
    }

    [Fact]
    public async Task EnsureNoCollision_WithExcludeUnitRsc_IgnoresSelf()
    {
        var self = UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0001----"),
            "BOX01", "RGA", StartTime, 1, 1);
        _repo.FindByLocationAndTimeAsync("RGA", StartTime, Arg.Any<CancellationToken>())
            .Returns(self);

        var result = await _detector.EnsureNoCollisionAsync(
            "RGA", StartTime, excludeUnitRsc: "BOXM57KG--------------8FNL0001----", CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureNoCollision_WithExcludeDifferentRsc_StillDetectsConflict()
    {
        var other = UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0002----"),
            "BOX01", "RGA", StartTime, 1, 1);
        _repo.FindByLocationAndTimeAsync("RGA", StartTime, Arg.Any<CancellationToken>())
            .Returns(other);

        var result = await _detector.EnsureNoCollisionAsync(
            "RGA", StartTime, excludeUnitRsc: "BOXM57KG--------------8FNL0001----", CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.LocationTimeOccupied");
    }
}
```

- [ ] **Step 2: Run tests — expect compile error**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~ScheduleCollisionDetectorTests"`
Expected: compile error — `ScheduleCollisionDetector` not found.

- [ ] **Step 3: Implement `ScheduleCollisionDetector.cs`**

```csharp
using ErrorOr;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;

namespace OVR.Modules.Scheduling.Domain;

public interface IScheduleCollisionDetector
{
    Task<ErrorOr<Success>> EnsureNoCollisionAsync(
        string locationCode,
        DateTime startTime,
        string? excludeUnitRsc,
        CancellationToken ct = default);
}

internal sealed class ScheduleCollisionDetector(IUnitScheduleRepository repo)
    : IScheduleCollisionDetector
{
    public async Task<ErrorOr<Success>> EnsureNoCollisionAsync(
        string locationCode,
        DateTime startTime,
        string? excludeUnitRsc,
        CancellationToken ct = default)
    {
        var existing = await repo.FindByLocationAndTimeAsync(locationCode, startTime, ct);
        if (existing is null)
            return Result.Success;
        if (existing.UnitRsc.Value == excludeUnitRsc)
            return Result.Success;
        return SchedulingErrors.LocationTimeOccupied(
            locationCode, startTime, existing.UnitRsc.Value);
    }
}
```

If ErrorOr 2.0 doesn't expose `Result.Success`, use `new Success()` or `Success.Instance` — check the ErrorOr package and adjust.

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~ScheduleCollisionDetectorTests"`
Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.Scheduling/Domain/ScheduleCollisionDetector.cs tests/OVR.Modules.Scheduling.Tests/Domain/ScheduleCollisionDetectorTests.cs
git commit -m "feat(scheduling): add ScheduleCollisionDetector domain service"
```

---

## Task 12: `CreateSession` feature — command, validator, handler, endpoint, tests

**Files:**
- Create: `src/OVR.Modules.Scheduling/Features/CreateSession/CreateSessionCommand.cs`
- Create: `src/OVR.Modules.Scheduling/Features/CreateSession/CreateSessionValidator.cs`
- Create: `src/OVR.Modules.Scheduling/Features/CreateSession/CreateSessionHandler.cs`
- Create: `src/OVR.Modules.Scheduling/Features/CreateSession/CreateSessionEndpoint.cs`
- Create: `tests/OVR.Modules.Scheduling.Tests/Features/CreateSession/CreateSessionHandlerTests.cs`

- [ ] **Step 1: Create `CreateSessionCommand.cs`**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.CreateSession;

public sealed record CreateSessionCommand(
    string Code,
    string VenueCode,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    TimeSpan? Leadin) : IRequest<ErrorOr<CreateSessionResponse>>;

public sealed record CreateSessionResponse(
    string Code,
    string VenueCode,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    TimeSpan? Leadin,
    DateTime CreatedAt);
```

- [ ] **Step 2: Create `CreateSessionValidator.cs`**

```csharp
using FluentValidation;

namespace OVR.Modules.Scheduling.Features.CreateSession;

public sealed class CreateSessionValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(1, 10)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Code must be 1..10 uppercase alphanumeric chars.");

        RuleFor(x => x.VenueCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z0-9]{3}$")
            .WithMessage("VenueCode must be exactly 3 uppercase alphanumeric chars.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(1, 40);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be greater than StartDate.");

        RuleFor(x => x.Leadin)
            .Must(l => l >= TimeSpan.Zero)
            .When(x => x.Leadin.HasValue)
            .WithMessage("Leadin must be non-negative when provided.");
    }
}
```

- [ ] **Step 3: Write failing handler tests**

Create `tests/OVR.Modules.Scheduling.Tests/Features/CreateSession/CreateSessionHandlerTests.cs`:

```csharp
using FluentAssertions;
using NSubstitute;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Features.CreateSession;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Contracts;

namespace OVR.Modules.Scheduling.Tests.Features.CreateSession;

public class CreateSessionHandlerTests
{
    private readonly ISessionRepository _repo = Substitute.For<ISessionRepository>();
    private readonly ICommonCodeCache _cache = Substitute.For<ICommonCodeCache>();
    private readonly CreateSessionHandler _handler;

    public CreateSessionHandlerTests()
    {
        _handler = new CreateSessionHandler(_repo, _cache);
    }

    private static CreateSessionCommand ValidCommand() =>
        new(
            Code: "BOX01",
            VenueCode: "ABC",
            Name: "Boxing Session 1",
            StartDate: new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            EndDate: new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc),
            Leadin: TimeSpan.FromMinutes(5));

    private void SetupValidVenue() =>
        _cache.Exists(CommonCodeTypes.Venue, "ABC").Returns(true);

    [Fact]
    public async Task Handle_ValidCommand_PersistsAndReturnsResponse()
    {
        SetupValidVenue();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Code.Should().Be("BOX01");
        await _repo.Received(1).AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidVenue_ReturnsInvalidVenueError()
    {
        _cache.Exists(CommonCodeTypes.Venue, "ABC").Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.InvalidVenue");
        await _repo.DidNotReceive().AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ReturnsSessionAlreadyExists()
    {
        SetupValidVenue();
        var existing = Session.Create("BOX01", "ABC", "existing",
            new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc),
            null);
        _repo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.SessionAlreadyExists");
    }
}
```

The package `OVR.Modules.CommonCodes.Contracts` must be available via the project reference. If the test project doesn't reference it transitively, add `<ProjectReference Include="..\..\src\OVR.Modules.CommonCodes\OVR.Modules.CommonCodes.csproj" />` to `OVR.Modules.Scheduling.Tests.csproj`.

- [ ] **Step 4: Run tests — expect compile error (handler missing)**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~CreateSessionHandlerTests"`
Expected: compile error — `CreateSessionHandler` not found.

- [ ] **Step 5: Implement `CreateSessionHandler.cs`**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Contracts;

namespace OVR.Modules.Scheduling.Features.CreateSession;

public sealed class CreateSessionHandler(
    ISessionRepository repository,
    ICommonCodeCache cache)
    : IRequestHandler<CreateSessionCommand, ErrorOr<CreateSessionResponse>>
{
    public async Task<ErrorOr<CreateSessionResponse>> Handle(
        CreateSessionCommand request,
        CancellationToken ct)
    {
        if (!cache.Exists(CommonCodeTypes.Venue, request.VenueCode))
            return SchedulingErrors.InvalidVenue(request.VenueCode);

        var existing = await repository.GetByCodeAsync(request.Code, ct);
        if (existing is not null)
            return SchedulingErrors.SessionAlreadyExists(request.Code);

        var session = Session.Create(
            request.Code, request.VenueCode, request.Name,
            request.StartDate, request.EndDate, request.Leadin);

        await repository.AddAsync(session, ct);

        return new CreateSessionResponse(
            session.Code, session.VenueCode, session.Name,
            session.StartDate, session.EndDate, session.Leadin, session.CreatedAt);
    }
}
```

Note: `CommonCodeTypes` is in `OVR.Modules.CommonCodes.Contracts`. If `Venue` key doesn't match what you need (constant may be called `Venues` per earlier spec), adjust — check file at `src/OVR.Modules.CommonCodes/Contracts/CommonCodeTypes.cs` to confirm. If there's no `Venue` there, use the SharedKernel constant `WellKnownCodeTypes.Venue`.

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~CreateSessionHandlerTests"`
Expected: 3 tests pass.

- [ ] **Step 7: Create `CreateSessionEndpoint.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.CreateSession;

public static class CreateSessionEndpoint
{
    public static async Task<IResult> Handle(
        CreateSessionCommand command,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(
            $"/api/scheduling/sessions/{command.Code}", httpContext);
    }
}
```

- [ ] **Step 8: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 9: Commit**

```bash
git add src/OVR.Modules.Scheduling/Features/CreateSession/ tests/OVR.Modules.Scheduling.Tests/Features/CreateSession/ tests/OVR.Modules.Scheduling.Tests/OVR.Modules.Scheduling.Tests.csproj
git commit -m "feat(scheduling): implement CreateSession feature (command, validator, handler, endpoint)"
```

---

## Task 13: `ScheduleUnit` feature — command, validator, handler, endpoint, tests

**Files:**
- Create: `src/OVR.Modules.Scheduling/Features/ScheduleUnit/ScheduleUnitCommand.cs`
- Create: `src/OVR.Modules.Scheduling/Features/ScheduleUnit/ScheduleUnitValidator.cs`
- Create: `src/OVR.Modules.Scheduling/Features/ScheduleUnit/ScheduleUnitHandler.cs`
- Create: `src/OVR.Modules.Scheduling/Features/ScheduleUnit/ScheduleUnitEndpoint.cs`
- Create: `tests/OVR.Modules.Scheduling.Tests/Features/ScheduleUnit/ScheduleUnitHandlerTests.cs`

- [ ] **Step 1: Create `ScheduleUnitCommand.cs`**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.ScheduleUnit;

public sealed record ScheduleUnitCommand(
    string SessionCode,
    string UnitRsc,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation) : IRequest<ErrorOr<ScheduleUnitResponse>>;

public sealed record ScheduleUnitResponse(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string Status,
    DateTime ScheduledAt);
```

- [ ] **Step 2: Create `ScheduleUnitValidator.cs`**

```csharp
using FluentValidation;

namespace OVR.Modules.Scheduling.Features.ScheduleUnit;

public sealed class ScheduleUnitValidator : AbstractValidator<ScheduleUnitCommand>
{
    public ScheduleUnitValidator()
    {
        RuleFor(x => x.SessionCode).NotEmpty();
        RuleFor(x => x.UnitRsc).NotEmpty().Length(34);
        RuleFor(x => x.LocationCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z0-9]{3}$");
        RuleFor(x => x.StartTime).NotEqual(default(DateTime));
        RuleFor(x => x.OrderInSession).GreaterThanOrEqualTo(1);
        RuleFor(x => x.OrderInLocation).GreaterThanOrEqualTo(1);
    }
}
```

- [ ] **Step 3: Write failing handler tests**

```csharp
using FluentAssertions;
using MediatR;
using NSubstitute;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Features.ScheduleUnit;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Tests.Features.ScheduleUnit;

public class ScheduleUnitHandlerTests
{
    private readonly ISessionRepository _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly IUnitScheduleRepository _scheduleRepo = Substitute.For<IUnitScheduleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly IScheduleCollisionDetector _collision = Substitute.For<IScheduleCollisionDetector>();
    private readonly ScheduleUnitHandler _handler;

    private static readonly DateTime StartTime =
        new(2026, 4, 20, 10, 15, 0, DateTimeKind.Utc);

    public ScheduleUnitHandlerTests()
    {
        _handler = new ScheduleUnitHandler(_sessionRepo, _scheduleRepo, _publisher, _collision);
    }

    private static Session ExistingSession() =>
        Session.Create("BOX01", "ABC", "Boxing Session 1",
            new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc),
            null);

    private static ScheduleUnitCommand ValidCommand() =>
        new("BOX01", "BOXM57KG--------------8FNL0001----", "RGA", StartTime, 1, 1);

    private void SetupHappyPath()
    {
        _sessionRepo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>())
            .Returns(ExistingSession());
        _scheduleRepo.GetByUnitRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UnitSchedule?)null);
        _collision.EnsureNoCollisionAsync(
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ErrorOr.Result.Success);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsSchedulePublishesEvent()
    {
        SetupHappyPath();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _scheduleRepo.Received(1).AddAsync(Arg.Any<UnitSchedule>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(Arg.Any<UnitScheduledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionNotFound_Returns404()
    {
        _sessionRepo.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.SessionNotFound");
    }

    [Fact]
    public async Task Handle_StartTimeBeforeSession_Returns_StartTimeOutsideSession()
    {
        _sessionRepo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>())
            .Returns(ExistingSession());

        var cmd = ValidCommand() with { StartTime = new DateTime(2026, 4, 20, 9, 0, 0, DateTimeKind.Utc) };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.StartTimeOutsideSession");
    }

    [Fact]
    public async Task Handle_StartTimeAfterSession_Returns_StartTimeOutsideSession()
    {
        _sessionRepo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>())
            .Returns(ExistingSession());

        var cmd = ValidCommand() with { StartTime = new DateTime(2026, 4, 20, 15, 0, 0, DateTimeKind.Utc) };

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.StartTimeOutsideSession");
    }

    [Fact]
    public async Task Handle_UnitAlreadyScheduled_ReturnsConflict()
    {
        _sessionRepo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>())
            .Returns(ExistingSession());
        var existing = UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0001----"),
            "BOX01", "RGA", StartTime, 1, 1);
        _scheduleRepo.GetByUnitRscAsync("BOXM57KG--------------8FNL0001----", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.UnitAlreadyScheduled");
    }

    [Fact]
    public async Task Handle_LocationTimeOccupied_ReturnsConflict()
    {
        _sessionRepo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>())
            .Returns(ExistingSession());
        _scheduleRepo.GetByUnitRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UnitSchedule?)null);
        _collision.EnsureNoCollisionAsync(
            "RGA", StartTime, null, Arg.Any<CancellationToken>())
            .Returns(OVR.Modules.Scheduling.Errors.SchedulingErrors
                .LocationTimeOccupied("RGA", StartTime, "BOXM57KG--------------8FNL0002----"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.LocationTimeOccupied");
    }
}
```

- [ ] **Step 4: Run tests — expect compile failure**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~ScheduleUnitHandlerTests"`
Expected: compile error — `ScheduleUnitHandler` not found.

- [ ] **Step 5: Implement `ScheduleUnitHandler.cs`**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Features.ScheduleUnit;

public sealed class ScheduleUnitHandler(
    ISessionRepository sessionRepository,
    IUnitScheduleRepository scheduleRepository,
    IPublisher publisher,
    IScheduleCollisionDetector collisionDetector)
    : IRequestHandler<ScheduleUnitCommand, ErrorOr<ScheduleUnitResponse>>
{
    public async Task<ErrorOr<ScheduleUnitResponse>> Handle(
        ScheduleUnitCommand request,
        CancellationToken ct)
    {
        var session = await sessionRepository.GetByCodeAsync(request.SessionCode, ct);
        if (session is null)
            return SchedulingErrors.SessionNotFound(request.SessionCode);

        if (request.StartTime < session.StartDate || request.StartTime > session.EndDate)
            return SchedulingErrors.StartTimeOutsideSession(
                request.StartTime, session.StartDate, session.EndDate);

        var existing = await scheduleRepository.GetByUnitRscAsync(request.UnitRsc, ct);
        if (existing is not null)
            return SchedulingErrors.UnitAlreadyScheduled(request.UnitRsc);

        var collisionResult = await collisionDetector.EnsureNoCollisionAsync(
            request.LocationCode, request.StartTime, excludeUnitRsc: null, ct);
        if (collisionResult.IsError)
            return collisionResult.Errors;

        var unitRsc = Rsc.Create(request.UnitRsc);
        var schedule = UnitSchedule.Create(
            unitRsc, request.SessionCode, request.LocationCode,
            request.StartTime, request.OrderInSession, request.OrderInLocation);

        await scheduleRepository.AddAsync(schedule, ct);

        foreach (var e in schedule.DomainEvents)
            await publisher.Publish(e, ct);
        schedule.ClearDomainEvents();

        return new ScheduleUnitResponse(
            UnitRsc: schedule.UnitRsc.Value,
            EventRsc: schedule.EventRsc.Value,
            SessionCode: schedule.SessionCode,
            LocationCode: schedule.LocationCode,
            StartTime: schedule.StartTime,
            OrderInSession: schedule.OrderInSession,
            OrderInLocation: schedule.OrderInLocation,
            Status: schedule.Status.ToString(),
            ScheduledAt: schedule.ScheduledAt);
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~ScheduleUnitHandlerTests"`
Expected: 6 tests pass.

- [ ] **Step 7: Create `ScheduleUnitEndpoint.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.ScheduleUnit;

public static class ScheduleUnitEndpoint
{
    public static async Task<IResult> Handle(
        string sessionCode,
        ScheduleUnitBody body,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var command = new ScheduleUnitCommand(
            sessionCode,
            body.UnitRsc,
            body.LocationCode,
            body.StartTime,
            body.OrderInSession,
            body.OrderInLocation);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(
            $"/api/scheduling/unit-schedules/{body.UnitRsc}", httpContext);
    }
}

public sealed record ScheduleUnitBody(
    string UnitRsc,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation);
```

- [ ] **Step 8: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 9: Commit**

```bash
git add src/OVR.Modules.Scheduling/Features/ScheduleUnit/ tests/OVR.Modules.Scheduling.Tests/Features/ScheduleUnit/
git commit -m "feat(scheduling): implement ScheduleUnit feature"
```

---

## Task 14: `RescheduleUnit` feature — command, validator, handler, endpoint, tests

**Files:**
- Create: `src/OVR.Modules.Scheduling/Features/RescheduleUnit/RescheduleUnitCommand.cs`
- Create: `src/OVR.Modules.Scheduling/Features/RescheduleUnit/RescheduleUnitValidator.cs`
- Create: `src/OVR.Modules.Scheduling/Features/RescheduleUnit/RescheduleUnitHandler.cs`
- Create: `src/OVR.Modules.Scheduling/Features/RescheduleUnit/RescheduleUnitEndpoint.cs`
- Create: `tests/OVR.Modules.Scheduling.Tests/Features/RescheduleUnit/RescheduleUnitHandlerTests.cs`

- [ ] **Step 1: Create `RescheduleUnitCommand.cs`**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.RescheduleUnit;

public sealed record RescheduleUnitCommand(
    string UnitRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string? Reason) : IRequest<ErrorOr<RescheduleUnitResponse>>;

public sealed record RescheduleUnitResponse(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string Status,
    DateTime ScheduledAt,
    DateTime? UpdatedAt);
```

- [ ] **Step 2: Create `RescheduleUnitValidator.cs`**

```csharp
using FluentValidation;

namespace OVR.Modules.Scheduling.Features.RescheduleUnit;

public sealed class RescheduleUnitValidator : AbstractValidator<RescheduleUnitCommand>
{
    public RescheduleUnitValidator()
    {
        RuleFor(x => x.UnitRsc).NotEmpty().Length(34);
        RuleFor(x => x.SessionCode).NotEmpty();
        RuleFor(x => x.LocationCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z0-9]{3}$");
        RuleFor(x => x.StartTime).NotEqual(default(DateTime));
        RuleFor(x => x.OrderInSession).GreaterThanOrEqualTo(1);
        RuleFor(x => x.OrderInLocation).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Reason)
            .Length(1, 200)
            .When(x => x.Reason is not null);
    }
}
```

- [ ] **Step 3: Write failing handler tests**

```csharp
using FluentAssertions;
using MediatR;
using NSubstitute;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Features.RescheduleUnit;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Tests.Features.RescheduleUnit;

public class RescheduleUnitHandlerTests
{
    private readonly ISessionRepository _sessionRepo = Substitute.For<ISessionRepository>();
    private readonly IUnitScheduleRepository _scheduleRepo = Substitute.For<IUnitScheduleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly IScheduleCollisionDetector _collision = Substitute.For<IScheduleCollisionDetector>();
    private readonly RescheduleUnitHandler _handler;

    private static readonly DateTime OldTime =
        new(2026, 4, 20, 10, 15, 0, DateTimeKind.Utc);
    private static readonly DateTime NewTime =
        new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    public RescheduleUnitHandlerTests()
    {
        _handler = new RescheduleUnitHandler(_sessionRepo, _scheduleRepo, _publisher, _collision);
    }

    private static Session ExistingSession(string code = "BOX01") =>
        Session.Create(code, "ABC", "session",
            new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 20, 14, 0, 0, DateTimeKind.Utc),
            null);

    private static UnitSchedule ExistingSchedule()
    {
        var s = UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0001----"),
            "BOX01", "RGA", OldTime, 1, 1);
        s.ClearDomainEvents();
        return s;
    }

    private static RescheduleUnitCommand ValidCommand() =>
        new(
            UnitRsc: "BOXM57KG--------------8FNL0001----",
            SessionCode: "BOX01",
            LocationCode: "RGB",
            StartTime: NewTime,
            OrderInSession: 2,
            OrderInLocation: 1,
            Reason: "mat swap");

    private void SetupHappyPath()
    {
        _scheduleRepo.GetByUnitRscAsync("BOXM57KG--------------8FNL0001----", Arg.Any<CancellationToken>())
            .Returns(ExistingSchedule());
        _sessionRepo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>())
            .Returns(ExistingSession());
        _collision.EnsureNoCollisionAsync(
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ErrorOr.Result.Success);
    }

    [Fact]
    public async Task Handle_ValidReschedule_UpdatesAndPublishesChangedEvent()
    {
        SetupHappyPath();

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.StartTime.Should().Be(NewTime);
        await _scheduleRepo.Received(1).UpdateAsync(Arg.Any<UnitSchedule>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(Arg.Any<UnitScheduleChangedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnitScheduleNotFound_Returns404()
    {
        _scheduleRepo.GetByUnitRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UnitSchedule?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.UnitScheduleNotFound");
    }

    [Fact]
    public async Task Handle_NewSessionNotFound_Returns_SessionNotFound()
    {
        _scheduleRepo.GetByUnitRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ExistingSchedule());
        _sessionRepo.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        var cmd = ValidCommand() with { SessionCode = "BOX99" };
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.SessionNotFound");
    }

    [Fact]
    public async Task Handle_CollisionExcludesSelf_Returns200()
    {
        SetupHappyPath();
        _collision.EnsureNoCollisionAsync(
            "RGB", NewTime, "BOXM57KG--------------8FNL0001----", Arg.Any<CancellationToken>())
            .Returns(ErrorOr.Result.Success);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CollisionWithDifferentUnit_ReturnsConflict()
    {
        _scheduleRepo.GetByUnitRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ExistingSchedule());
        _sessionRepo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>())
            .Returns(ExistingSession());
        _collision.EnsureNoCollisionAsync(
            "RGB", NewTime, "BOXM57KG--------------8FNL0001----", Arg.Any<CancellationToken>())
            .Returns(OVR.Modules.Scheduling.Errors.SchedulingErrors
                .LocationTimeOccupied("RGB", NewTime, "BOXM57KG--------------8FNL0005----"));

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.LocationTimeOccupied");
    }

    [Fact]
    public async Task Handle_StartTimeOutsideSession_ReturnsValidation()
    {
        _scheduleRepo.GetByUnitRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ExistingSchedule());
        _sessionRepo.GetByCodeAsync("BOX01", Arg.Any<CancellationToken>())
            .Returns(ExistingSession());

        var cmd = ValidCommand() with { StartTime = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Utc) };
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.StartTimeOutsideSession");
    }
}
```

- [ ] **Step 4: Run tests — expect compile failure**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~RescheduleUnitHandlerTests"`
Expected: compile error.

- [ ] **Step 5: Implement `RescheduleUnitHandler.cs`**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;

namespace OVR.Modules.Scheduling.Features.RescheduleUnit;

public sealed class RescheduleUnitHandler(
    ISessionRepository sessionRepository,
    IUnitScheduleRepository scheduleRepository,
    IPublisher publisher,
    IScheduleCollisionDetector collisionDetector)
    : IRequestHandler<RescheduleUnitCommand, ErrorOr<RescheduleUnitResponse>>
{
    public async Task<ErrorOr<RescheduleUnitResponse>> Handle(
        RescheduleUnitCommand request,
        CancellationToken ct)
    {
        var schedule = await scheduleRepository.GetByUnitRscAsync(request.UnitRsc, ct);
        if (schedule is null)
            return SchedulingErrors.UnitScheduleNotFound(request.UnitRsc);

        var session = await sessionRepository.GetByCodeAsync(request.SessionCode, ct);
        if (session is null)
            return SchedulingErrors.SessionNotFound(request.SessionCode);

        if (request.StartTime < session.StartDate || request.StartTime > session.EndDate)
            return SchedulingErrors.StartTimeOutsideSession(
                request.StartTime, session.StartDate, session.EndDate);

        var collisionResult = await collisionDetector.EnsureNoCollisionAsync(
            request.LocationCode, request.StartTime,
            excludeUnitRsc: request.UnitRsc, ct);
        if (collisionResult.IsError)
            return collisionResult.Errors;

        schedule.Reschedule(
            request.SessionCode, request.LocationCode, request.StartTime,
            request.OrderInSession, request.OrderInLocation, request.Reason);

        await scheduleRepository.UpdateAsync(schedule, ct);

        foreach (var e in schedule.DomainEvents)
            await publisher.Publish(e, ct);
        schedule.ClearDomainEvents();

        return new RescheduleUnitResponse(
            UnitRsc: schedule.UnitRsc.Value,
            EventRsc: schedule.EventRsc.Value,
            SessionCode: schedule.SessionCode,
            LocationCode: schedule.LocationCode,
            StartTime: schedule.StartTime,
            OrderInSession: schedule.OrderInSession,
            OrderInLocation: schedule.OrderInLocation,
            Status: schedule.Status.ToString(),
            ScheduledAt: schedule.ScheduledAt,
            UpdatedAt: schedule.UpdatedAt);
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~RescheduleUnitHandlerTests"`
Expected: 6 tests pass.

- [ ] **Step 7: Create `RescheduleUnitEndpoint.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.RescheduleUnit;

public static class RescheduleUnitEndpoint
{
    public static async Task<IResult> Handle(
        string unitRsc,
        RescheduleUnitBody body,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var command = new RescheduleUnitCommand(
            unitRsc,
            body.SessionCode,
            body.LocationCode,
            body.StartTime,
            body.OrderInSession,
            body.OrderInLocation,
            body.Reason);

        var result = await sender.Send(command, ct);
        return result.ToApiResult(httpContext);
    }
}

public sealed record RescheduleUnitBody(
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string? Reason);
```

- [ ] **Step 8: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 9: Commit**

```bash
git add src/OVR.Modules.Scheduling/Features/RescheduleUnit/ tests/OVR.Modules.Scheduling.Tests/Features/RescheduleUnit/
git commit -m "feat(scheduling): implement RescheduleUnit feature"
```

---

## Task 15: `UnscheduleUnit` feature — command, handler, endpoint, tests

**Files:**
- Create: `src/OVR.Modules.Scheduling/Features/UnscheduleUnit/UnscheduleUnitCommand.cs`
- Create: `src/OVR.Modules.Scheduling/Features/UnscheduleUnit/UnscheduleUnitHandler.cs`
- Create: `src/OVR.Modules.Scheduling/Features/UnscheduleUnit/UnscheduleUnitEndpoint.cs`
- Create: `tests/OVR.Modules.Scheduling.Tests/Features/UnscheduleUnit/UnscheduleUnitHandlerTests.cs`

Note: Unschedule has no validator beyond route param validation (handled by ASP.NET routing). If you want a FluentValidation validator for defensive purposes, it's just `RuleFor(x => x.UnitRsc).NotEmpty().Length(34);` — add a `UnscheduleUnitValidator.cs` mirroring that rule. Optional in MVP.

- [ ] **Step 1: Create `UnscheduleUnitCommand.cs`**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.UnscheduleUnit;

public sealed record UnscheduleUnitCommand(string UnitRsc)
    : IRequest<ErrorOr<Success>>;
```

- [ ] **Step 2: Write failing handler tests**

```csharp
using FluentAssertions;
using MediatR;
using NSubstitute;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Features.UnscheduleUnit;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Tests.Features.UnscheduleUnit;

public class UnscheduleUnitHandlerTests
{
    private readonly IUnitScheduleRepository _repo = Substitute.For<IUnitScheduleRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly UnscheduleUnitHandler _handler;

    public UnscheduleUnitHandlerTests()
    {
        _handler = new UnscheduleUnitHandler(_repo, _publisher);
    }

    [Fact]
    public async Task Handle_ValidUnitRsc_DeletesAndPublishesUnscheduledEvent()
    {
        var schedule = UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0001----"),
            "BOX01", "RGA",
            new DateTime(2026, 4, 20, 10, 15, 0, DateTimeKind.Utc),
            1, 1);
        _repo.GetByUnitRscAsync("BOXM57KG--------------8FNL0001----", Arg.Any<CancellationToken>())
            .Returns(schedule);

        var result = await _handler.Handle(
            new UnscheduleUnitCommand("BOXM57KG--------------8FNL0001----"),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _repo.Received(1).DeleteAsync(
            "BOXM57KG--------------8FNL0001----", Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<UnitUnscheduledEvent>(e =>
                e.UnitRsc == "BOXM57KG--------------8FNL0001----"
                && e.EventRsc == "BOXM57KG--------------------------"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotFound_Returns404()
    {
        _repo.GetByUnitRscAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((UnitSchedule?)null);

        var result = await _handler.Handle(
            new UnscheduleUnitCommand("BOXM57KG--------------8FNL0099----"),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Scheduling.UnitScheduleNotFound");
    }
}
```

- [ ] **Step 3: Run tests — expect compile failure**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~UnscheduleUnitHandlerTests"`
Expected: compile error.

- [ ] **Step 4: Implement `UnscheduleUnitHandler.cs`**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.Scheduling.Errors;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Modules.Scheduling.Features.UnscheduleUnit;

public sealed class UnscheduleUnitHandler(
    IUnitScheduleRepository repository,
    IPublisher publisher)
    : IRequestHandler<UnscheduleUnitCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(
        UnscheduleUnitCommand request,
        CancellationToken ct)
    {
        var schedule = await repository.GetByUnitRscAsync(request.UnitRsc, ct);
        if (schedule is null)
            return SchedulingErrors.UnitScheduleNotFound(request.UnitRsc);

        var eventRsc = schedule.EventRsc.Value;
        await repository.DeleteAsync(request.UnitRsc, ct);

        await publisher.Publish(
            new UnitUnscheduledEvent(request.UnitRsc, eventRsc, DateTime.UtcNow), ct);

        return Result.Success;
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~UnscheduleUnitHandlerTests"`
Expected: 2 tests pass.

- [ ] **Step 6: Create `UnscheduleUnitEndpoint.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.UnscheduleUnit;

public static class UnscheduleUnitEndpoint
{
    public static async Task<IResult> Handle(
        string unitRsc,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new UnscheduleUnitCommand(unitRsc), ct);
        return result.Match(
            _ => Results.NoContent(),
            errors => result.ToApiResult(httpContext));
    }
}
```

If `result.Match` signature doesn't match (ErrorOr 2.0 has `Match(onValue, onErrors)`), simplify by checking `result.IsError` explicitly:

```csharp
if (result.IsError)
    return result.ToApiResult(httpContext);
return Results.NoContent();
```

- [ ] **Step 7: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 8: Commit**

```bash
git add src/OVR.Modules.Scheduling/Features/UnscheduleUnit/ tests/OVR.Modules.Scheduling.Tests/Features/UnscheduleUnit/
git commit -m "feat(scheduling): implement UnscheduleUnit feature"
```

---

## Task 16: `ListUnitsByLocation` feature — query, validator, handler, endpoint, tests

**Files:**
- Create: `src/OVR.Modules.Scheduling/Features/ListUnitsByLocation/ListUnitsByLocationQuery.cs`
- Create: `src/OVR.Modules.Scheduling/Features/ListUnitsByLocation/ListUnitsByLocationValidator.cs`
- Create: `src/OVR.Modules.Scheduling/Features/ListUnitsByLocation/ListUnitsByLocationHandler.cs`
- Create: `src/OVR.Modules.Scheduling/Features/ListUnitsByLocation/ListUnitsByLocationEndpoint.cs`
- Create: `tests/OVR.Modules.Scheduling.Tests/Features/ListUnitsByLocation/ListUnitsByLocationHandlerTests.cs`

- [ ] **Step 1: Create `ListUnitsByLocationQuery.cs`**

```csharp
using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.ListUnitsByLocation;

public sealed record ListUnitsByLocationQuery(
    string LocationCode,
    DateOnly? Date) : IRequest<ErrorOr<IReadOnlyList<ScheduledUnitDto>>>;

public sealed record ScheduledUnitDto(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string Status,
    DateTime ScheduledAt);
```

- [ ] **Step 2: Create `ListUnitsByLocationValidator.cs`**

```csharp
using FluentValidation;

namespace OVR.Modules.Scheduling.Features.ListUnitsByLocation;

public sealed class ListUnitsByLocationValidator : AbstractValidator<ListUnitsByLocationQuery>
{
    public ListUnitsByLocationValidator()
    {
        RuleFor(x => x.LocationCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z0-9]{3}$");
    }
}
```

- [ ] **Step 3: Write failing handler tests**

```csharp
using FluentAssertions;
using NSubstitute;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Features.ListUnitsByLocation;
using OVR.Modules.Scheduling.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Tests.Features.ListUnitsByLocation;

public class ListUnitsByLocationHandlerTests
{
    private readonly IUnitScheduleRepository _repo = Substitute.For<IUnitScheduleRepository>();
    private readonly ListUnitsByLocationHandler _handler;

    public ListUnitsByLocationHandlerTests()
    {
        _handler = new ListUnitsByLocationHandler(_repo);
    }

    [Fact]
    public async Task Handle_WithDate_ReturnsResultsFromRepo()
    {
        var date = new DateOnly(2026, 4, 20);
        var schedule = UnitSchedule.Create(
            Rsc.Create("BOXM57KG--------------8FNL0001----"),
            "BOX01", "RGA",
            new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc),
            1, 1);
        _repo.ListByLocationAndDateAsync("RGA", date, Arg.Any<CancellationToken>())
            .Returns(new[] { schedule });

        var result = await _handler.Handle(
            new ListUnitsByLocationQuery("RGA", date),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(1);
        result.Value[0].UnitRsc.Should().Be("BOXM57KG--------------8FNL0001----");
    }

    [Fact]
    public async Task Handle_WithoutDate_UsesTodayUtc()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _repo.ListByLocationAndDateAsync("RGA", today, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UnitSchedule>());

        var result = await _handler.Handle(
            new ListUnitsByLocationQuery("RGA", null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
        await _repo.Received(1).ListByLocationAndDateAsync(
            "RGA", today, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoUnits_ReturnsEmptyList()
    {
        _repo.ListByLocationAndDateAsync(
            Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<UnitSchedule>());

        var result = await _handler.Handle(
            new ListUnitsByLocationQuery("XYZ", new DateOnly(2026, 4, 20)),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeEmpty();
    }
}
```

- [ ] **Step 4: Run tests — expect compile failure**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~ListUnitsByLocationHandlerTests"`
Expected: compile error.

- [ ] **Step 5: Implement `ListUnitsByLocationHandler.cs`**

```csharp
using ErrorOr;
using MediatR;
using OVR.Modules.Scheduling.Persistence;

namespace OVR.Modules.Scheduling.Features.ListUnitsByLocation;

public sealed class ListUnitsByLocationHandler(IUnitScheduleRepository repository)
    : IRequestHandler<ListUnitsByLocationQuery, ErrorOr<IReadOnlyList<ScheduledUnitDto>>>
{
    public async Task<ErrorOr<IReadOnlyList<ScheduledUnitDto>>> Handle(
        ListUnitsByLocationQuery request,
        CancellationToken ct)
    {
        var date = request.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var schedules = await repository.ListByLocationAndDateAsync(
            request.LocationCode, date, ct);

        var dtos = schedules
            .Select(s => new ScheduledUnitDto(
                UnitRsc: s.UnitRsc.Value,
                EventRsc: s.EventRsc.Value,
                SessionCode: s.SessionCode,
                LocationCode: s.LocationCode,
                StartTime: s.StartTime,
                OrderInSession: s.OrderInSession,
                OrderInLocation: s.OrderInLocation,
                Status: s.Status.ToString(),
                ScheduledAt: s.ScheduledAt))
            .ToList();

        return (IReadOnlyList<ScheduledUnitDto>)dtos;
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/ --filter "FullyQualifiedName~ListUnitsByLocationHandlerTests"`
Expected: 3 tests pass.

- [ ] **Step 7: Create `ListUnitsByLocationEndpoint.cs`**

```csharp
using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.ListUnitsByLocation;

public static class ListUnitsByLocationEndpoint
{
    public static async Task<IResult> Handle(
        string locationCode,
        DateOnly? date,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new ListUnitsByLocationQuery(locationCode, date), ct);
        return result.ToApiResult(httpContext);
    }
}
```

- [ ] **Step 8: Build**

Run: `dotnet build`
Expected: succeeds.

- [ ] **Step 9: Commit**

```bash
git add src/OVR.Modules.Scheduling/Features/ListUnitsByLocation/ tests/OVR.Modules.Scheduling.Tests/Features/ListUnitsByLocation/
git commit -m "feat(scheduling): implement ListUnitsByLocation query"
```

---

## Task 17: I18n files (eng/spa/por)

**Files:**
- Create: `src/OVR.Modules.Scheduling/I18n/eng.json`
- Create: `src/OVR.Modules.Scheduling/I18n/spa.json`
- Create: `src/OVR.Modules.Scheduling/I18n/por.json`

- [ ] **Step 1: Create `eng.json`**

```json
{
  "Scheduling.InvalidVenue": "Venue '{{venueCode}}' is not recognized.",
  "Scheduling.SessionAlreadyExists": "Session '{{sessionCode}}' already exists.",
  "Scheduling.SessionNotFound": "Session '{{sessionCode}}' was not found.",
  "Scheduling.StartTimeOutsideSession": "StartTime {{startTime}} is outside the session window ({{sessionStart}} – {{sessionEnd}}).",
  "Scheduling.UnitAlreadyScheduled": "Unit '{{unitRsc}}' is already scheduled. Use reschedule instead.",
  "Scheduling.LocationTimeOccupied": "Location '{{locationCode}}' is already occupied at {{startTime}} by unit '{{conflictingUnit}}'.",
  "Scheduling.UnitScheduleNotFound": "Schedule for unit '{{unitRsc}}' was not found."
}
```

- [ ] **Step 2: Create `spa.json`**

```json
{
  "Scheduling.InvalidVenue": "La sede '{{venueCode}}' no está registrada.",
  "Scheduling.SessionAlreadyExists": "Ya existe una sesión con el código '{{sessionCode}}'.",
  "Scheduling.SessionNotFound": "La sesión '{{sessionCode}}' no fue encontrada.",
  "Scheduling.StartTimeOutsideSession": "La hora de inicio {{startTime}} está fuera de la ventana de la sesión ({{sessionStart}} – {{sessionEnd}}).",
  "Scheduling.UnitAlreadyScheduled": "La unidad '{{unitRsc}}' ya está programada. Use reprogramar en su lugar.",
  "Scheduling.LocationTimeOccupied": "La ubicación '{{locationCode}}' ya está ocupada a las {{startTime}} por la unidad '{{conflictingUnit}}'.",
  "Scheduling.UnitScheduleNotFound": "No se encontró programación para la unidad '{{unitRsc}}'."
}
```

- [ ] **Step 3: Create `por.json`**

```json
{
  "Scheduling.InvalidVenue": "A sede '{{venueCode}}' não está registrada.",
  "Scheduling.SessionAlreadyExists": "Já existe uma sessão com o código '{{sessionCode}}'.",
  "Scheduling.SessionNotFound": "A sessão '{{sessionCode}}' não foi encontrada.",
  "Scheduling.StartTimeOutsideSession": "A hora de início {{startTime}} está fora da janela da sessão ({{sessionStart}} – {{sessionEnd}}).",
  "Scheduling.UnitAlreadyScheduled": "A unidade '{{unitRsc}}' já está programada. Use reprogramar.",
  "Scheduling.LocationTimeOccupied": "O local '{{locationCode}}' já está ocupado às {{startTime}} pela unidade '{{conflictingUnit}}'.",
  "Scheduling.UnitScheduleNotFound": "Programação para a unidade '{{unitRsc}}' não foi encontrada."
}
```

- [ ] **Step 4: Build**

Run: `dotnet build src/OVR.Modules.Scheduling/`
Expected: succeeds and output includes `I18n.Scheduling/{eng,spa,por}.json`.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.Scheduling/I18n/
git commit -m "feat(scheduling): add i18n translations for all error messages"
```

---

## Task 18: Wire up `SchedulingModule` DI + endpoints

**Files:**
- Modify: `src/OVR.Modules.Scheduling/SchedulingModule.cs`
- Modify: `src/OVR.Api/Program.cs` (verify assembly registered)

- [ ] **Step 1: Replace `SchedulingModule.cs` contents**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Features.CreateSession;
using OVR.Modules.Scheduling.Features.ListUnitsByLocation;
using OVR.Modules.Scheduling.Features.RescheduleUnit;
using OVR.Modules.Scheduling.Features.ScheduleUnit;
using OVR.Modules.Scheduling.Features.UnscheduleUnit;
using OVR.Modules.Scheduling.Persistence;

namespace OVR.Modules.Scheduling;

public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services)
    {
        services.AddScoped<ISessionRepository, MongoSessionRepository>();
        services.AddScoped<IUnitScheduleRepository, MongoUnitScheduleRepository>();
        services.AddScoped<IScheduleCollisionDetector, ScheduleCollisionDetector>();
        return services;
    }

    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduling")
            .WithTags("Scheduling");

        group.MapPost("/sessions", CreateSessionEndpoint.Handle)
            .WithName("CreateSession");

        group.MapPost("/sessions/{sessionCode}/schedule-unit", ScheduleUnitEndpoint.Handle)
            .WithName("ScheduleUnit");

        group.MapPatch("/unit-schedules/{unitRsc}", RescheduleUnitEndpoint.Handle)
            .WithName("RescheduleUnit");

        group.MapDelete("/unit-schedules/{unitRsc}", UnscheduleUnitEndpoint.Handle)
            .WithName("UnscheduleUnit");

        group.MapGet("/locations/{locationCode}/today", ListUnitsByLocationEndpoint.Handle)
            .WithName("ListUnitsByLocation");

        return app;
    }
}
```

- [ ] **Step 2: Verify Program.cs registers the Scheduling assembly**

Run: `grep -n "SchedulingModule\|Scheduling" src/OVR.Api/Program.cs`

Expected: the file should reference `typeof(SchedulingModule).Assembly` in both the `AddMediatR(...)` and `AddValidatorsFromAssemblies(...)` calls, plus `AddSchedulingModule()` and `MapSchedulingEndpoints()`.

If any is missing (particularly in FluentValidation), add them. The file already has `AddSchedulingModule()` and `MapSchedulingEndpoints()` — just ensure the assembly is in the validator registration.

- [ ] **Step 3: Run the full module test suite**

Run: `dotnet test tests/OVR.Modules.Scheduling.Tests/`
Expected: all tests pass (27+ unit tests).

- [ ] **Step 4: Build whole solution**

Run: `dotnet build`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/OVR.Modules.Scheduling/SchedulingModule.cs src/OVR.Api/Program.cs
git commit -m "feat(scheduling): wire module DI, endpoints, and FluentValidation registration"
```

(If Program.cs didn't need changes, drop it from the `git add`.)

---

## Task 19: Integration test fixture — `SchedulingWebAppFactory`

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/Scheduling/Support/SchedulingWebAppFactory.cs`
- Modify: `tests/OVR.Api.IntegrationTests/OVR.Api.IntegrationTests.csproj` (already references CommonCodes from MVP 1; verify)

- [ ] **Step 1: Verify the test project's csproj**

Run: `cat tests/OVR.Api.IntegrationTests/OVR.Api.IntegrationTests.csproj`

Expected: should already reference `OVR.Modules.CommonCodes` from MVP 1. If missing, add:

```xml
<ProjectReference Include="..\..\src\OVR.Modules.CommonCodes\OVR.Modules.CommonCodes.csproj" />
```

- [ ] **Step 2: Create the factory**

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using OVR.Modules.CommonCodes.Persistence;
using Testcontainers.MongoDb;

namespace OVR.Api.IntegrationTests.Scheduling.Support;

public sealed class SchedulingWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:8")
        .Build();

    private const string DatabaseName = "ovr_scheduling_tests";

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
                ["MongoDb:ConnectionString"] = _mongo.GetConnectionString(),
                ["MongoDb:DatabaseName"] = DatabaseName
            });
        });
    }

    private async Task SeedCommonCodesAsync()
    {
        var client = new MongoClient(_mongo.GetConnectionString());
        var db = client.GetDatabase(DatabaseName);
        var collection = db.GetCollection<CommonCodeDocument>("commonCodes_codes");

        var seed = new List<CommonCodeDocument>
        {
            new() { Id = "VENUES:ABC", Type = "VENUES", Code = "ABC", Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Arena Boxing Center" } }, Attributes = [] },
            new() { Id = "VENUES:DEF", Type = "VENUES", Code = "DEF", Order = 2,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Secondary Arena" } }, Attributes = [] },
        };

        await collection.InsertManyAsync(seed);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build tests/OVR.Api.IntegrationTests/`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/Scheduling/Support/SchedulingWebAppFactory.cs
git commit -m "test(scheduling): add WebAppFactory for integration tests"
```

---

## Task 20: Integration tests — `CreateSessionEndpointTests`

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/Scheduling/CreateSessionEndpointTests.cs`

- [ ] **Step 1: Write integration tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class CreateSessionEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public CreateSessionEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_ValidPayload_Returns201()
    {
        var body = new
        {
            code = "BOX01",
            venueCode = "ABC",
            name = "Boxing Session 1",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = "00:05:00"
        };

        var response = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Contain("BOX01");
    }

    [Fact]
    public async Task POST_UnknownVenue_Returns400()
    {
        var body = new
        {
            code = "BOX02",
            venueCode = "ZZZ",
            name = "x",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = (TimeSpan?)null
        };

        var response = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("Scheduling.InvalidVenue");
    }

    [Fact]
    public async Task POST_DuplicateCode_Returns409()
    {
        var body = new
        {
            code = "BOX03",
            venueCode = "ABC",
            name = "duplicate test",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = (TimeSpan?)null
        };

        await _client.PostAsJsonAsync("/api/scheduling/sessions", body);
        var second = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_EndBeforeStart_Returns400FromValidator()
    {
        var body = new
        {
            code = "BOX04",
            venueCode = "ABC",
            name = "bad dates",
            startDate = "2026-04-20T14:00:00Z",
            endDate = "2026-04-20T10:00:00Z",
            leadin = (TimeSpan?)null
        };

        var response = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~CreateSessionEndpointTests"`
Expected: 4 tests pass (may take ~30s for Mongo container).

- [ ] **Step 3: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/Scheduling/CreateSessionEndpointTests.cs
git commit -m "test(scheduling): add integration tests for CreateSession endpoint"
```

---

## Task 21: Integration tests — `ScheduleUnitEndpointTests`

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/Scheduling/ScheduleUnitEndpointTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class ScheduleUnitEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public ScheduleUnitEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> EnsureSessionAsync(string code = "BOX10")
    {
        var body = new
        {
            code,
            venueCode = "ABC",
            name = $"Session {code}",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = "00:05:00"
        };
        var response = await _client.PostAsJsonAsync("/api/scheduling/sessions", body);
        // 201 on first call, 409 on subsequent — we don't care here, we just want the session to exist
        return code;
    }

    [Fact]
    public async Task POST_ValidPayload_Returns201()
    {
        var session = await EnsureSessionAsync("BOX10");
        var body = new
        {
            unitRsc = "BOXM57KG--------------8FNL0001----",
            locationCode = "RGA",
            startTime = "2026-04-20T10:15:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", body);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task POST_MissingSession_Returns404()
    {
        var body = new
        {
            unitRsc = "BOXM57KG--------------8FNL0002----",
            locationCode = "RGA",
            startTime = "2026-04-20T11:00:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };

        var response = await _client.PostAsJsonAsync(
            "/api/scheduling/sessions/MISSING/schedule-unit", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_StartTimeBeforeSession_Returns400()
    {
        var session = await EnsureSessionAsync("BOX11");
        var body = new
        {
            unitRsc = "BOXM57KG--------------8FNL0003----",
            locationCode = "RGA",
            startTime = "2026-04-20T08:00:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("StartTimeOutsideSession");
    }

    [Fact]
    public async Task POST_AlreadyScheduled_Returns409()
    {
        var session = await EnsureSessionAsync("BOX12");
        var body = new
        {
            unitRsc = "BOXM57KG--------------8FNL0004----",
            locationCode = "RGA",
            startTime = "2026-04-20T12:00:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };

        await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", body);

        var second = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await second.Content.ReadAsStringAsync()).Should().Contain("UnitAlreadyScheduled");
    }

    [Fact]
    public async Task POST_CollisionAtSameLocationTime_Returns409()
    {
        var session = await EnsureSessionAsync("BOX13");
        var first = new
        {
            unitRsc = "BOXM57KG--------------8FNL0005----",
            locationCode = "RGA",
            startTime = "2026-04-20T13:00:00Z",
            orderInSession = 1,
            orderInLocation = 1
        };
        var colliding = new
        {
            unitRsc = "BOXM57KG--------------8FNL0006----",
            locationCode = "RGA",
            startTime = "2026-04-20T13:00:00Z",  // same time + same location
            orderInSession = 2,
            orderInLocation = 2
        };

        await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", first);

        var response = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{session}/schedule-unit", colliding);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("LocationTimeOccupied");
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~ScheduleUnitEndpointTests"`
Expected: 5 tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/Scheduling/ScheduleUnitEndpointTests.cs
git commit -m "test(scheduling): add integration tests for ScheduleUnit endpoint"
```

---

## Task 22: Integration tests — `RescheduleUnitEndpointTests`

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/Scheduling/RescheduleUnitEndpointTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class RescheduleUnitEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public RescheduleUnitEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task EnsureSessionAsync(string code) =>
        await _client.PostAsJsonAsync("/api/scheduling/sessions", new
        {
            code,
            venueCode = "ABC",
            name = $"Session {code}",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = (TimeSpan?)null
        });

    private async Task<string> ScheduleUnitAsync(
        string sessionCode, string unitRsc, string locationCode, string startTime,
        int orderInSession = 1, int orderInLocation = 1)
    {
        var body = new { unitRsc, locationCode, startTime, orderInSession, orderInLocation };
        var response = await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/{sessionCode}/schedule-unit", body);
        response.EnsureSuccessStatusCode();
        return unitRsc;
    }

    [Fact]
    public async Task PATCH_ValidNewTime_Returns200()
    {
        await EnsureSessionAsync("BOX20");
        var unitRsc = await ScheduleUnitAsync(
            "BOX20", "BOXM57KG--------------8FNL0010----", "RGA", "2026-04-20T10:15:00Z");

        var body = new
        {
            sessionCode = "BOX20",
            locationCode = "RGB",
            startTime = "2026-04-20T11:00:00Z",
            orderInSession = 2,
            orderInLocation = 1,
            reason = "mat swap"
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/scheduling/unit-schedules/{unitRsc}", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PATCH_NotFound_Returns404()
    {
        var body = new
        {
            sessionCode = "BOX20",
            locationCode = "RGA",
            startTime = "2026-04-20T11:00:00Z",
            orderInSession = 1,
            orderInLocation = 1,
            reason = (string?)null
        };

        var response = await _client.PatchAsJsonAsync(
            "/api/scheduling/unit-schedules/BOXM99KG--------------8FNL9999----", body);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_SelfCollisionIgnored_Returns200()
    {
        await EnsureSessionAsync("BOX21");
        var unitRsc = await ScheduleUnitAsync(
            "BOX21", "BOXM57KG--------------8FNL0011----", "RGA", "2026-04-20T10:30:00Z");

        // Same location and time — should NOT self-collide
        var body = new
        {
            sessionCode = "BOX21",
            locationCode = "RGA",
            startTime = "2026-04-20T10:30:00Z",
            orderInSession = 5,  // only order changes
            orderInLocation = 5,
            reason = (string?)null
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/scheduling/unit-schedules/{unitRsc}", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PATCH_CollisionWithOther_Returns409()
    {
        await EnsureSessionAsync("BOX22");
        await ScheduleUnitAsync(
            "BOX22", "BOXM57KG--------------8FNL0012----", "RGA", "2026-04-20T10:45:00Z");
        var target = await ScheduleUnitAsync(
            "BOX22", "BOXM57KG--------------8FNL0013----", "RGA", "2026-04-20T11:00:00Z", 2, 2);

        // Try to reschedule target onto first unit's slot
        var body = new
        {
            sessionCode = "BOX22",
            locationCode = "RGA",
            startTime = "2026-04-20T10:45:00Z",  // collides with first
            orderInSession = 1,
            orderInLocation = 1,
            reason = (string?)null
        };

        var response = await _client.PatchAsJsonAsync(
            $"/api/scheduling/unit-schedules/{target}", body);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~RescheduleUnitEndpointTests"`
Expected: 4 tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/Scheduling/RescheduleUnitEndpointTests.cs
git commit -m "test(scheduling): add integration tests for RescheduleUnit endpoint"
```

---

## Task 23: Integration tests — `UnscheduleUnitEndpointTests` + `ListUnitsByLocationEndpointTests`

**Files:**
- Create: `tests/OVR.Api.IntegrationTests/Scheduling/UnscheduleUnitEndpointTests.cs`
- Create: `tests/OVR.Api.IntegrationTests/Scheduling/ListUnitsByLocationEndpointTests.cs`

- [ ] **Step 1: Create `UnscheduleUnitEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class UnscheduleUnitEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public UnscheduleUnitEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DELETE_Existing_Returns204AndPersistenceIsGone()
    {
        await _client.PostAsJsonAsync("/api/scheduling/sessions", new
        {
            code = "BOX30",
            venueCode = "ABC",
            name = "unschedule test",
            startDate = "2026-04-20T10:00:00Z",
            endDate = "2026-04-20T14:00:00Z",
            leadin = (TimeSpan?)null
        });

        await _client.PostAsJsonAsync(
            "/api/scheduling/sessions/BOX30/schedule-unit",
            new
            {
                unitRsc = "BOXM57KG--------------8FNL0020----",
                locationCode = "RGA",
                startTime = "2026-04-20T10:30:00Z",
                orderInSession = 1,
                orderInLocation = 1
            });

        var response = await _client.DeleteAsync(
            "/api/scheduling/unit-schedules/BOXM57KG--------------8FNL0020----");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify gone: re-scheduling the same RSC should succeed (not conflict)
        var reSchedule = await _client.PostAsJsonAsync(
            "/api/scheduling/sessions/BOX30/schedule-unit",
            new
            {
                unitRsc = "BOXM57KG--------------8FNL0020----",
                locationCode = "RGB",
                startTime = "2026-04-20T11:00:00Z",
                orderInSession = 2,
                orderInLocation = 1
            });
        reSchedule.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DELETE_NotFound_Returns404()
    {
        var response = await _client.DeleteAsync(
            "/api/scheduling/unit-schedules/BOXM99KG--------------8FNL9999----");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 2: Create `ListUnitsByLocationEndpointTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OVR.Api.IntegrationTests.Scheduling.Support;

namespace OVR.Api.IntegrationTests.Scheduling;

public class ListUnitsByLocationEndpointTests : IClassFixture<SchedulingWebAppFactory>
{
    private readonly HttpClient _client;

    public ListUnitsByLocationEndpointTests(SchedulingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task SeedScheduledUnitAsync(string unitSuffix, string locationCode, string startTime)
    {
        await _client.PostAsJsonAsync("/api/scheduling/sessions", new
        {
            code = $"BOX{unitSuffix}",
            venueCode = "ABC",
            name = $"Session {unitSuffix}",
            startDate = "2026-04-21T08:00:00Z",
            endDate = "2026-04-21T20:00:00Z",
            leadin = (TimeSpan?)null
        });

        await _client.PostAsJsonAsync(
            $"/api/scheduling/sessions/BOX{unitSuffix}/schedule-unit",
            new
            {
                unitRsc = $"BOXM57KG--------------8FNL{unitSuffix}----",
                locationCode,
                startTime,
                orderInSession = 1,
                orderInLocation = 1
            });
    }

    [Fact]
    public async Task GET_WithScheduledUnits_ReturnsSortedByStartTime()
    {
        await SeedScheduledUnitAsync("0050", "RGA", "2026-04-21T14:00:00Z");
        await SeedScheduledUnitAsync("0051", "RGA", "2026-04-21T11:00:00Z");
        await SeedScheduledUnitAsync("0052", "RGA", "2026-04-21T13:00:00Z");

        var response = await _client.GetAsync(
            "/api/scheduling/locations/RGA/today?date=2026-04-21");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        // Expect order: 11:00, 13:00, 14:00 (by startTime ascending)
        var idx0051 = payload.IndexOf("8FNL0051", StringComparison.Ordinal);
        var idx0052 = payload.IndexOf("8FNL0052", StringComparison.Ordinal);
        var idx0050 = payload.IndexOf("8FNL0050", StringComparison.Ordinal);
        idx0051.Should().BeGreaterThan(0);
        idx0051.Should().BeLessThan(idx0052);
        idx0052.Should().BeLessThan(idx0050);
    }

    [Fact]
    public async Task GET_NoUnitsAtLocation_ReturnsEmpty()
    {
        var response = await _client.GetAsync(
            "/api/scheduling/locations/ZZZ/today?date=2026-04-21");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Be("[]");
    }

    [Fact]
    public async Task GET_FiltersToRequestedDate_IgnoresOtherDays()
    {
        await SeedScheduledUnitAsync("0060", "RGC", "2026-04-22T10:00:00Z");
        await SeedScheduledUnitAsync("0061", "RGC", "2026-04-23T10:00:00Z");

        var response = await _client.GetAsync(
            "/api/scheduling/locations/RGC/today?date=2026-04-22");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadAsStringAsync();
        payload.Should().Contain("8FNL0060");
        payload.Should().NotContain("8FNL0061");
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/OVR.Api.IntegrationTests/ --filter "FullyQualifiedName~Scheduling"`
Expected: all Scheduling integration tests pass (should be ~15 total: 4 CreateSession + 5 ScheduleUnit + 4 RescheduleUnit + 2 UnscheduleUnit + 3 ListByLocation).

- [ ] **Step 4: Commit**

```bash
git add tests/OVR.Api.IntegrationTests/Scheduling/UnscheduleUnitEndpointTests.cs tests/OVR.Api.IntegrationTests/Scheduling/ListUnitsByLocationEndpointTests.cs
git commit -m "test(scheduling): add integration tests for Unschedule and ListUnitsByLocation endpoints"
```

---

## Task 24: Final verification — run full test suite + smoke test

- [ ] **Step 1: Run full test suite**

Run: `dotnet test`
Expected: all previous tests still pass; the full suite runs clean.

- [ ] **Step 2: Smoke test via the API (optional, needs Docker Mongo)**

```bash
docker compose --profile db up -d
dotnet run --project src/OVR.Api &
sleep 5
```

Quick manual sanity check:

```bash
# Create session
curl -i -X POST http://localhost:5000/api/scheduling/sessions \
  -H 'Content-Type: application/json' \
  -d '{"code":"BOX01","venueCode":"ABC","name":"Test","startDate":"2026-04-20T10:00:00Z","endDate":"2026-04-20T14:00:00Z","leadin":"00:05:00"}'

# Schedule a unit (assumes Venue ABC and discipline/event codes seeded in your local Mongo; or ignore 400 on unknown venue)
curl -i -X POST http://localhost:5000/api/scheduling/sessions/BOX01/schedule-unit \
  -H 'Content-Type: application/json' \
  -d '{"unitRsc":"BOXM57KG--------------8FNL0001----","locationCode":"RGA","startTime":"2026-04-20T10:15:00Z","orderInSession":1,"orderInLocation":1}'

# List
curl -i 'http://localhost:5000/api/scheduling/locations/RGA/today?date=2026-04-20'
```

Kill the API: `kill %1`

- [ ] **Step 3: Final commit if any ad-hoc fixes made**

```bash
git status
# if anything needs committing:
git add <specific paths>
git commit -m "chore: final verification tweaks"
```

- [ ] **Step 4: Branch is ready for review and merge.**

---

## Self-Review Checklist (executed while writing the plan)

**1. Spec coverage:**

| Spec section | Task(s) |
|---|---|
| Delete wrong Unit/UnitStatus stubs | Task 1 |
| Add WellKnownCodeTypes.Location | Task 1 |
| Scheduling csproj packages + CommonCodes ref | Task 2 |
| UnitScheduledEvent (new) | Task 3 |
| UnitUnscheduledEvent (new) | Task 3 |
| UnitScheduleChangedEvent (rewrite) | Task 3 |
| ScheduleStatus enum | Task 5 |
| Session aggregate | Task 6 |
| UnitSchedule aggregate | Task 7 |
| SchedulingErrors | Task 8 |
| Session persistence | Task 9 |
| UnitSchedule persistence + Hydrate helper | Task 10 |
| ScheduleCollisionDetector | Task 11 |
| CreateSession feature | Task 12 |
| ScheduleUnit feature | Task 13 |
| RescheduleUnit feature | Task 14 |
| UnscheduleUnit feature | Task 15 |
| ListUnitsByLocation feature | Task 16 |
| I18n translations | Task 17 |
| SchedulingModule wiring | Task 18 |
| Integration fixture | Task 19 |
| CreateSession integration tests | Task 20 |
| ScheduleUnit integration tests | Task 21 |
| RescheduleUnit integration tests | Task 22 |
| Unschedule + ListByLocation integration tests | Task 23 |
| Final verification | Task 24 |

All spec sections covered.

**2. Placeholder scan:** No "TBD", "TODO", or "similar to X" patterns. Notes like "adjust if ErrorOr API differs" are genuine flexibility notes for the implementer given ErrorOr 2.0 variations — not placeholders.

**3. Type consistency:** Names match across tasks — `Session`, `UnitSchedule`, `ScheduleStatus`, `ScheduleCollisionDetector`, `IScheduleCollisionDetector`, `SchedulingErrors`, `SessionDocument`, `UnitScheduleDocument`, `ISessionRepository`, `IUnitScheduleRepository`, `MongoSessionRepository`, `MongoUnitScheduleRepository`, `CreateSessionCommand`, `CreateSessionHandler`, `CreateSessionEndpoint`, `CreateSessionResponse`, `ScheduleUnitCommand`, `ScheduleUnitHandler`, `ScheduleUnitEndpoint`, `ScheduleUnitBody`, `ScheduleUnitResponse`, `RescheduleUnitCommand`, `RescheduleUnitHandler`, `RescheduleUnitEndpoint`, `RescheduleUnitBody`, `RescheduleUnitResponse`, `UnscheduleUnitCommand`, `UnscheduleUnitHandler`, `UnscheduleUnitEndpoint`, `ListUnitsByLocationQuery`, `ListUnitsByLocationHandler`, `ListUnitsByLocationEndpoint`, `ScheduledUnitDto`, `UnitScheduledEvent`, `UnitScheduleChangedEvent`, `UnitUnscheduledEvent`, `SchedulingWebAppFactory`.

**4. Deliverable check:** After Task 24 the operator can create Sessions, schedule/reschedule/unschedule Units, and list Units by location+date. All 3 integration events dispatch correctly. Integration tests verify end-to-end with real Mongo.
