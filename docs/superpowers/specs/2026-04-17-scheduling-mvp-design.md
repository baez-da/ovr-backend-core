# Scheduling MVP — Design Spec

**Date**: 2026-04-17
**Status**: Draft — pending user review
**Step**: 2 of 5 in the MVP roadmap for Eugenio's demo

## Context

This is step 2 of the thin end-to-end MVP slice for OVR, following CompetitionConfig MVP (step 1). The goal is to let the operator build the operational calendar: create `Session` time blocks and assign Units (from CompetitionConfig) to `(Session, Location, StartTime, Order)`, emitting integration events that downstream modules (DataEntry, DataDistribution) will consume in later MVPs.

**Deliverable**: an operator can `POST /sessions` to create a Session at a Venue, then `POST /sessions/{code}/schedule-unit` repeatedly to assign bracket Units to specific mats at specific times. The operator can `PATCH` a schedule (reschedule), `DELETE` a schedule (unschedule), and `GET` all Units scheduled at a given Location on a given date.

**Scope**: boxing pilot. Single Venue + multiple Mats (Locations). No UI yet — API only. No coupling to Entries or DataEntry yet. Unit RSCs are trusted from the client; no cross-module validation that the Unit exists in CompetitionConfig (would couple contexts unnecessarily at MVP stage).

**Reference docs**:
- `docs/sessions-and-units.md` — ODF Session/Unit fundamentals
- `docs/odf-domain-structure.md` — RSC hierarchy, message mapping
- `docs/superpowers/specs/2026-04-17-competitionconfig-mvp-design.md` — MVP 1 (pattern template)
- `CLAUDE.md` — module conventions (vertical-slice, 3-level validation, common codes)

## Design decisions summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| `Session` boundary | Lightweight aggregate (metadata only) | Changes rarely; operational granularity lives in UnitSchedule |
| `UnitSchedule` boundary | Separate aggregate, `UnitRsc` as Id | Operator changes schedule per-unit; avoids loading Session for each reschedule |
| Creation strategy | Lazy (on-demand) | Scheduling doesn't react to `EventStructureGeneratedEvent`; operator creates UnitSchedules explicitly |
| StartTime calculation | Explicit from caller | Defers duration model to DataEntry MVP when Round/Period concepts appear |
| API shape | REST with distinct verbs per intent | Matches Entries/CompetitionConfig conventions; maps intent→event cleanly |
| Collision rule | Unique `(LocationCode, StartTime)` per UnitSchedule | Cheap guard against obvious operator error |
| Unschedule semantics | Hard delete | Simpler than soft-delete; audit can be added later via event log |
| Integration events | 3 events, all full snapshots | `UnitScheduledEvent` (new), `UnitScheduleChangedEvent` (replace existing), `UnitUnscheduledEvent` (new) |
| Location CC validation | Deferred | Follow MVP 1 pattern with PHASE_TYPE |
| Venue CC validation | Enforced at Session creation | `WellKnownCodeTypes.Venue` already exists |

## Section 1 — Architecture and bounded context

Scheduling owns the **operational calendar** of the competition: which Sessions exist, and when/where each Unit competes within them.

**Responsibilities:**
- Create `Session` instances (time block at a Venue)
- Assign Units to `(Session, Location, StartTime, Order)` — create `UnitSchedule`
- Reschedule / unschedule UnitSchedules
- Serve operator read query "what's on Mat X today"
- Emit integration events (`UnitScheduledEvent`, `UnitScheduleChangedEvent`, `UnitUnscheduledEvent`)

**Not its responsibilities:**
- Knowing which Units exist structurally (that's CompetitionConfig — Scheduling accepts RSC from client)
- Knowing competitors in a Unit (that's DataEntry/Entries)
- Results (that's DataEntry)
- Modeling venues/mats as entities (they're CC `@Venue` and `@Location`)
- Duration modeling or auto-time calculation (deferred to future MVP)

**Outbound dependencies:**
- `ICommonCodeCache` (SharedKernel) — validate `VenueCode` at Session creation
- `Rsc` value object (SharedKernel) — parse UnitRsc, derive EventRsc
- `MediatR.IPublisher` — dispatch integration events

**Inbound dependencies (MVP 2):** none. Future MVPs:
- **DataEntry (MVP 3)** consumes `UnitScheduledEvent` to create `UnitResult` at status `START_LIST`
- **DataDistribution (MVP 5)** consumes all events to emit `DT_SCHEDULE` / `DT_SCHEDULE_UPDATE`

**Folder structure:**

```
OVR.Modules.Scheduling/
├── SchedulingModule.cs              # DI + endpoint mapping
├── Domain/
│   ├── Session.cs                   # aggregate
│   ├── UnitSchedule.cs              # aggregate
│   ├── ScheduleStatus.cs            # enum aligned with CC@ScheduleStatus
│   └── ScheduleCollisionDetector.cs # domain service for collision check
├── Features/
│   ├── CreateSession/
│   ├── ScheduleUnit/
│   ├── RescheduleUnit/
│   ├── UnscheduleUnit/
│   └── ListUnitsByLocation/
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
```

**Cleanup:**
- Delete `Domain/Unit.cs` (name collides with `CompetitionConfig.Domain.Unit`; scaffolding duplicate)
- Delete `Domain/UnitStatus.cs` (mixes schedule and result statuses — anti-pattern)
- Replace `SharedKernel/Domain/Events/Integration/UnitScheduleChangedEvent.cs` (current version has 3 fields — insufficient)
- Add `WellKnownCodeTypes.Location = "LOCATION"` constant (deferred validation)

## Section 2 — Domain components

### `Session` aggregate

Identity: `SessionCode` (string, ODF format `DDDnn`, 1..10 chars).

```csharp
public sealed class Session : AggregateRoot<string>
{
    public string Code { get; private set; }
    public string VenueCode { get; private set; }
    public string Name { get; private set; }
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
        TimeSpan? leadin);
}
```

**Invariants enforced in `Create`:**
- `code` non-empty, regex `^[A-Z0-9]+$`, length 1..10
- `venueCode` non-empty, exactly 3 uppercase alphanumeric
- `name` non-empty, length 1..40
- `endDate > startDate`
- `leadin` if present, `>= TimeSpan.Zero`

**No lifecycle methods in MVP** (no cancel, no update). Delete + recreate if changes needed.

### `UnitSchedule` aggregate

Identity: `UnitRsc` (34 chars, Unit-level RSC).

```csharp
public sealed class UnitSchedule : AggregateRoot<string>
{
    public Rsc UnitRsc { get; private set; }
    public Rsc EventRsc { get; private set; }
    public string SessionCode { get; private set; }
    public string LocationCode { get; private set; }
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
        int orderInLocation);
        // Raises UnitScheduledEvent

    public void Reschedule(
        string newSessionCode,
        string newLocationCode,
        DateTime newStartTime,
        int newOrderInSession,
        int newOrderInLocation,
        string? reason);
        // Raises UnitScheduleChangedEvent
}
```

**Invariants in `Create`:**
- `unitRsc.IsAtLevel(RscLevel.Unit)` (throws `ArgumentException` otherwise)
- `sessionCode` non-empty
- `locationCode` non-empty, exactly 3 uppercase alphanumeric
- `orderInSession >= 1`, `orderInLocation >= 1`
- `Status = Scheduled` by default

**Invariants in `Reschedule`:**
- `Status == Scheduled` (future CANCELLED states rejected)
- Sets `UpdatedAt = UtcNow`

### `ScheduleStatus` enum

```csharp
public enum ScheduleStatus
{
    Scheduled = 1
    // CANCELLED, RESCHEDULED, POSTPONED, UNSCHEDULED — reserved for future MVPs
}
```

Aligned with ODF `CC @ScheduleStatus`. MVP only uses `Scheduled`. Does NOT mix with result statuses (which will live in DataEntry's `UnitResult`).

### `ScheduleCollisionDetector` domain service

```csharp
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
        CancellationToken ct)
    {
        var existing = await repo.FindByLocationAndTimeAsync(locationCode, startTime, ct);
        if (existing is null) return Result.Success;
        if (existing.UnitRsc.Value == excludeUnitRsc) return Result.Success;
        return SchedulingErrors.LocationTimeOccupied(
            locationCode, startTime, existing.UnitRsc.Value);
    }
}
```

Service (not method on aggregate) because the invariant queries another aggregate (UnitSchedule by location+time). The `excludeUnitRsc` parameter lets reschedule flows skip self-collision when only non-locating fields change.

### Repositories

```csharp
public interface ISessionRepository
{
    Task<Session?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct = default);
}

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

### Integration events (SharedKernel)

Replace the existing thin `UnitScheduleChangedEvent`. Add two new events.

```csharp
// src/OVR.SharedKernel/Domain/Events/Integration/UnitScheduledEvent.cs (NEW)
public sealed record UnitScheduledEvent(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    DateTime ScheduledAt
) : DomainEventBase;

// src/OVR.SharedKernel/Domain/Events/Integration/UnitScheduleChangedEvent.cs (REWRITE)
public sealed record UnitScheduleChangedEvent(
    string UnitRsc,
    string EventRsc,
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string? Reason,
    DateTime ChangedAt
) : DomainEventBase;

// src/OVR.SharedKernel/Domain/Events/Integration/UnitUnscheduledEvent.cs (NEW)
public sealed record UnitUnscheduledEvent(
    string UnitRsc,
    string EventRsc,
    DateTime UnscheduledAt
) : DomainEventBase;
```

All payloads are **full snapshots**, not diffs. Downstream consumers receive complete state and don't need to track previous values.

## Section 3 — Data flows

### Flow 1: Create Session

```
POST /api/scheduling/sessions
Body: { code, venueCode, name, startDate, endDate, leadin? }
   ↓
CreateSessionEndpoint → MediatR.Send(CreateSessionCommand)
   ↓
[LoggingBehavior → ValidationBehavior]
CreateSessionValidator: field shapes + endDate > startDate
   ↓
CreateSessionHandler
   ├─ cache.Exists("VENUES", venueCode) → else InvalidVenue
   ├─ sessionRepo.GetByCodeAsync(code) → if exists, SessionAlreadyExists
   ├─ Session.Create(...)
   └─ sessionRepo.AddAsync(session)
   ↓
201 Created { code, venueCode, name, startDate, endDate, leadin, createdAt }
Location: /api/scheduling/sessions/{code}
```

No integration event emitted — empty sessions don't interest downstream consumers.

### Flow 2: Schedule Unit

```
POST /api/scheduling/sessions/{sessionCode}/schedule-unit
Body: { unitRsc, locationCode, startTime, orderInSession, orderInLocation }
   ↓
ScheduleUnitEndpoint → MediatR.Send(ScheduleUnitCommand)
   ↓
[Validator] unitRsc length==34, locationCode length==3, orders >= 1
   ↓
ScheduleUnitHandler
   ├─ sessionRepo.GetByCodeAsync(sessionCode) → else SessionNotFound
   ├─ If startTime < session.StartDate || startTime > session.EndDate
   │    → StartTimeOutsideSession
   ├─ scheduleRepo.GetByUnitRscAsync(unitRsc) → if exists, UnitAlreadyScheduled
   ├─ collisionDetector.EnsureNoCollisionAsync(locationCode, startTime, null)
   │    → on failure, LocationTimeOccupied
   ├─ UnitSchedule.Create(...) — raises UnitScheduledEvent
   ├─ scheduleRepo.AddAsync(schedule)
   ├─ foreach domainEvent: publisher.Publish(event)
   └─ schedule.ClearDomainEvents()
   ↓
201 Created { unitRsc, sessionCode, locationCode, startTime, orders, status, scheduledAt }
Location: /api/scheduling/unit-schedules/{unitRsc}
```

### Flow 3: Reschedule Unit

```
PATCH /api/scheduling/unit-schedules/{unitRsc}
Body: { sessionCode, locationCode, startTime, orderInSession, orderInLocation, reason? }
   ↓
RescheduleUnitHandler
   ├─ scheduleRepo.GetByUnitRscAsync(unitRsc) → else UnitScheduleNotFound
   ├─ If newSessionCode != current.SessionCode:
   │    sessionRepo.GetByCodeAsync(newSessionCode) → else SessionNotFound
   │    + validate startTime within new session's window
   ├─ Else validate startTime within the (same) session's window
   ├─ collisionDetector.EnsureNoCollisionAsync(locationCode, startTime, unitRsc)
   │    → excludes self, so only true conflicts flagged
   ├─ schedule.Reschedule(...) — raises UnitScheduleChangedEvent
   ├─ scheduleRepo.UpdateAsync(schedule)
   └─ publish events
   ↓
200 OK { ... }
```

PATCH semantic: client sends **full desired state** (all fields required in body). Partial-patch rejected to keep handler simple.

### Flow 4: Unschedule Unit

```
DELETE /api/scheduling/unit-schedules/{unitRsc}
   ↓
UnscheduleUnitHandler
   ├─ scheduleRepo.GetByUnitRscAsync(unitRsc) → else UnitScheduleNotFound
   ├─ Capture EventRsc from schedule (for event payload)
   ├─ scheduleRepo.DeleteAsync(unitRsc)                 // hard delete
   └─ publisher.Publish(new UnitUnscheduledEvent(unitRsc, eventRsc, UtcNow))
   ↓
204 No Content
```

**Note on delete + event**: the event is constructed and published directly by the handler (not raised by the aggregate, since the aggregate no longer exists post-delete). Acceptable because the event is flat (IDs + timestamp only).

### Flow 5: Read — units at a location on a date

```
GET /api/scheduling/locations/{locationCode}/today?date=2026-04-20
   ↓
ListUnitsByLocationEndpoint → MediatR.Send(ListUnitsByLocationQuery)
   ↓
[Validator] locationCode length==3
   ↓
ListUnitsByLocationHandler
   ├─ date = requestedDate ?? DateOnly.FromDateTime(DateTime.UtcNow)
   └─ scheduleRepo.ListByLocationAndDateAsync(locationCode, date)
        → MongoQuery filter: {
            locationCode: X,
            startTime: { $gte: dateUtc00:00, $lt: dateUtc+1day }
          }
        → sorted by startTime ascending
   ↓
200 OK [ { unitRsc, eventRsc, sessionCode, locationCode, startTime,
           orderInSession, orderInLocation, status, scheduledAt }, ... ]
```

### Transactional ordering and partial failures

Per handler: (1) validations → (2) persist → (3) publish events.

If publish fails after persist succeeds, downstream doesn't know about the change. Same known MVP limitation as in MVP 1 (CompetitionConfig). Documented as deferred.

### Downstream consumers (out of MVP 2 scope, illustrative)

```
UnitScheduledEvent         → DataEntry (MVP 3): create UnitResult, status=START_LIST
                             DataDistribution (MVP 5): emit DT_SCHEDULE_UPDATE

UnitScheduleChangedEvent   → DataEntry: update existing UnitResult startTime
                             DataDistribution: emit DT_SCHEDULE_UPDATE with ModificationIndicator=U

UnitUnscheduledEvent       → DataEntry: remove UnitResult if exists
                             DataDistribution: emit DT_SCHEDULE_UPDATE with UNSCHEDULED status
```

## Section 4 — Error handling and validation

### 3 validation levels

**Level 1 — Input (FluentValidation):**

`CreateSessionValidator`:
- `Code`: not empty, length 1..10, regex `^[A-Z0-9]+$`
- `VenueCode`: not empty, length == 3, regex `^[A-Z0-9]{3}$`
- `Name`: not empty, length 1..40
- `StartDate`, `EndDate`: required; `EndDate > StartDate`
- `Leadin`: if present, `>= TimeSpan.Zero`

`ScheduleUnitValidator`:
- `SessionCode` (route): not empty
- `UnitRsc`: not empty, length == 34
- `LocationCode`: not empty, length == 3, regex `^[A-Z0-9]{3}$`
- `StartTime`: required
- `OrderInSession`, `OrderInLocation`: `>= 1`

`RescheduleUnitValidator`: same fields as `ScheduleUnitValidator`; `Reason`: if present, length 1..200.

`UnscheduleUnitValidator`:
- `UnitRsc` (route): not empty, length == 34

`ListUnitsByLocationValidator`:
- `LocationCode` (route): not empty, length == 3, regex `^[A-Z0-9]{3}$`

Validation failures → `400 Bad Request` via `ValidationBehavior`.

**Level 2 — Application (`ErrorOr`):**

All typed errors in `Errors/SchedulingErrors.cs`:

| Error | ErrorType | HTTP | Message key |
|-------|-----------|------|-------------|
| `InvalidVenue` | Validation | 400 | `Scheduling.InvalidVenue` |
| `SessionAlreadyExists` | Conflict | 409 | `Scheduling.SessionAlreadyExists` |
| `SessionNotFound` | NotFound | 404 | `Scheduling.SessionNotFound` |
| `StartTimeOutsideSession` | Validation | 400 | `Scheduling.StartTimeOutsideSession` |
| `UnitAlreadyScheduled` | Conflict | 409 | `Scheduling.UnitAlreadyScheduled` |
| `LocationTimeOccupied` | Conflict | 409 | `Scheduling.LocationTimeOccupied` |
| `UnitScheduleNotFound` | NotFound | 404 | `Scheduling.UnitScheduleNotFound` |

**Level 3 — Domain (aggregate invariants):**

Aggregates throw `ArgumentException` (or derivatives) for internal invariant violations — these are contract bugs, not recoverable errors:

- `Session.Create`: null/whitespace checks; `endDate > startDate`
- `UnitSchedule.Create`: `unitRsc.IsAtLevel(Unit)`
- `UnitSchedule.Reschedule`: `Status == Scheduled`

Invariants requiring external queries (Session existence, collision) live at level 2.

### i18n

Create `src/OVR.Modules.Scheduling/I18n/{eng,spa,por}.json` with all 7 error keys.

Example `eng.json`:
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

Register in csproj:
```xml
<Content Include="I18n\**"
         Link="I18n.Scheduling\%(RecursiveDir)%(Filename)%(Extension)"
         CopyToOutputDirectory="PreserveNewest" />
```

### Explicit non-goals

- Concurrent reschedule on the same Unit — one wins, other returns 409 via collision detector
- Cross-module validation of UnitRsc existence in CompetitionConfig (would couple contexts)
- Timezone handling — MVP assumes all times in UTC
- Duration-based overlap detection — deferred (needs duration model in DataEntry)
- Cascade effects of Session cancel — no cancel in MVP
- Soft-delete / audit trail of unscheduled units

## Section 5 — Testing

### Unit tests

Project: `tests/OVR.Modules.Scheduling.Tests/` (create, modeled after `OVR.Modules.CompetitionConfig.Tests/`).

Stack: xUnit + FluentAssertions + NSubstitute.

**`SessionAggregateTests`**:
- `Create_WithValidInputs_SetsProperties`
- `Create_WithEndDateBeforeStartDate_Throws`
- `Create_WithEmptyCode_Throws`
- `Create_WithInvalidVenueLength_Throws`
- `Create_WithNullLeadin_AllowsIt`
- `Create_WithNegativeLeadin_Throws`

**`UnitScheduleAggregateTests`**:
- `Create_FromUnitLevelRsc_DerivesEventRsc`
- `Create_FromNonUnitLevelRsc_Throws`
- `Create_SetsStatusScheduledAndScheduledAt`
- `Create_RaisesUnitScheduledEvent_WithCorrectPayload`
- `Reschedule_WithNewTime_UpdatesFieldsAndRaisesChangedEvent`
- `Reschedule_WithReason_IncludesReasonInEvent`
- `Reschedule_WithOrderChangeOnly_StillRaisesEvent`

**`ScheduleCollisionDetectorTests`**:
- `EnsureNoCollision_NoOtherUnit_ReturnsSuccess`
- `EnsureNoCollision_DifferentLocation_ReturnsSuccess`
- `EnsureNoCollision_DifferentTime_ReturnsSuccess`
- `EnsureNoCollision_SameLocationAndTime_ReturnsLocationTimeOccupied`
- `EnsureNoCollision_WithExcludeUnitRsc_IgnoresSelf`

**`CreateSessionHandlerTests`** (mock `ICommonCodeCache`, `ISessionRepository`):
- `Handle_ValidCommand_PersistsAndReturnsResponse`
- `Handle_InvalidVenue_ReturnsInvalidVenueError`
- `Handle_DuplicateCode_ReturnsConflict`

**`ScheduleUnitHandlerTests`**:
- `Handle_ValidCommand_PersistsSchedulePublishesEvent`
- `Handle_SessionNotFound_Returns404`
- `Handle_StartTimeBeforeSession_Returns400_StartTimeOutsideSession`
- `Handle_StartTimeAfterSession_Returns400_StartTimeOutsideSession`
- `Handle_UnitAlreadyScheduled_ReturnsConflict`
- `Handle_LocationTimeOccupied_ReturnsConflict`
- `Handle_Success_PublishesUnitScheduledEventWithCorrectPayload`

**`RescheduleUnitHandlerTests`**:
- `Handle_ValidReschedule_UpdatesAndPublishesChangedEvent`
- `Handle_UnitScheduleNotFound_Returns404`
- `Handle_NewSessionNotFound_Returns404_SessionNotFound`
- `Handle_ChangingOnlyOrder_DoesNotCollideWithSelf`
- `Handle_CollisionWithDifferentUnit_ReturnsLocationTimeOccupied`

**`UnscheduleUnitHandlerTests`**:
- `Handle_ValidUnitRsc_DeletesAndPublishesUnscheduledEvent`
- `Handle_NotFound_Returns404`

**`ListUnitsByLocationHandlerTests`**:
- `Handle_WithDate_ReturnsOnlyUnitsInThatDate_Sorted`
- `Handle_WithoutDate_UsesToday`
- `Handle_NoUnits_ReturnsEmptyList`

### Integration tests

Project: extend `tests/OVR.Api.IntegrationTests/` with `Scheduling/` folder.

Fixture: `SchedulingWebAppFactory` (new, analogous to `CompetitionConfigWebAppFactory`). Seeds CC: DISCIPLINE `BOX`, VENUES `ABC`, EVENT `57KG` (to pre-create a CompetitionConfig Event with a real RSC for testing).

**`CreateSessionEndpointTests`**:
- `POST_ValidPayload_Returns201`
- `POST_UnknownVenue_Returns400`
- `POST_DuplicateCode_Returns409`
- `POST_EndBeforeStart_Returns400FromValidator`

**`ScheduleUnitEndpointTests`** (test fixture pre-creates a Session):
- `POST_ValidPayload_Returns201AndPersistsSchedule`
- `POST_MissingSession_Returns404`
- `POST_StartTimeBeforeSession_Returns400`
- `POST_AlreadyScheduled_Returns409`
- `POST_CollisionAtSameLocationTime_Returns409`
- `POST_Success_PublishesUnitScheduledEvent`

**`RescheduleUnitEndpointTests`**:
- `PATCH_ValidNewTime_Returns200`
- `PATCH_NotFound_Returns404`
- `PATCH_ChangingOnlyOrder_DoesNotSelfCollide_Returns200`
- `PATCH_CollisionWithOther_Returns409`

**`UnscheduleUnitEndpointTests`**:
- `DELETE_Existing_Returns204_AndPersistenceIsGone`
- `DELETE_NotFound_Returns404`

**`ListUnitsByLocationEndpointTests`**:
- `GET_WithScheduledUnits_ReturnsSortedByStartTime`
- `GET_NoUnitsAtLocation_ReturnsEmpty`
- `GET_FiltersToRequestedDate_IgnoresOtherDays`

### Coverage targets for "MVP done"

- 100% branches of `ScheduleCollisionDetector`
- Happy path + each error path (4xx) in every handler
- At least one test per integration event (Scheduled, Changed, Unscheduled)
- End-to-end: "schedule 2 units at RGA + 1 at RGB; GET RGA/today returns 2 sorted"

### Out of scope

- Performance tests
- Timezone / DST tests (assume UTC)
- Concurrency race tests (noted limitation)

## Persistence layout

Two MongoDB collections.

### `scheduling_sessions`

```json
{
  "_id": "BOX01",
  "venueCode": "ABC",
  "name": "Boxing Session 1",
  "startDate": "2026-04-20T10:00:00Z",
  "endDate": "2026-04-20T14:00:00Z",
  "leadin": "PT5M",
  "createdAt": "2026-04-17T..."
}
```

### `scheduling_unit_schedules`

```json
{
  "_id": "BOXM57KG--------------8FNL0001----",
  "eventRsc": "BOXM57KG--------------------------",
  "sessionCode": "BOX01",
  "locationCode": "RGA",
  "startTime": "2026-04-20T10:15:00Z",
  "orderInSession": 1,
  "orderInLocation": 1,
  "status": "Scheduled",
  "scheduledAt": "2026-04-17T...",
  "updatedAt": null
}
```

**Recommended indexes** (created during MVP or documented as immediate follow-up):
- `scheduling_unit_schedules`: `{ locationCode: 1, startTime: 1 }` — supports both `FindByLocationAndTimeAsync` and `ListByLocationAndDateAsync`
- `scheduling_unit_schedules`: `{ sessionCode: 1 }` — supports future "list units in session" queries

`_id` indexes are automatic (BsonId on the RSC / SessionCode strings).

## Deferred items (post-MVP)

- Multi-document transaction around `ScheduleUnit` / `RescheduleUnit` flows (same posture as MVP 1)
- Location CC validation (`WellKnownCodeTypes.Location` added, not consulted)
- Session lifecycle: cancel (with cascade), metadata update
- Duration model for overlap detection
- Auto-calculated StartTime from `gapMinutes` (needs duration model)
- Soft-delete / audit trail for unscheduled units
- Cross-module sanity check (does the UnitRsc actually exist in CompetitionConfig?) — optional `IUnitReader` contract later
- Timezone handling (discipline-specific venues)
- `ScheduleStatus` richer state machine (`Cancelled`, `Postponed`, etc.)
- Read models for "units pending scheduling in event X" (via projection or cross-module composition)

## Out of scope (explicit)

- UI (Blazor WASM comes after API stabilizes)
- Authentication / authorization
- Schedule version history
- DT_SCHEDULE XML emission (that's DataDistribution, MVP 5)
- Cross-venue scheduling logic
- Broadcast-session linkage
