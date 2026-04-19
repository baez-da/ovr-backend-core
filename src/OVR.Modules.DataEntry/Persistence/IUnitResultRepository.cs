using OVR.Modules.DataEntry.Domain;

namespace OVR.Modules.DataEntry.Persistence;

public interface IUnitResultRepository
{
    Task<bool> ExistsAsync(string unitRsc, CancellationToken ct);
    Task<UnitResult?> GetAsync(string unitRsc, CancellationToken ct);
    Task<IReadOnlyList<UnitResult>> GetManyAsync(
        IReadOnlyList<string> unitRscs, CancellationToken ct);
    Task<IReadOnlyList<UnitResult>> ListAllAsync(CancellationToken ct);
    Task SaveNewAsync(UnitResult unitResult, CancellationToken ct);
    Task UpdateAsync(UnitResult unitResult, CancellationToken ct);
}
