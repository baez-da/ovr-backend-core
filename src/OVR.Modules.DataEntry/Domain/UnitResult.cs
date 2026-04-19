using ErrorOr;
using OVR.Modules.DataEntry.Errors;
using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Domain;

public sealed class UnitResult : AggregateRoot<string>
{
    private readonly List<Competitor> _competitors = new();
    private readonly List<Period> _periods = new();

    public Rsc UnitRsc { get; private set; } = null!;
    public ResultStatus Status { get; private set; }
    public IReadOnlyList<Competitor> Competitors => _competitors;
    public IReadOnlyList<Period> Periods => _periods;
    public Decision? Decision { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public string? CurrentPeriodCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UnitResult() { }

    public static ErrorOr<UnitResult> CreateForFirstRound(
        Rsc unitRsc, Competitor red, Competitor blue)
    {
        if (unitRsc is null)
            return DataEntryErrors.InvalidCompetitors("UnitRsc is required.");

        if (red.SortOrder != 1 || blue.SortOrder != 2)
            return DataEntryErrors.InvalidCompetitors(
                "Competitors must have SortOrder 1 (red) and 2 (blue).");

        if (red.ParticipantId is null || blue.ParticipantId is null)
            return DataEntryErrors.InvalidCompetitors(
                "MVP 3 requires real ParticipantIds (NOCOMP not supported).");

        if (red.ParticipantId == blue.ParticipantId)
            return DataEntryErrors.InvalidCompetitors(
                "Competitors must be distinct participants.");

        var now = DateTime.UtcNow;
        var ur = new UnitResult
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            Status = ResultStatus.StartList,
            CreatedAt = now
        };
        ur._competitors.Add(red);
        ur._competitors.Add(blue);

        ur.RaiseDomainEvent(new UnitResultStartListCreatedEvent(
            UnitRsc: unitRsc.Value,
            EventRsc: Rsc.Create(unitRsc.AtEventLevel()).Value,
            Competitors: new[]
            {
                new CompetitorSnapshot(red.SortOrder,
                    red.ParticipantId?.Value, red.Seed, red.Organisation.Code),
                new CompetitorSnapshot(blue.SortOrder,
                    blue.ParticipantId?.Value, blue.Seed, blue.Organisation.Code)
            },
            CreatedAt: now));

        return ur;
    }
}
