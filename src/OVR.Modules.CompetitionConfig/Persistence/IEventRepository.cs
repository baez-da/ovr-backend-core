using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Persistence;

public interface IEventRepository
{
    Task<Event?> GetByRscAsync(string eventRsc, CancellationToken ct = default);
    Task AddAsync(Event @event, CancellationToken ct = default);
    Task UpdateAsync(Event @event, CancellationToken ct = default);
}
