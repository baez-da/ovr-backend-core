using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.CoachAssignment.Features.GetCoachAssignment;

public static class GetCoachAssignmentEndpoint
{
    public static async Task<IResult> Handle(
        string id,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new GetCoachAssignmentQuery(id), ct);
        return result.ToApiResult(httpContext);
    }
}
