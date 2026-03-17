using OVR.Modules.Entries.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Entries.Persistence;

internal static class EntryMapping
{
    public static EntryDocument ToDocument(Entry entry) => new()
    {
        Id = entry.Id,
        ParticipantId = entry.ParticipantId.Value,
        EventRsc = entry.EventRsc.Value,
        CompetitorType = entry.CompetitorType.ToString(),
        Organisation = entry.Organisation.Code,
        Status = entry.Status.ToString(),
        InscriptionStatus = entry.InscriptionStatus.ToString(),
        RegisteredEventRsc = entry.RegisteredEventRsc?.Value,
        Category = entry.Category,
        TeamId = entry.TeamId?.Value,
        Seed = entry.Seed,
        ExternalSystem = entry.ExternalId?.System,
        ExternalIdValue = entry.ExternalId?.Value,
        CreatedAt = entry.CreatedAt,
        UpdatedAt = entry.UpdatedAt
    };

    public static Entry ToDomain(EntryDocument doc)
    {
        var participantId = ParticipantId.Create(doc.ParticipantId);
        var eventRsc = Rsc.Create(doc.EventRsc);
        var competitorType = Enum.Parse<CompetitorType>(doc.CompetitorType, ignoreCase: true);
        var organisation = Organisation.Create(doc.Organisation);
        var status = Enum.Parse<EntryStatus>(doc.Status, ignoreCase: true);
        var inscriptionStatus = Enum.Parse<InscriptionStatus>(doc.InscriptionStatus, ignoreCase: true);
        var registeredEventRsc = doc.RegisteredEventRsc is not null ? Rsc.Create(doc.RegisteredEventRsc) : null;
        var teamId = doc.TeamId is not null ? TeamId.Create(doc.TeamId) : null;
        var externalId = doc.ExternalSystem is not null && doc.ExternalIdValue is not null
            ? ExternalSystemId.Create(doc.ExternalSystem, doc.ExternalIdValue)
            : null;

        return Entry.Hydrate(
            doc.Id, participantId, eventRsc, competitorType, organisation,
            status, inscriptionStatus, registeredEventRsc, doc.Category,
            teamId, doc.Seed, externalId, doc.CreatedAt, doc.UpdatedAt);
    }
}
