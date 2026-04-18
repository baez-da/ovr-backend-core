using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.Scheduling.Domain;
using OVR.Modules.Scheduling.Features.CreateSession;
using OVR.Modules.Scheduling.Features.ListUnitsByLocation;
using OVR.Modules.Scheduling.Features.RescheduleUnit;
using OVR.Modules.Scheduling.Features.ScheduleUnit;
using OVR.Modules.Scheduling.Features.UnscheduleUnit;
using OVR.Modules.Scheduling.Persistence;

namespace OVR.Modules.Scheduling;

public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services)
    {
        services.AddScoped<ISessionRepository, MongoSessionRepository>();
        services.AddScoped<IUnitScheduleRepository, MongoUnitScheduleRepository>();
        services.AddScoped<IScheduleCollisionDetector, ScheduleCollisionDetector>();
        return services;
    }

    public static IEndpointRouteBuilder MapSchedulingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scheduling")
            .WithTags("Scheduling");

        group.MapPost("/sessions", CreateSessionEndpoint.Handle)
            .WithName("CreateSession");

        group.MapPost("/sessions/{sessionCode}/schedule-unit", ScheduleUnitEndpoint.Handle)
            .WithName("ScheduleUnit");

        group.MapPatch("/unit-schedules/{unitRsc}", RescheduleUnitEndpoint.Handle)
            .WithName("RescheduleUnit");

        group.MapDelete("/unit-schedules/{unitRsc}", UnscheduleUnitEndpoint.Handle)
            .WithName("UnscheduleUnit");

        group.MapGet("/locations/{locationCode}/today", ListUnitsByLocationEndpoint.Handle)
            .WithName("ListUnitsByLocation");

        return app;
    }
}
