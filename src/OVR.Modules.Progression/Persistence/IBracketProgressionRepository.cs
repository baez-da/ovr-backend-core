using OVR.Modules.Progression.Domain;

namespace OVR.Modules.Progression.Persistence;

public interface IBracketProgressionRepository
{
    Task<BracketProgression?> GetByEventAsync(string eventRsc, CancellationToken ct);
    Task<bool> SaveNewAsync(BracketProgression agg, CancellationToken ct);
    Task ReplaceAsync(BracketProgression agg, CancellationToken ct);
}
