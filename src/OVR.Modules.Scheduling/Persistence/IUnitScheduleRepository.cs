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
