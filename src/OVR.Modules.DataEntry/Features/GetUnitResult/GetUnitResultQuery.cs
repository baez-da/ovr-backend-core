using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.GetUnitResult;

public sealed record GetUnitResultQuery(string UnitRsc)
    : IRequest<ErrorOr<UnitResultResponse>>;

public sealed record UnitResultResponse(
    string UnitRsc,
    string Status,
    string? CurrentPeriodCode,
    DateTime? StartedAt,
    DateTime? EndedAt,
    IReadOnlyList<CompetitorResponse> Competitors,
    IReadOnlyList<PeriodResponse> Periods,
    DecisionResponse? Decision,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CompetitorResponse(
    int SortOrder, string? ParticipantId, int? Seed,
    string Organisation, string? Wlt);

public sealed record PeriodResponse(
    string Code, IReadOnlyList<ScorecardResponse> Scorecards);

public sealed record ScorecardResponse(string JudgePos, int HomeScore, int AwayScore);

public sealed record DecisionResponse(
    string Type, string Code, string? DecisionMark,
    string? StoppageRound, string? StoppageTime, string? WinnerParticipantId);
