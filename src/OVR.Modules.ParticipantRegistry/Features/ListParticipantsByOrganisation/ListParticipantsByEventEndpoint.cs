using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByOrganisation;

public static class ListParticipantsByEventEndpoint
{
    public static async Task<IResult> Handle(
        string organisation, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var result = await sender.Send(new ListParticipantsByOrganisationQuery(organisation), ct);
        return result.ToApiResult(httpContext);
    }
}
