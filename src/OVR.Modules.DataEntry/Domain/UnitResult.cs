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

    public ErrorOr<Success> FinishByStoppage(
        ResultCode resultCode,
        string stoppageRound,
        string stoppageTime,
        ParticipantId? winnerParticipantId)
    {
        if (Status != ResultStatus.Live)
            return DataEntryErrors.InvalidStatusTransition(Status.ToString(), "finish");

        if (Decision is not null)
            return DataEntryErrors.DecisionAlreadyExists();

        if (resultCode == ResultCode.Wp)
            return DataEntryErrors.InvalidStoppageData(
                "WP is reserved for point decisions, not stoppages.");

        if (!BoxingRules.PeriodCodes.Contains(stoppageRound))
            return DataEntryErrors.InvalidStoppageData(
                $"Invalid stoppage round '{stoppageRound}'.");

        var noWinnerCodes = new[] { ResultCode.Nc, ResultCode.Dko, ResultCode.Bdsq };
        var requiresWinner = !noWinnerCodes.Contains(resultCode);

        if (requiresWinner && winnerParticipantId is null)
            return DataEntryErrors.InvalidStoppageData(
                $"ResultCode {resultCode} requires a winnerParticipantId.");

        if (!requiresWinner && winnerParticipantId is not null)
            return DataEntryErrors.InvalidStoppageData(
                $"ResultCode {resultCode} must not have a winnerParticipantId.");

        if (winnerParticipantId is not null &&
            !_competitors.Any(c => c.ParticipantId == winnerParticipantId))
            return DataEntryErrors.InvalidStoppageData(
                "winnerParticipantId does not match any competitor.");

        var type = _periods.Count > 0 ? ResultType.RmPoints : ResultType.Rm;

        var decisionMark = type == ResultType.RmPoints
            ? ComputeInterimDecisionMark(winnerParticipantId)
            : null;

        Decision = new Decision(
            Type: type,
            Code: resultCode,
            DecisionMark: decisionMark,
            StoppageRound: stoppageRound,
            StoppageTime: stoppageTime,
            WinnerParticipantId: winnerParticipantId);

        EndedAt = DateTime.UtcNow;
        UpdatedAt = EndedAt;

        return Result.Success;
    }

    private string? ComputeInterimDecisionMark(ParticipantId? winner)
    {
        if (winner is null) return null;
        var red = _competitors.First(c => c.SortOrder == 1);
        var blue = _competitors.First(c => c.SortOrder == 2);
        var resolver = new TenPointMustResolver();
        var interim = resolver.Resolve(_periods, red.ParticipantId!, blue.ParticipantId!);
        return interim.DecisionMark;
    }

    public ErrorOr<Success> Confirm()
    {
        if (Status != ResultStatus.Live)
            return DataEntryErrors.InvalidStatusTransition(Status.ToString(), "Official");

        if (Decision is null)
            return DataEntryErrors.DecisionRequired();

        var winner = Decision.WinnerParticipantId;
        var newCompetitors = _competitors.Select(c =>
        {
            Wlt wlt;
            if (winner is null)
                wlt = Wlt.L;
            else
                wlt = c.ParticipantId == winner ? Wlt.W : Wlt.L;
            return c with { Wlt = wlt };
        }).ToList();
        _competitors.Clear();
        _competitors.AddRange(newCompetitors);

        Status = ResultStatus.Official;
        var confirmedAt = DateTime.UtcNow;
        UpdatedAt = confirmedAt;
        if (EndedAt is null) EndedAt = confirmedAt;

        RaiseDomainEvent(new UnitResultOfficialEvent(
            UnitRsc: UnitRsc.Value,
            WinnerParticipantId: winner?.Value,
            ResultCode: Decision.Code.ToString(),
            ResultType: Decision.Type.ToString(),
            DecisionMark: Decision.DecisionMark,
            StoppageRound: Decision.StoppageRound,
            StoppageTime: Decision.StoppageTime,
            ConfirmedAt: confirmedAt));

        return Result.Success;
    }

    /// <summary>
    /// Creates an empty StartList UnitResult for a later-round unit whose competitors
    /// have not yet advanced. The two competitor slots are initialised with no ParticipantId;
    /// they are filled later via <see cref="AdvanceCompetitor"/>.
    /// Raises <see cref="UnitResultStartListCreatedEvent"/> so Progression can flush any
    /// buffered advancements immediately.
    /// </summary>
    public static ErrorOr<UnitResult> CreateForLaterRound(Rsc unitRsc)
    {
        if (unitRsc is null)
            return DataEntryErrors.InvalidCompetitors("UnitRsc is required.");

        var now = DateTime.UtcNow;
        var ur = new UnitResult
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            Status = ResultStatus.StartList,
            CreatedAt = now
        };

        // Two empty slots — ParticipantId is null until a competitor advances in.
        var red  = new Competitor(1, null, null, null, Organisation.Create("TBD"), null);
        var blue = new Competitor(2, null, null, null, Organisation.Create("TBD"), null);
        ur._competitors.Add(red);
        ur._competitors.Add(blue);

        ur.RaiseDomainEvent(new UnitResultStartListCreatedEvent(
            UnitRsc: unitRsc.Value,
            EventRsc: Rsc.Create(unitRsc.AtEventLevel()).Value,
            Competitors: new[]
            {
                new CompetitorSnapshot(red.SortOrder,  red.ParticipantId?.Value,  red.Seed,  red.Organisation.Code),
                new CompetitorSnapshot(blue.SortOrder, blue.ParticipantId?.Value, blue.Seed, blue.Organisation.Code)
            },
            CreatedAt: now));

        return ur;
    }

    /// <summary>
    /// Creates a UnitResult that is immediately Official because the opponent is a bye.
    /// Raises both <see cref="UnitResultStartListCreatedEvent"/> and
    /// <see cref="UnitResultOfficialEvent"/> so downstream handlers receive the full
    /// state machine trace even though the unit never enters Live.
    /// </summary>
    public static ErrorOr<UnitResult> CreateByeOfficial(Rsc unitRsc, Competitor winner)
    {
        if (unitRsc is null)
            return DataEntryErrors.InvalidCompetitors("UnitRsc is required.");

        if (winner.ParticipantId is null)
            return DataEntryErrors.InvalidCompetitors(
                "Bye winner must have a real ParticipantId.");

        if (winner.SortOrder < 1 || winner.SortOrder > 2)
            return DataEntryErrors.InvalidCompetitors(
                "Bye winner must have SortOrder 1 or 2.");

        var now = DateTime.UtcNow;

        // Bye = walkover (Wo). No periods, no scoring → Rm decision type.
        var byeDecision = new Decision(
            Type: ResultType.Rm,
            Code: ResultCode.Wo,
            DecisionMark: null,
            StoppageRound: null,
            StoppageTime: null,
            WinnerParticipantId: winner.ParticipantId);

        var ur = new UnitResult
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            Status = ResultStatus.Official,
            Decision = byeDecision,
            CreatedAt = now,
            UpdatedAt = now,
            EndedAt = now
        };

        var byeSlot = winner.SortOrder == 1 ? 2 : 1;
        var bye = new Competitor(byeSlot, null, null, null,
            Organisation.Create("BYE"), Wlt.L);
        var winnerWithWlt = winner with { Wlt = Wlt.W };

        ur._competitors.Add(winnerWithWlt);
        ur._competitors.Add(bye);

        ur.RaiseDomainEvent(new UnitResultStartListCreatedEvent(
            UnitRsc: unitRsc.Value,
            EventRsc: Rsc.Create(unitRsc.AtEventLevel()).Value,
            Competitors: new[]
            {
                new CompetitorSnapshot(winnerWithWlt.SortOrder,
                    winnerWithWlt.ParticipantId?.Value, winnerWithWlt.Seed,
                    winnerWithWlt.Organisation.Code),
                new CompetitorSnapshot(bye.SortOrder,
                    bye.ParticipantId?.Value, bye.Seed, bye.Organisation.Code)
            },
            CreatedAt: now));

        ur.RaiseDomainEvent(new UnitResultOfficialEvent(
            UnitRsc: unitRsc.Value,
            WinnerParticipantId: winnerWithWlt.ParticipantId?.Value,
            ResultCode: byeDecision.Code.ToString(),
            ResultType: byeDecision.Type.ToString(),
            DecisionMark: null,
            StoppageRound: null,
            StoppageTime: null,
            ConfirmedAt: now));

        return ur;
    }

    // Pre-condition: UnitResult is in StartList state. Progression guarantees this by
    // withholding advancements until UnitResultStartListCreatedEvent has fired.
    // Revisit if direct-advancement-without-scheduling becomes a requirement.
    public ErrorOr<Success> AdvanceCompetitor(int slot, ParticipantId participantId)
    {
        if (slot < 1 || slot > 2)
            return DataEntryErrors.InvalidSlot(slot);

        if (Status != ResultStatus.StartList)
            return DataEntryErrors.UnitNotInStartList(UnitRsc.Value);

        var existing = _competitors.FirstOrDefault(c => c.SortOrder == slot);
        if (existing is not null && existing.ParticipantId == participantId)
            return Result.Success; // idempotent no-op

        if (existing is not null && existing.ParticipantId is not null)
            return DataEntryErrors.SlotConflict(
                UnitRsc.Value, slot,
                existing.ParticipantId.Value,
                participantId.Value);

        // Slot is empty (ParticipantId is null) — fill it.
        var updated = existing! with { ParticipantId = participantId };
        var index = _competitors.IndexOf(existing);
        _competitors[index] = updated;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success;
    }

    internal static UnitResult Hydrate(
        Rsc unitRsc,
        ResultStatus status,
        IReadOnlyList<Competitor> competitors,
        IReadOnlyList<Period> periods,
        Decision? decision,
        DateTime? startedAt,
        DateTime? endedAt,
        string? currentPeriodCode,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var ur = new UnitResult
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            Status = status,
            Decision = decision,
            StartedAt = startedAt,
            EndedAt = endedAt,
            CurrentPeriodCode = currentPeriodCode,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        ur._competitors.AddRange(competitors);
        ur._periods.AddRange(periods);
        return ur;
    }
}
