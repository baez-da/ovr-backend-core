using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByEvent;

public static class ListParticipantsByEventEndpoint
{
    public static async Task<IResult> Handle(
        string organisation,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ListParticipantsByEventQuery(organisation), ct);
        return result.ToApiResult();
    }
}
