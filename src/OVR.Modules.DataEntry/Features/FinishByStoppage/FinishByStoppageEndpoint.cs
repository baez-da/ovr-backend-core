using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.FinishByStoppage;

public static class FinishByStoppageEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, FinishByStoppageBody body,
        ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var command = new FinishByStoppageCommand(
            rsc, body.ResultCode, body.StoppageRound, body.StoppageTime, body.WinnerParticipantId);
        var result = await sender.Send(command, ct);
        return result.ToApiResult(httpContext);
    }
}

public sealed record FinishByStoppageBody(
    string ResultCode,
    string StoppageRound,
    string StoppageTime,
    string? WinnerParticipantId);
