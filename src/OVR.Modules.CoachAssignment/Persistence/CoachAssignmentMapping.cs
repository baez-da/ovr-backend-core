using OVR.Modules.CoachAssignment.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CoachAssignment.Persistence;

internal static class CoachAssignmentMapping
{
    public static CoachAssignmentDocument ToDocument(CoachAssignmentEntity entity) => new()
    {
        Id = entity.Id,
        ParticipantId = entity.ParticipantId.Value,
        EventRsc = entity.EventRsc.Value,
        Function = entity.Function,
        AccreditationFunction = entity.AccreditationFunction,
        Organisation = entity.Organisation.Code,
        Status = entity.Status.ToString(),
        ExternalSystem = entity.ExternalId?.System,
        ExternalIdValue = entity.ExternalId?.Value,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static CoachAssignmentEntity ToDomain(CoachAssignmentDocument doc)
    {
        var participantId = ParticipantId.Create(doc.ParticipantId);
        var eventRsc = Rsc.Create(doc.EventRsc);
        var organisation = Organisation.Create(doc.Organisation);
        var status = Enum.Parse<CoachAssignmentStatus>(doc.Status, ignoreCase: true);
        var externalId = doc.ExternalSystem is not null && doc.ExternalIdValue is not null
            ? ExternalSystemId.Create(doc.ExternalSystem, doc.ExternalIdValue)
            : null;

        return CoachAssignmentEntity.Hydrate(
            doc.Id, participantId, eventRsc,
            doc.Function, doc.AccreditationFunction,
            organisation, status, externalId,
            doc.CreatedAt, doc.UpdatedAt);
    }
}
