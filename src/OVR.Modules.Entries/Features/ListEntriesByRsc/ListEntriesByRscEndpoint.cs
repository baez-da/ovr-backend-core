using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Entries.Features.ListEntriesByRsc;

public static class ListEntriesByRscEndpoint
{
    public static async Task<IResult> Handle(
        string rscPrefix,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new ListEntriesByRscQuery(rscPrefix), ct);
        return result.ToApiResult(httpContext);
    }
}
