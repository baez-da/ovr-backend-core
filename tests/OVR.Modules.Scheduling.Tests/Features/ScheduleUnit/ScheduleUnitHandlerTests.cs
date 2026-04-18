using ErrorOr;
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
            .Returns(Result.Success);
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
