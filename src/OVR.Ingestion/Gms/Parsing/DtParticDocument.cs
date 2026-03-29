namespace OVR.Ingestion.Gms.Parsing;

public sealed record DtParticDocument(
    string CompetitionCode,
    string Discipline,
    int Version,
    string FeedFlag,
    IReadOnlyList<ParsedParticipant> Participants);

public sealed record ParsedParticipant(
    string Code,
    string? GivenName,
    string FamilyName,
    string Gender,
    string Organisation,
    string? BirthDate,
    string MainFunctionId,
    string Status,
    bool Current,
    string? Nationality,
    int? Height,
    string? PassportGivenName,
    string? PassportFamilyName,
    string PrintName,
    string PrintInitialName,
    string TVName,
    string TVInitialName,
    string TVFamilyName,
    string? PscbName,
    string? PscbShortName,
    string? PscbLongName,
    string? PlaceofBirth,
    string? CountryofBirth,
    string? PlaceofResidence,
    string? CountryofResidence,
    string? LocalGivenName,
    string? LocalFamilyName,
    string? OlympicSolidarity,
    string Parent,
    ParsedDiscipline DisciplineInfo);

public sealed record ParsedDiscipline(
    string Code,
    string? IFId,
    IReadOnlyList<ParsedKeyValue> DisciplineEntries,
    IReadOnlyList<ParsedRegisteredEvent> RegisteredEvents);

public sealed record ParsedRegisteredEvent(
    string Event,
    string? Bib,
    IReadOnlyList<ParsedKeyValue> EventEntries);

public sealed record ParsedKeyValue(string Code, string Value);
