using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.OfficialAssignment.Features.ListOfficialsByUnit;

public static class ListOfficialsByUnitEndpoint
{
    public static async Task<IResult> Handle(
        string rscPrefix,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ListOfficialsByUnitQuery(rscPrefix), ct);
        return result.ToApiResult();
    }
}
