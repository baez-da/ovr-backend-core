using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Domain;

public sealed class Participant : AggregateRoot<string>
{
    public ParticipantId ParticipantId { get; private set; } = null!;
    public Description Description { get; private set; } = null!;
    public ExtendedDescription ExtendedDescription { get; private set; } = new();
    public IReadOnlyList<ParticipantFunction> Functions { get; private set; } = [];
    public string PrintName { get; private set; } = string.Empty;
    public string PrintInitialName { get; private set; } = string.Empty;
    public string TvName { get; private set; } = string.Empty;
    public string TvInitialName { get; private set; } = string.Empty;
    public string TvFamilyName { get; private set; } = string.Empty;
    public string PscbName { get; private set; } = string.Empty;
    public string PscbShortName { get; private set; } = string.Empty;
    public string PscbLongName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Participant() { }

    public static Participant Create(
        Description description,
        ExtendedDescription? extendedDescription,
        IReadOnlyList<ParticipantFunction> functions,
        string printName,
        string printInitialName,
        string tvName,
        string tvInitialName,
        string tvFamilyName,
        string pscbName,
        string pscbShortName,
        string pscbLongName)
    {
        ValidateFunctions(functions);

        var participantId = ParticipantId.Generate();
        var participant = new Participant
        {
            Id = participantId.Value,
            ParticipantId = participantId,
            Description = description,
            ExtendedDescription = extendedDescription ?? new ExtendedDescription(),
            Functions = functions.ToList(),
            PrintName = printName,
            PrintInitialName = printInitialName,
            TvName = tvName,
            TvInitialName = tvInitialName,
            TvFamilyName = tvFamilyName,
            PscbName = pscbName,
            PscbShortName = pscbShortName,
            PscbLongName = pscbLongName,
            CreatedAt = DateTime.UtcNow
        };

        var mainFunctionIds = functions.Where(f => f.IsMain).Select(f => f.FunctionId).ToList();
        participant.RaiseDomainEvent(new ParticipantCreatedEvent(
            participantId.Value,
            mainFunctionIds,
            description.Organisation.Code));

        return participant;
    }

    public static Participant Hydrate(
        ParticipantId participantId,
        Description description,
        ExtendedDescription? extendedDescription,
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
        DateTime? updatedAt)
    {
        return new Participant
        {
            Id = participantId.Value,
            ParticipantId = participantId,
            Description = description,
            ExtendedDescription = extendedDescription ?? new ExtendedDescription(),
            Functions = functions.ToList(),
            PrintName = printName,
            PrintInitialName = printInitialName,
            TvName = tvName,
            TvInitialName = tvInitialName,
            TvFamilyName = tvFamilyName,
            PscbName = pscbName,
            PscbShortName = pscbShortName,
            PscbLongName = pscbLongName,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private static void ValidateFunctions(IReadOnlyList<ParticipantFunction> functions)
    {
        if (functions.Count == 0)
            throw new ArgumentException("At least one function is required.");

        var disciplines = functions.Select(f => f.DisciplineCode).Distinct();
        foreach (var discipline in disciplines)
        {
            var mainCount = functions.Count(f => f.DisciplineCode == discipline && f.IsMain);
            if (mainCount != 1)
                throw new ArgumentException(
                    $"Exactly one main function required per discipline. Discipline '{discipline}' has {mainCount}.");
        }

        var hasDuplicates = functions.GroupBy(f => (f.FunctionId, f.DisciplineCode)).Any(g => g.Count() > 1);
        if (hasDuplicates)
            throw new ArgumentException("Duplicate function+discipline combination found.");
    }
}
