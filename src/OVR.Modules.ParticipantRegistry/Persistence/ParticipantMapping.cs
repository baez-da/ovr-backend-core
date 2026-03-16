using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Persistence;

internal static class ParticipantMapping
{
    public static ParticipantDocument ToDocument(Participant participant) => new()
    {
        Id = participant.Id,
        Type = participant.Type.ToString(),
        GivenName = participant.Description.GivenName,
        FamilyName = participant.Description.FamilyName,
        GenderCode = participant.Description.Gender.Value,
        BirthDate = participant.Description.BirthDate,
        Noc = participant.Description.Organisation.Code,
        PrintName = participant.PrintName,
        TvName = participant.TvName,
        ExtendedDescription = new Dictionary<string, string>(participant.ExtendedDescription.Properties),
        CreatedAt = participant.CreatedAt,
        UpdatedAt = participant.UpdatedAt
    };

    public static Participant ToDomain(ParticipantDocument doc)
    {
        var participantId = ParticipantId.Create(doc.Id);
        var type = Enum.Parse<ParticipantType>(doc.Type, ignoreCase: true);
        var gender = Gender.FromCode(doc.GenderCode);
        var noc = Noc.Create(doc.Noc);
        var description = Description.Create(doc.GivenName, doc.FamilyName, gender, doc.BirthDate, noc);

        return Participant.Create(participantId, type, description);
    }
}
