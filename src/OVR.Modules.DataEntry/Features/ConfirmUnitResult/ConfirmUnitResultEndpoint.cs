using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.ConfirmUnitResult;

public static class ConfirmUnitResultEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(new ConfirmUnitResultCommand(rsc), ct);
        return result.ToApiResult(httpContext);
    }
}
