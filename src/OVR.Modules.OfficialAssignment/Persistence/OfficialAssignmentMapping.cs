using OVR.Modules.OfficialAssignment.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.OfficialAssignment.Persistence;

internal static class OfficialAssignmentMapping
{
    public static OfficialAssignmentDocument ToDocument(OfficialAssignmentEntity entity) => new()
    {
        Id = entity.Id,
        ParticipantId = entity.ParticipantId.Value,
        UnitRsc = entity.UnitRsc.Value,
        Function = entity.Function,
        AccreditationFunction = entity.AccreditationFunction,
        Organisation = entity.Organisation.Code,
        Status = entity.Status.ToString(),
        TeamId = entity.TeamId?.Value,
        ExternalSystem = entity.ExternalId?.System,
        ExternalIdValue = entity.ExternalId?.Value,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    public static OfficialAssignmentEntity ToDomain(OfficialAssignmentDocument doc)
    {
        var participantId = ParticipantId.Create(doc.ParticipantId);
        var unitRsc = Rsc.Create(doc.UnitRsc);
        var organisation = Organisation.Create(doc.Organisation);
        var status = Enum.Parse<AssignmentStatus>(doc.Status, ignoreCase: true);
        var teamId = doc.TeamId is not null ? TeamId.Create(doc.TeamId) : null;
        var externalId = doc.ExternalSystem is not null && doc.ExternalIdValue is not null
            ? ExternalSystemId.Create(doc.ExternalSystem, doc.ExternalIdValue)
            : null;

        return OfficialAssignmentEntity.Hydrate(
            doc.Id, participantId, unitRsc,
            doc.Function, doc.AccreditationFunction,
            organisation, status,
            teamId, externalId, doc.CreatedAt, doc.UpdatedAt);
    }
}
