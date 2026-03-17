using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Entries.Features.ChangeEntryStatus;

public static class ChangeEntryStatusEndpoint
{
    public static async Task<IResult> Handle(
        string id,
        ChangeEntryStatusRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ChangeEntryStatusCommand(id, request.NewStatus), ct);
        return result.ToApiResult();
    }
}

public sealed record ChangeEntryStatusRequest(string NewStatus);
