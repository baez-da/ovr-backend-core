using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.OfficialAssignment;

public static class OfficialAssignmentModule
{
    public static IServiceCollection AddOfficialAssignmentModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapOfficialAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/official-assignments")
            .WithTags("OfficialAssignment");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "OfficialAssignment module" }))
            .WithName("GetOfficialAssignments");

        return app;
    }
}
