using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.GetUnitResult;

public static class GetUnitResultEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(new GetUnitResultQuery(rsc), ct);
        return result.ToApiResult(httpContext);
    }
}
