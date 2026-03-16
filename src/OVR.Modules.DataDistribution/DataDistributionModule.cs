using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.DataDistribution;

public static class DataDistributionModule
{
    public static IServiceCollection AddDataDistributionModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapDataDistributionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/distribution")
            .WithTags("DataDistribution");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "DataDistribution module" }))
            .WithName("GetDistribution");

        return app;
    }
}
