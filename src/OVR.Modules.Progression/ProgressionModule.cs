using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.Progression;

public static class ProgressionModule
{
    public static IServiceCollection AddProgressionModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapProgressionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/progression")
            .WithTags("Progression");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "Progression module" }))
            .WithName("GetProgression");

        return app;
    }
}
