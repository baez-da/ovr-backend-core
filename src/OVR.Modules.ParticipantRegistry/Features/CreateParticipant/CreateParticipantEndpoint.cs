using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public static class CreateParticipantEndpoint
{
    public static async Task<IResult> Handle(
        CreateParticipantCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult($"/api/participants/{command.ParticipantId}");
    }
}
