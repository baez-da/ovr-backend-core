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

public sealed class ScheduleCollisionDetector(IUnitScheduleRepository repo)
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
