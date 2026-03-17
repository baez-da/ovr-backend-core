using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.CoachAssignment.Features.AssignCoach;
using OVR.Modules.CoachAssignment.Features.GetCoachAssignment;
using OVR.Modules.CoachAssignment.Features.ListCoachesByEvent;
using OVR.Modules.CoachAssignment.Persistence;

namespace OVR.Modules.CoachAssignment;

public static class CoachAssignmentModule
{
    public static IServiceCollection AddCoachAssignmentModule(this IServiceCollection services)
    {
        services.AddScoped<ICoachAssignmentRepository, MongoCoachAssignmentRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapCoachAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coach-assignments")
            .WithTags("CoachAssignment");

        group.MapPost("/", AssignCoachEndpoint.Handle)
            .WithName("AssignCoach");

        group.MapGet("/{id}", GetCoachAssignmentEndpoint.Handle)
            .WithName("GetCoachAssignment");

        group.MapGet("/by-rsc/{rscPrefix}", ListCoachesByEventEndpoint.Handle)
            .WithName("ListCoachesByEvent");

        return app;
    }
}
