using ErrorOr;
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
