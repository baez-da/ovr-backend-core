using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.CompetitionConfig.Contracts;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Features.CreateEvent;
using OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;
using OVR.Modules.CompetitionConfig.Persistence;

namespace OVR.Modules.CompetitionConfig;

public static class CompetitionConfigModule
{
    public static IServiceCollection AddCompetitionConfigModule(this IServiceCollection services)
    {
        services.AddScoped<IEventRepository, MongoEventRepository>();
        services.AddScoped<IUnitRepository, MongoUnitRepository>();
        services.AddScoped<IUnitLineupReader, MongoUnitLineupReader>();
        services.AddSingleton<BracketGenerator>();
        return services;
    }

    public static IEndpointRouteBuilder MapCompetitionConfigEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/competition-config")
            .WithTags("CompetitionConfig");

        group.MapPost("/events", CreateEventEndpoint.Handle)
            .WithName("CreateEvent");

        group.MapPost("/events/{rsc}/generate-structure", GenerateEventStructureEndpoint.Handle)
            .WithName("GenerateEventStructure");

        return app;
    }
}
