using ErrorOr;
using OVR.Modules.CompetitionConfig.Errors;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed class Event : AggregateRoot<string>
{
    public Rsc Rsc { get; private set; } = null!;
    public string Discipline { get; private set; } = string.Empty;
    public Gender Gender { get; private set; } = null!;
    public string EventCode { get; private set; } = string.Empty;
    public string? Modifier { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public CompetitionFormat? Format { get; private set; }
    public int? Size { get; private set; }
    public IReadOnlyList<Phase> Phases => _phases.AsReadOnly();
    public DateTime CreatedAt { get; private set; }
    public DateTime? StructureGeneratedAt { get; private set; }

    private readonly List<Phase> _phases = new();

    private Event() { }

    public static Event Create(
        Rsc rsc,
        string discipline,
        Gender gender,
        string eventCode,
        string? modifier,
        string name)
    {
        ArgumentNullException.ThrowIfNull(rsc);
        ArgumentException.ThrowIfNullOrWhiteSpace(discipline);
        ArgumentNullException.ThrowIfNull(gender);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Cross-field consistency: the RSC must be derivable from the denormalized fields.
        // Protects against callers (integration handlers, imports, reconstitution paths)
        // passing mismatched values.
        if (rsc.Discipline != discipline)
            throw new ArgumentException(
                $"RSC discipline segment '{rsc.Discipline}' does not match discipline '{discipline}'.",
                nameof(discipline));

        if (rsc.Gender.ToString() != gender.Value)
            throw new ArgumentException(
                $"RSC gender segment '{rsc.Gender}' does not match gender '{gender.Value}'.",
                nameof(gender));

        // Event segment in RSC is right-padded with '-'; compare by trimming.
        var rscEventSegment = rsc.Event;
        var expectedEventPrefix = eventCode.PadRight(8, '-');
        if (!rscEventSegment.StartsWith(expectedEventPrefix))
            throw new ArgumentException(
                $"RSC event segment '{rscEventSegment}' does not start with event code '{eventCode}'.",
                nameof(eventCode));

        return new Event
        {
            Id = rsc.Value,
            Rsc = rsc,
            Discipline = discipline,
            Gender = gender,
            EventCode = eventCode,
            Modifier = modifier,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }

    public ErrorOr<IReadOnlyList<(Rsc Rsc, int? SeedA, int? SeedB)>> GenerateStructure(
        CompetitionFormat format,
        int size,
        int startUnitNumber,
        BracketGenerator generator)
    {
        if (Format.HasValue)
            return CompetitionConfigErrors.StructureAlreadyGenerated(Id);

        if (format != CompetitionFormat.SingleElimination)
            return CompetitionConfigErrors.UnsupportedFormat(format.ToString());

        if (size < 2 || size > 128)
            return CompetitionConfigErrors.InvalidSize(size);

        var plan = generator.Generate(format, size, startUnitNumber);

        _phases.AddRange(plan.Phases.Select(s => Phase.CreateInternal(s.Code, s.Order, s.UnitCount)));
        Format = format;
        Size = size;
        StructureGeneratedAt = DateTime.UtcNow;

        var eventPrefix = Rsc.Value[..22];
        var unitEntries = plan.UnitLocalSegments
            .Select((seg, idx) => (
                Rsc: Rsc.Create(eventPrefix + seg),
                SeedA: plan.UnitSeedPairings[idx].SeedA,
                SeedB: plan.UnitSeedPairings[idx].SeedB))
            .ToList();

        RaiseDomainEvent(new EventStructureGeneratedEvent(
            EventRsc: Id,
            Format: format.ToString(),
            Size: size,
            Phases: plan.Phases.Select(p => new PhaseInfo(p.Code, p.Order, p.UnitCount)).ToList(),
            UnitRscs: unitEntries.Select(e => e.Rsc.Value).ToList(),
            GeneratedAt: StructureGeneratedAt.Value,
            Edges: plan.Edges.ToList()));

        return unitEntries;
    }

    internal void HydrateFromStorage(
        CompetitionFormat format,
        int size,
        IReadOnlyList<(string Code, int Order, int UnitCount)> phases,
        DateTime structureGeneratedAt)
    {
        Format = format;
        Size = size;
        _phases.Clear();
        _phases.AddRange(phases.Select(p => Phase.CreateInternal(p.Code, p.Order, p.UnitCount)));
        StructureGeneratedAt = structureGeneratedAt;
    }
}
