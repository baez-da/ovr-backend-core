using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.OfficialAssignment.Features.AssignOfficial;
using OVR.Modules.OfficialAssignment.Features.GetOfficialAssignment;
using OVR.Modules.OfficialAssignment.Features.ListOfficialsByUnit;
using OVR.Modules.OfficialAssignment.Persistence;

namespace OVR.Modules.OfficialAssignment;

public static class OfficialAssignmentModule
{
    public static IServiceCollection AddOfficialAssignmentModule(this IServiceCollection services)
    {
        services.AddScoped<IOfficialAssignmentRepository, MongoOfficialAssignmentRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapOfficialAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/official-assignments")
            .WithTags("OfficialAssignment");

        group.MapPost("/", AssignOfficialEndpoint.Handle)
            .WithName("AssignOfficial");

        group.MapGet("/{id}", GetOfficialAssignmentEndpoint.Handle)
            .WithName("GetOfficialAssignment");

        group.MapGet("/by-rsc/{rscPrefix}", ListOfficialsByUnitEndpoint.Handle)
            .WithName("ListOfficialsByUnit");

        return app;
    }
}
