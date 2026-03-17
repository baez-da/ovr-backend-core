using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Entries.Features.CreateEntry;

public static class CreateEntryEndpoint
{
    public static async Task<IResult> Handle(
        CreateEntryCommand command,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult($"/api/entries/{command.ParticipantId}_{command.EventRsc}", httpContext);
    }
}
