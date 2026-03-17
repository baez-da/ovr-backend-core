using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.OfficialAssignment.Domain;

public sealed class OfficialAssignmentEntity : AggregateRoot<string>
{
    public ParticipantId ParticipantId { get; private set; } = null!;
    public Rsc UnitRsc { get; private set; } = null!;
    public string Function { get; private set; } = string.Empty;
    public string? AccreditationFunction { get; private set; }
    public Organisation Organisation { get; private set; } = null!;
    public AssignmentStatus Status { get; private set; }
    public TeamId? TeamId { get; private set; }
    public ExternalSystemId? ExternalId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private OfficialAssignmentEntity() { }

    public static OfficialAssignmentEntity Create(
        ParticipantId participantId,
        Rsc unitRsc,
        string function,
        Organisation organisation,
        string? accreditationFunction = null,
        TeamId? teamId = null)
    {
        var assignment = new OfficialAssignmentEntity
        {
            Id = $"{participantId.Value}_{unitRsc.Value}",
            ParticipantId = participantId,
            UnitRsc = unitRsc,
            Function = function,
            AccreditationFunction = accreditationFunction,
            Organisation = organisation,
            Status = AssignmentStatus.Assigned,
            TeamId = teamId,
            CreatedAt = DateTime.UtcNow
        };

        assignment.RaiseDomainEvent(new OfficialAssignedEvent(
            participantId.Value, unitRsc.Value, function));

        return assignment;
    }

    public static OfficialAssignmentEntity Hydrate(
        string id, ParticipantId participantId, Rsc unitRsc,
        string function, string? accreditationFunction,
        Organisation organisation, AssignmentStatus status,
        TeamId? teamId, ExternalSystemId? externalId,
        DateTime createdAt, DateTime? updatedAt)
    {
        return new OfficialAssignmentEntity
        {
            Id = id, ParticipantId = participantId, UnitRsc = unitRsc,
            Function = function, AccreditationFunction = accreditationFunction,
            Organisation = organisation, Status = status,
            TeamId = teamId, ExternalId = externalId,
            CreatedAt = createdAt, UpdatedAt = updatedAt
        };
    }

    public void ChangeStatus(AssignmentStatus newStatus)
    {
        var valid = (Status, newStatus) switch
        {
            (AssignmentStatus.Assigned, AssignmentStatus.Confirmed) => true,
            (AssignmentStatus.Assigned, AssignmentStatus.Withdrawn) => true,
            (AssignmentStatus.Confirmed, AssignmentStatus.Withdrawn) => true,
            (AssignmentStatus.Confirmed, AssignmentStatus.Replaced) => true,
            _ => false
        };
        if (!valid)
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}.");
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
