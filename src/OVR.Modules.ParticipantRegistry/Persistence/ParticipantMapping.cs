using OVR.Modules.ParticipantRegistry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Persistence;

internal static class ParticipantMapping
{
    public static ParticipantDocument ToDocument(Participant participant) => new()
    {
        Id = participant.Id,
        Functions = participant.Functions.Select(f => new ParticipantFunctionDocument
        {
            Function = f.Function,
            Discipline = f.Discipline,
            IsMain = f.IsMain
        }).ToList(),
        GivenName = participant.BiographicData.GivenName,
        FamilyName = participant.BiographicData.FamilyName,
        Gender = participant.BiographicData.Gender.Value,
        BirthDate = participant.BiographicData.BirthDate,
        Organisation = participant.BiographicData.Organisation.Code,
        PrintName = participant.PrintName,
        PrintInitialName = participant.PrintInitialName,
        TvName = participant.TvName,
        TvInitialName = participant.TvInitialName,
        TvFamilyName = participant.TvFamilyName,
        PscbName = participant.PscbName,
        PscbShortName = participant.PscbShortName,
        PscbLongName = participant.PscbLongName,
        PhotoUrl = participant.PhotoUrl,
        ExtendedDescription = new Dictionary<string, string>(participant.ExtendedDescription.Properties),
        CreatedAt = participant.CreatedAt,
        UpdatedAt = participant.UpdatedAt
    };

    public static Participant ToDomain(ParticipantDocument doc)
    {
        var participantId = ParticipantId.Create(doc.Id);
        var gender = Gender.FromCode(doc.Gender);
        var organisation = Organisation.Create(doc.Organisation);
        var biographicData = BiographicData.Create(doc.GivenName, doc.FamilyName, gender, doc.BirthDate, organisation);

        var functions = doc.Functions
            .Select(f => ParticipantFunction.Create(f.Function, f.Discipline, f.IsMain))
            .ToList();

        var extendedDescription = new ExtendedDescription();
        foreach (var kvp in doc.ExtendedDescription)
            extendedDescription.Set(kvp.Key, kvp.Value);

        return Participant.Hydrate(
            participantId, biographicData, extendedDescription, functions,
            doc.PrintName, doc.PrintInitialName, doc.TvName, doc.TvInitialName,
            doc.TvFamilyName, doc.PscbName, doc.PscbShortName, doc.PscbLongName,
            doc.CreatedAt, doc.UpdatedAt, doc.PhotoUrl);
    }
}
