using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public static class GetParticipantEndpoint
{
    public static async Task<IResult> Handle(
        string id,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new GetParticipantQuery(id), ct);
        return result.ToApiResult(httpContext);
    }
}
