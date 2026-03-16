using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByEvent;

public static class ListParticipantsByEventEndpoint
{
    public static async Task<IResult> Handle(
        string noc,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ListParticipantsByEventQuery(noc), ct);
        return result.ToApiResult();
    }
}
