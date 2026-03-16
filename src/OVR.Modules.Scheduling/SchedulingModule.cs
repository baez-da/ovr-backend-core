using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.Scheduling;

public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/schedule")
            .WithTags("Scheduling");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "Scheduling module" }))
            .WithName("GetSchedule");

        return app;
    }
}
