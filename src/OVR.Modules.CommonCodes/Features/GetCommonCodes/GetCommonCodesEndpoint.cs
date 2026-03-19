using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.CommonCodes.Features.GetCommonCodes;

public static class GetCommonCodesEndpoint
{
    public static IEndpointRouteBuilder MapGetCommonCodesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/common-codes", Handle)
            .WithName("GetCommonCodes")
            .WithTags("CommonCodes");

        return app;
    }

    private static async Task<IResult> Handle(
        string? type,
        string? languages,
        ISender sender,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var langArray = languages?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var query = new GetCommonCodesQuery(type ?? string.Empty, langArray);
        var result = await sender.Send(query, ct);

        return result.ToApiResult(httpContext);
    }
}
