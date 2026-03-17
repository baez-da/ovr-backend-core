using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.CoachAssignment.Features.ListCoachesByEvent;

public static class ListCoachesByEventEndpoint
{
    public static async Task<IResult> Handle(
        string rscPrefix,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new ListCoachesByEventQuery(rscPrefix), ct);
        return result.ToApiResult(httpContext);
    }
}
