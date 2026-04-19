using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.DataEntry.Features.ListUnitResults;

public static class ListUnitResultsEndpoint
{
    public static async Task<IResult> Handle(
        string? sessionCode, string? locationCode, string? status,
        ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(
            new ListUnitResultsQuery(sessionCode, locationCode, status), ct);
        return result.ToApiResult(httpContext);
    }
}
