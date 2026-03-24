using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Reporting.Features.SeedTemplates;

public static class SeedTemplatesEndpoint
{
    public static async Task<IResult> Handle(
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new SeedTemplatesCommand(), ct);
        return result.ToApiResult(httpContext);
    }
}
