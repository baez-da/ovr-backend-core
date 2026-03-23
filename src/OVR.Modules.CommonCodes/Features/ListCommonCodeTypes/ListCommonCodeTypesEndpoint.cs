using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.CommonCodes.Features.ListCommonCodeTypes;

public static class ListCommonCodeTypesEndpoint
{
    public static IEndpointRouteBuilder MapListCommonCodeTypesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/common-codes/types", Handle)
            .WithName("ListCommonCodeTypes")
            .WithTags("CommonCodes");

        return app;
    }

    private static async Task<IResult> Handle(
        ISender sender,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var result = await sender.Send(new ListCommonCodeTypesQuery(), ct);
        return result.ToApiResult(httpContext);
    }
}
