using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class Participant : AggregateRoot<string>
{
    public ParticipantId ParticipantId { get; private set; } = null!;
    public BiographicData BiographicData { get; private set; } = null!;
    public SupplementaryData SupplementaryData { get; private set; } = new();
    public IReadOnlyList<ParticipantFunction> Functions { get; private set; } = [];
    public string PrintName { get; private set; } = string.Empty;
    public string PrintInitialName { get; private set; } = string.Empty;
    public string TvName { get; private set; } = string.Empty;
    public string TvInitialName { get; private set; } = string.Empty;
    public string TvFamilyName { get; private set; } = string.Empty;
    public string PscbName { get; private set; } = string.Empty;
    public string PscbShortName { get; private set; } = string.Empty;
    public string PscbLongName { get; private set; } = string.Empty;
    public string? PhotoUrl { get; private set; }
    public string? Code { get; private set; }
    public string? Nationality { get; private set; }
    public string? Status { get; private set; }
    public bool Current { get; private set; } = true;
    public string? PassportGivenName { get; private set; }
    public string? PassportFamilyName { get; private set; }
    public int? Height { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Participant() { }

    public static Participant Create(
        BiographicData biographicData,
        SupplementaryData? extendedDescription,
        IReadOnlyList<ParticipantFunction> functions,
        string printName,
        string printInitialName,
        string tvName,
        string tvInitialName,
        string tvFamilyName,
        string pscbName,
        string pscbShortName,
        string pscbLongName,
        string? photoUrl = null)
    {
        ValidateFunctions(functions);

        var participantId = ParticipantId.Generate();
        var participant = new Participant
        {
            Id = participantId.Value,
            ParticipantId = participantId,
            BiographicData = biographicData,
            SupplementaryData = extendedDescription ?? new SupplementaryData(),
            Functions = functions.ToList(),
            PrintName = printName,
            PrintInitialName = printInitialName,
            TvName = tvName,
            TvInitialName = tvInitialName,
            TvFamilyName = tvFamilyName,
            PscbName = pscbName,
            PscbShortName = pscbShortName,
            PscbLongName = pscbLongName,
            PhotoUrl = photoUrl,
            CreatedAt = DateTime.UtcNow
        };

        var mainFunctionIds = functions.Where(f => f.IsMain).Select(f => f.Function).ToList();
        participant.RaiseDomainEvent(new ParticipantCreatedEvent(
            participantId.Value,
            mainFunctionIds,
            biographicData.Organisation.Code));

        return participant;
    }

    public static Participant CreateFromOdf(
        string code,
        BiographicData biographicData,
        IReadOnlyList<ParticipantFunction> functions,
        string printName, string printInitialName,
        string tvName, string tvInitialName, string tvFamilyName,
        string pscbName, string pscbShortName, string pscbLongName,
        string? nationality, string? status, bool current,
        string? passportGivenName, string? passportFamilyName,
        int? height, SupplementaryData? supplementaryData)
    {
        ValidateFunctions(functions);

        var participantId = ParticipantId.Generate();
        return new Participant
        {
            Id = participantId.Value,
            ParticipantId = participantId,
            BiographicData = biographicData,
            SupplementaryData = supplementaryData ?? new SupplementaryData(),
            Functions = functions.ToList(),
            PrintName = printName,
            PrintInitialName = printInitialName,
            TvName = tvName,
            TvInitialName = tvInitialName,
            TvFamilyName = tvFamilyName,
            PscbName = pscbName,
            PscbShortName = pscbShortName,
            PscbLongName = pscbLongName,
            Code = code,
            Nationality = nationality,
            Status = status,
            Current = current,
            PassportGivenName = passportGivenName,
            PassportFamilyName = passportFamilyName,
            Height = height,
            CreatedAt = DateTime.UtcNow
        };
        // No domain events — bulk import uses DtParticImportedEvent
    }

    public static Participant Hydrate(
        ParticipantId participantId,
        BiographicData biographicData,
        SupplementaryData? extendedDescription,
        IReadOnlyList<ParticipantFunction> functions,
        string printName,
        string printInitialName,
        string tvName,
        string tvInitialName,
        string tvFamilyName,
        string pscbName,
        string pscbShortName,
        string pscbLongName,
        DateTime createdAt,
        DateTime? updatedAt,
        string? photoUrl = null,
        string? code = null,
        string? nationality = null,
        string? status = null,
        bool current = true,
        string? passportGivenName = null,
        string? passportFamilyName = null,
        int? height = null)
    {
        return new Participant
        {
            Id = participantId.Value,
            ParticipantId = participantId,
            BiographicData = biographicData,
            SupplementaryData = extendedDescription ?? new SupplementaryData(),
            Functions = functions.ToList(),
            PrintName = printName,
            PrintInitialName = printInitialName,
            TvName = tvName,
            TvInitialName = tvInitialName,
            TvFamilyName = tvFamilyName,
            PscbName = pscbName,
            PscbShortName = pscbShortName,
            PscbLongName = pscbLongName,
            PhotoUrl = photoUrl,
            Code = code,
            Nationality = nationality,
            Status = status,
            Current = current,
            PassportGivenName = passportGivenName,
            PassportFamilyName = passportFamilyName,
            Height = height,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private static void ValidateFunctions(IReadOnlyList<ParticipantFunction> functions)
    {
        if (functions.Count == 0)
            throw new ArgumentException("At least one function is required.");

        var disciplines = functions.Select(f => f.Discipline).Distinct();
        foreach (var discipline in disciplines)
        {
            var mainCount = functions.Count(f => f.Discipline == discipline && f.IsMain);
            if (mainCount != 1)
                throw new ArgumentException(
                    $"Exactly one main function required per discipline. Discipline '{discipline}' has {mainCount}.");
        }

        var hasDuplicates = functions.GroupBy(f => (f.Function, f.Discipline)).Any(g => g.Count() > 1);
        if (hasDuplicates)
            throw new ArgumentException("Duplicate function+discipline combination found.");
    }
}
