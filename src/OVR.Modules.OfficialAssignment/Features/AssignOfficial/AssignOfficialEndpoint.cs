using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.OfficialAssignment.Features.AssignOfficial;

public static class AssignOfficialEndpoint
{
    public static async Task<IResult> Handle(
        AssignOfficialCommand command,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult($"/api/official-assignments/{command.ParticipantId}_{command.UnitRsc}", httpContext);
    }
}
