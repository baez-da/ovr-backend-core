using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.CompetitionConfig.Features.CreateEvent;

public static class CreateEventEndpoint
{
    public static async Task<IResult> Handle(
        CreateEventCommand command,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult($"/api/competition-config/events/{result.Value?.Rsc}", httpContext);
    }
}
