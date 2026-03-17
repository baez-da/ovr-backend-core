using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.OfficialAssignment.Features.GetOfficialAssignment;

public static class GetOfficialAssignmentEndpoint
{
    public static async Task<IResult> Handle(
        string id,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new GetOfficialAssignmentQuery(id), ct);
        return result.ToApiResult(httpContext);
    }
}
