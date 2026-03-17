using OVR.Modules.CoachAssignment.Domain;

namespace OVR.Modules.CoachAssignment.Persistence;

public interface ICoachAssignmentRepository
{
    Task<CoachAssignmentEntity?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<CoachAssignmentEntity>> FindByRscPrefixAsync(string rscPrefix, CancellationToken ct = default);
    Task AddAsync(CoachAssignmentEntity assignment, CancellationToken ct = default);
    Task UpdateAsync(CoachAssignmentEntity assignment, CancellationToken ct = default);
}
