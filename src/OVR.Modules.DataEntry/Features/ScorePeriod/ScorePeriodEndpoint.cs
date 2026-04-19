using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.ScorePeriod;

public static class ScorePeriodEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, string code, ScorePeriodBody body,
        ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var command = new ScorePeriodCommand(rsc, code,
            body.Scorecards.Select(s =>
                new ScorecardDto(s.JudgePos, s.HomeScore, s.AwayScore)).ToList());
        var result = await sender.Send(command, ct);
        return result.ToApiResult(httpContext);
    }
}

public sealed record ScorePeriodBody(IReadOnlyList<ScorecardBody> Scorecards);
public sealed record ScorecardBody(string JudgePos, int HomeScore, int AwayScore);
