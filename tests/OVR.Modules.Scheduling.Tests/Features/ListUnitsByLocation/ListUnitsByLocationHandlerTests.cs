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
