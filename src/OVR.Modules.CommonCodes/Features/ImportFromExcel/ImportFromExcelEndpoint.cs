using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.CommonCodes.Features.ImportFromExcel;

public static class ImportFromExcelEndpoint
{
    public static IEndpointRouteBuilder MapImportFromExcelEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/common-codes/import", Handle)
            .WithName("ImportCommonCodes")
            .WithTags("CommonCodes")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> Handle(
        IFormFile file,
        string strategy,
        ISender sender,
        HttpContext httpContext,
        CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Position = 0;

        var command = new ImportFromExcelCommand(stream, strategy);
        var result = await sender.Send(command, ct);

        return result.ToApiResult(httpContext);
    }
}
