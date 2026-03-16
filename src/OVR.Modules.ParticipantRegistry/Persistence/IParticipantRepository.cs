using OVR.Modules.ParticipantRegistry.Domain;

namespace OVR.Modules.ParticipantRegistry.Persistence;

public interface IParticipantRepository
{
    Task<Participant?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Participant>> FindByNocAsync(string noc, CancellationToken ct = default);
    Task AddAsync(Participant participant, CancellationToken ct = default);
    Task UpdateAsync(Participant participant, CancellationToken ct = default);
}
