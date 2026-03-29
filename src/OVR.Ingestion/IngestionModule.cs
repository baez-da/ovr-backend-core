using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Ingestion.Features.ImportDtPartic;

namespace OVR.Ingestion;

public static class IngestionModule
{
    public static IServiceCollection AddIngestionModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapImportDtParticEndpoint();
        return app;
    }
}
