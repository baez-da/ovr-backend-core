namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitResultPeriodScoredEvent(
    string UnitRsc,
    string PeriodCode,
    IReadOnlyList<ScorecardSnapshot> Scorecards,
    DateTime ScoredAt) : DomainEventBase;

public sealed record ScorecardSnapshot(
    string JudgePos,
    int HomeScore,
    int AwayScore);
