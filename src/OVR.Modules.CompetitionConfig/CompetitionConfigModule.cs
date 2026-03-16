using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.CompetitionConfig;

public static class CompetitionConfigModule
{
    public static IServiceCollection AddCompetitionConfigModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapCompetitionConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/competition-config")
            .WithTags("CompetitionConfig");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "CompetitionConfig module" }))
            .WithName("GetCompetitionConfig");

        return app;
    }
}
