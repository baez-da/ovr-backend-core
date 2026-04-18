using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.UnscheduleUnit;

public static class UnscheduleUnitEndpoint
{
    public static async Task<IResult> Handle(
        string unitRsc,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new UnscheduleUnitCommand(unitRsc), ct);
        if (result.IsError)
            return result.ToApiResult(httpContext);
        return Results.NoContent();
    }
}
