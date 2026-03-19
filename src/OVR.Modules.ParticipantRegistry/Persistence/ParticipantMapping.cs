using OVR.Modules.ParticipantRegistry.Domain;
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
        Organisation = participant.Description.Organisation.Code,
        PrintName = participant.PrintName,
        PrintInitialName = participant.PrintInitialName,
        TvName = participant.TvName,
        TvInitialName = participant.TvInitialName,
        TvFamilyName = participant.TvFamilyName,
        PscbName = participant.PscbName,
        PscbShortName = participant.PscbShortName,
        PscbLongName = participant.PscbLongName,
        ExtendedDescription = new Dictionary<string, string>(participant.ExtendedDescription.Properties),
        CreatedAt = participant.CreatedAt,
        UpdatedAt = participant.UpdatedAt
    };

    public static Participant ToDomain(ParticipantDocument doc)
    {
        var participantId = ParticipantId.Create(doc.Id);
        var type = Enum.Parse<ParticipantType>(doc.Type, ignoreCase: true);
        var gender = Gender.FromCode(doc.GenderCode);
        var organisation = Organisation.Create(doc.Organisation);
        var description = Description.Create(doc.GivenName, doc.FamilyName, gender, doc.BirthDate, organisation);

        var extendedDescription = new ExtendedDescription();
        foreach (var kvp in doc.ExtendedDescription)
            extendedDescription.Set(kvp.Key, kvp.Value);

        return Participant.Hydrate(
            participantId, type, description, extendedDescription,
            doc.PrintName, doc.PrintInitialName, doc.TvName, doc.TvInitialName,
            doc.TvFamilyName, doc.PscbName, doc.PscbShortName, doc.PscbLongName,
            doc.CreatedAt, doc.UpdatedAt);
    }
}
