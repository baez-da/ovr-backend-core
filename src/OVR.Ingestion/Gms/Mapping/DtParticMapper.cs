using OVR.Ingestion.Gms.Parsing;
using OVR.Modules.Entries.Domain;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Ingestion.Gms.Mapping;

public static class DtParticMapper
{
    public static (Participant Participant, IReadOnlyList<Entry> Entries) Map(
        ParsedParticipant parsed, INameBuilder nameBuilder)
    {
        var gender = Gender.FromCode(parsed.Gender);
        var organisation = Organisation.Create(parsed.Organisation);
        var birthDate = DateOnly.TryParse(parsed.BirthDate, out var bd) ? bd : (DateOnly?)null;
        var biographic = BiographicData.Create(
            parsed.GivenName, parsed.FamilyName, gender, birthDate, organisation);

        var functions = new List<ParticipantFunction>
        {
            ParticipantFunction.Create(
                parsed.MainFunctionId,
                parsed.DisciplineInfo.Code,
                isMain: true,
                ifId: parsed.DisciplineInfo.IFId)
        };

        var pscbName = parsed.PscbName
            ?? nameBuilder.BuildPscbName(parsed.FamilyName, parsed.GivenName);
        var pscbShortName = parsed.PscbShortName
            ?? nameBuilder.BuildPscbShortName(parsed.FamilyName, parsed.GivenName);
        var pscbLongName = parsed.PscbLongName
            ?? nameBuilder.BuildPscbLongName(parsed.FamilyName, parsed.GivenName);

        var supplementaryData = BuildSupplementaryData(parsed);

        var participant = Participant.CreateFromOdf(
            code: parsed.Code,
            biographicData: biographic,
            functions: functions,
            printName: parsed.PrintName,
            printInitialName: parsed.PrintInitialName,
            tvName: parsed.TVName,
            tvInitialName: parsed.TVInitialName,
            tvFamilyName: parsed.TVFamilyName,
            pscbName: pscbName,
            pscbShortName: pscbShortName,
            pscbLongName: pscbLongName,
            nationality: parsed.Nationality,
            status: parsed.Status,
            current: parsed.Current,
            passportGivenName: parsed.PassportGivenName,
            passportFamilyName: parsed.PassportFamilyName,
            height: parsed.Height,
            supplementaryData: supplementaryData);

        var entries = MapEntries(parsed, participant.ParticipantId, organisation);

        return (participant, entries);
    }

    private static SupplementaryData BuildSupplementaryData(ParsedParticipant parsed)
    {
        var data = new SupplementaryData();

        SetIfPresent(data, "PlaceofBirth", parsed.PlaceofBirth);
        SetIfPresent(data, "CountryofBirth", parsed.CountryofBirth);
        SetIfPresent(data, "PlaceofResidence", parsed.PlaceofResidence);
        SetIfPresent(data, "CountryofResidence", parsed.CountryofResidence);
        SetIfPresent(data, "LocalGivenName", parsed.LocalGivenName);
        SetIfPresent(data, "LocalFamilyName", parsed.LocalFamilyName);
        SetIfPresent(data, "OlympicSolidarity", parsed.OlympicSolidarity);
        SetIfPresent(data, "Parent", parsed.Parent);

        foreach (var entry in parsed.DisciplineInfo.DisciplineEntries)
            data.Set(entry.Code, entry.Value);

        return data;
    }

    private static void SetIfPresent(SupplementaryData data, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            data.Set(key, value);
    }

    private static IReadOnlyList<Entry> MapEntries(
        ParsedParticipant parsed, ParticipantId participantId, Organisation organisation)
    {
        return parsed.DisciplineInfo.RegisteredEvents
            .Select(re =>
            {
                var eventEntries = re.EventEntries.Count > 0
                    ? re.EventEntries.ToDictionary(e => e.Code, e => e.Value)
                    : null;

                var seed = re.EventEntries
                    .FirstOrDefault(e => e.Code == "SEED")?.Value;

                return Entry.CreateFromOdf(
                    participantId,
                    Rsc.Create(re.Event),
                    CompetitorType.Individual,
                    organisation,
                    eventEntries,
                    seed);
            })
            .ToList();
    }
}
