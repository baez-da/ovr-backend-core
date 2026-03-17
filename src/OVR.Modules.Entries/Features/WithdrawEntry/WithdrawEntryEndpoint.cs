using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Entries.Features.WithdrawEntry;

public static class WithdrawEntryEndpoint
{
    public static async Task<IResult> Handle(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new WithdrawEntryCommand(id), ct);
        return result.ToApiResult();
    }
}
