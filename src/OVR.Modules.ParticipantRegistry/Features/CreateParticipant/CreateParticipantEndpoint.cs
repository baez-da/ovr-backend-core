using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public static class CreateParticipantEndpoint
{
    public static async Task<IResult> Handle(
        CreateParticipantCommand command,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(
            result.IsError ? string.Empty : $"/api/participants/{result.Value.Id}",
            httpContext);
    }
}
