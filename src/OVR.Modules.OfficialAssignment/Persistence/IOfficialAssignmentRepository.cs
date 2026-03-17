using OVR.Modules.OfficialAssignment.Domain;

namespace OVR.Modules.OfficialAssignment.Persistence;

public interface IOfficialAssignmentRepository
{
    Task<OfficialAssignmentEntity?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<OfficialAssignmentEntity>> FindByRscPrefixAsync(string rscPrefix, CancellationToken ct = default);
    Task AddAsync(OfficialAssignmentEntity assignment, CancellationToken ct = default);
    Task UpdateAsync(OfficialAssignmentEntity assignment, CancellationToken ct = default);
}
