using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.TeamComposition;

public static class TeamCompositionModule
{
    public static IServiceCollection AddTeamCompositionModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapTeamCompositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams")
            .WithTags("TeamComposition");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "TeamComposition module" }))
            .WithName("GetTeams");

        return app;
    }
}
