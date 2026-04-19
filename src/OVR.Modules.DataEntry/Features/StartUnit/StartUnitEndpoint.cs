using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.StartUnit;

public static class StartUnitEndpoint
{
    public static async Task<IResult> Handle(
        string rsc, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(new StartUnitCommand(rsc), ct);
        return result.ToApiResult(httpContext);
    }
}
