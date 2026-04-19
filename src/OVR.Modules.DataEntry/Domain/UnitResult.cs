using ErrorOr;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.SportRules;
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

    public ErrorOr<Success> Start()
    {
        if (Status != ResultStatus.StartList)
            return DataEntryErrors.InvalidStatusTransition(Status.ToString(), "Live");

        Status = ResultStatus.Live;
        StartedAt = DateTime.UtcNow;
        CurrentPeriodCode = "R1";
        UpdatedAt = StartedAt;

        RaiseDomainEvent(new UnitResultLiveEvent(UnitRsc.Value, StartedAt.Value));
        return Result.Success;
    }

    public ErrorOr<Success> ScorePeriod(
        string periodCode, IReadOnlyList<PeriodScorecard> cards)
    {
        if (Status != ResultStatus.Live)
            return DataEntryErrors.InvalidStatusTransition(Status.ToString(), "score period");

        if (Decision is not null)
            return DataEntryErrors.DecisionAlreadyExists();

        if (!BoxingRules.PeriodCodes.Contains(periodCode))
            return DataEntryErrors.InvalidPeriodCode(periodCode);

        if (_periods.Any(p => p.Code == periodCode))
            return DataEntryErrors.PeriodAlreadyScored(periodCode);

        var expectedNextIndex = _periods.Count;
        if (BoxingRules.PeriodCodes[expectedNextIndex] != periodCode)
            return DataEntryErrors.InvalidPeriodOrder(periodCode);

        if (cards.Count != BoxingRules.JudgeCount)
            return DataEntryErrors.InvalidScorecardCount();

        if (cards.Select(c => c.JudgePos).Distinct().Count() != cards.Count)
            return DataEntryErrors.DuplicateJudgePosition(
                cards.GroupBy(c => c.JudgePos).First(g => g.Count() > 1).Key.ToString());

        foreach (var c in cards)
        {
            if (c.HomeScore < BoxingRules.MinPeriodScore || c.HomeScore > BoxingRules.MaxPeriodScore)
                return DataEntryErrors.InvalidScoreRange(c.HomeScore);
            if (c.AwayScore < BoxingRules.MinPeriodScore || c.AwayScore > BoxingRules.MaxPeriodScore)
                return DataEntryErrors.InvalidScoreRange(c.AwayScore);
        }

        _periods.Add(new Period(periodCode, cards.OrderBy(c => c.JudgePos).ToList()));
        UpdatedAt = DateTime.UtcNow;

        // Auto-compute Decision after R3 (last period).
        if (_periods.Count == BoxingRules.PeriodCount)
        {
            var resolver = new TenPointMustResolver();
            var red = _competitors.First(c => c.SortOrder == 1);
            var blue = _competitors.First(c => c.SortOrder == 2);
            Decision = resolver.Resolve(_periods, red.ParticipantId!, blue.ParticipantId!);
            EndedAt = UpdatedAt;
        }

        var nextIndex = _periods.Count;
        CurrentPeriodCode = nextIndex < BoxingRules.PeriodCount
            ? BoxingRules.PeriodCodes[nextIndex]
            : BoxingRules.PeriodCodes[^1];

        RaiseDomainEvent(new UnitResultPeriodScoredEvent(
            UnitRsc.Value,
            periodCode,
            cards.Select(c => new ScorecardSnapshot(
                c.JudgePos.ToString(), c.HomeScore, c.AwayScore)).ToList(),
            UpdatedAt.Value));

        return Result.Success;
    }
}
