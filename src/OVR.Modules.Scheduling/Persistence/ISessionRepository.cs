using OVR.Modules.Scheduling.Domain;

namespace OVR.Modules.Scheduling.Persistence;

public interface ISessionRepository
{
    Task<Session?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(Session session, CancellationToken ct = default);
}
