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
