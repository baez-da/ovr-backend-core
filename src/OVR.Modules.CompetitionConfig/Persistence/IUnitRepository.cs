using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Persistence;

public interface IUnitRepository
{
    Task<Unit?> GetByRscAsync(string unitRsc, CancellationToken ct = default);
    Task<IReadOnlyList<Unit>> ListByEventAsync(string eventRsc, CancellationToken ct = default);
    Task AddManyAsync(IEnumerable<Unit> units, CancellationToken ct = default);
}
