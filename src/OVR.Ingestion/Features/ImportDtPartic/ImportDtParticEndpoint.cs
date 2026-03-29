using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OVR.SharedKernel.Extensions;

namespace OVR.Ingestion.Features.ImportDtPartic;

public static class ImportDtParticEndpoint
{
    public static IEndpointRouteBuilder MapImportDtParticEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ingestion/dt-partic", Handle)
            .WithName("ImportDtPartic")
            .WithTags("Ingestion")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> Handle(
        IFormFile file,
        ISender sender,
        HttpContext httpContext,
        CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, ct);
        stream.Position = 0;

        var command = new ImportDtParticCommand(stream);
        var result = await sender.Send(command, ct);

        return result.ToApiResult(httpContext);
    }
}
