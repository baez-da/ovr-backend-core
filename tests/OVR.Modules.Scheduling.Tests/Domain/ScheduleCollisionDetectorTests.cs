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
