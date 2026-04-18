using ErrorOr;
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
            .Returns(Result.Success);
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
            .Returns(Result.Success);

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
