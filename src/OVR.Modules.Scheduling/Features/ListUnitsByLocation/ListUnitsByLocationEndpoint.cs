using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.ListUnitsByLocation;

public static class ListUnitsByLocationEndpoint
{
    public static async Task<IResult> Handle(
        string locationCode,
        DateOnly? date,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new ListUnitsByLocationQuery(locationCode, date), ct);
        return result.ToApiResult(httpContext);
    }
}
