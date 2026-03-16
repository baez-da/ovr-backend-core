using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.CoachAssignment;

public static class CoachAssignmentModule
{
    public static IServiceCollection AddCoachAssignmentModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapCoachAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coach-assignments")
            .WithTags("CoachAssignment");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "CoachAssignment module" }))
            .WithName("GetCoachAssignments");

        return app;
    }
}
