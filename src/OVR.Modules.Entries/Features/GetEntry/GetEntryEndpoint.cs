using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Entries.Features.GetEntry;

public static class GetEntryEndpoint
{
    public static async Task<IResult> Handle(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetEntryQuery(id), ct);
        return result.ToApiResult();
    }
}
