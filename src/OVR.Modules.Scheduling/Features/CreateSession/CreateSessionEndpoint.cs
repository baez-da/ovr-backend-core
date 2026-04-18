using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.CreateSession;

public static class CreateSessionEndpoint
{
    public static async Task<IResult> Handle(
        CreateSessionCommand command,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(
            $"/api/scheduling/sessions/{command.Code}", httpContext);
    }
}
