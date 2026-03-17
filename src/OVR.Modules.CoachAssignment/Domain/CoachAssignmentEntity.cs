using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CoachAssignment.Domain;

public sealed class CoachAssignmentEntity : AggregateRoot<string>
{
    public ParticipantId ParticipantId { get; private set; } = null!;
    public Rsc EventRsc { get; private set; } = null!;
    public string Function { get; private set; } = string.Empty;
    public string? AccreditationFunction { get; private set; }
    public Organisation Organisation { get; private set; } = null!;
    public CoachAssignmentStatus Status { get; private set; }
    public ExternalSystemId? ExternalId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private CoachAssignmentEntity() { }

    public static CoachAssignmentEntity Create(
        ParticipantId participantId,
        Rsc eventRsc,
        string function,
        Organisation organisation,
        string? accreditationFunction = null)
    {
        var assignment = new CoachAssignmentEntity
        {
            Id = $"{participantId.Value}_{eventRsc.Value}",
            ParticipantId = participantId,
            EventRsc = eventRsc,
            Function = function,
            AccreditationFunction = accreditationFunction,
            Organisation = organisation,
            Status = CoachAssignmentStatus.Assigned,
            CreatedAt = DateTime.UtcNow
        };

        assignment.RaiseDomainEvent(new CoachAssignedEvent(
            participantId.Value, eventRsc.Value, function));

        return assignment;
    }

    public static CoachAssignmentEntity Hydrate(
        string id, ParticipantId participantId, Rsc eventRsc,
        string function, string? accreditationFunction,
        Organisation organisation, CoachAssignmentStatus status,
        ExternalSystemId? externalId,
        DateTime createdAt, DateTime? updatedAt)
    {
        return new CoachAssignmentEntity
        {
            Id = id, ParticipantId = participantId, EventRsc = eventRsc,
            Function = function, AccreditationFunction = accreditationFunction,
            Organisation = organisation, Status = status,
            ExternalId = externalId,
            CreatedAt = createdAt, UpdatedAt = updatedAt
        };
    }

    public void ChangeStatus(CoachAssignmentStatus newStatus)
    {
        var valid = (Status, newStatus) switch
        {
            (CoachAssignmentStatus.Assigned, CoachAssignmentStatus.Confirmed) => true,
            (CoachAssignmentStatus.Assigned, CoachAssignmentStatus.Withdrawn) => true,
            (CoachAssignmentStatus.Confirmed, CoachAssignmentStatus.Withdrawn) => true,
            (CoachAssignmentStatus.Confirmed, CoachAssignmentStatus.Replaced) => true,
            _ => false
        };
        if (!valid)
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}.");
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
