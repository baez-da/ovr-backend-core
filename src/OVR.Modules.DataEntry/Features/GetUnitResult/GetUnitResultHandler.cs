using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;

namespace OVR.Modules.DataEntry.Features.GetUnitResult;

public sealed class GetUnitResultHandler
    : IRequestHandler<GetUnitResultQuery, ErrorOr<UnitResultResponse>>
{
    private readonly IUnitResultRepository _repository;

    public GetUnitResultHandler(IUnitResultRepository repository)
    {
        _repository = repository;
    }

    public async Task<ErrorOr<UnitResultResponse>> Handle(
        GetUnitResultQuery request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);
        return Map(ur);
    }

    internal static UnitResultResponse Map(UnitResult ur) => new(
        UnitRsc: ur.UnitRsc.Value,
        Status: ur.Status.ToString(),
        CurrentPeriodCode: ur.CurrentPeriodCode,
        StartedAt: ur.StartedAt,
        EndedAt: ur.EndedAt,
        Competitors: ur.Competitors.Select(c => new CompetitorResponse(
            c.SortOrder, c.ParticipantId?.Value, c.Seed,
            c.Organisation.Code, c.Wlt?.ToString())).ToList(),
        Periods: ur.Periods.Select(p => new PeriodResponse(
            p.Code, p.Scorecards.Select(s => new ScorecardResponse(
                s.JudgePos.ToString(), s.HomeScore, s.AwayScore)).ToList())).ToList(),
        Decision: ur.Decision is null ? null : new DecisionResponse(
            ur.Decision.Type.ToString(), ur.Decision.Code.ToString(),
            ur.Decision.DecisionMark, ur.Decision.StoppageRound, ur.Decision.StoppageTime,
            ur.Decision.WinnerParticipantId?.Value),
        CreatedAt: ur.CreatedAt,
        UpdatedAt: ur.UpdatedAt);
}
