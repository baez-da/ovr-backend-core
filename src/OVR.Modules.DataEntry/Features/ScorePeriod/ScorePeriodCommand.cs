using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.ScorePeriod;

public sealed record ScorePeriodCommand(
    string UnitRsc,
    string PeriodCode,
    IReadOnlyList<ScorecardDto> Scorecards) : IRequest<ErrorOr<Success>>;

public sealed record ScorecardDto(string JudgePos, int HomeScore, int AwayScore);
