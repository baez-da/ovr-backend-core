using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.ParticipantRegistry.Contracts;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;
using OVR.Modules.ParticipantRegistry.Features.CreateParticipant;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;
using OVR.Modules.ParticipantRegistry.Features.ListParticipantsByOrganisation;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.Modules.ParticipantRegistry.Services;

namespace OVR.Modules.ParticipantRegistry;

public static class ParticipantRegistryModule
{
    public static IServiceCollection AddParticipantRegistryModule(this IServiceCollection services)
    {
        services.AddScoped<IParticipantRepository, MongoParticipantRepository>();
        services.AddSingleton<INameBuilder, OdfNameBuilder>();
        services.AddScoped<IParticipantReader, ParticipantReaderService>();
        return services;
    }

    public static IEndpointRouteBuilder MapParticipantRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/participants")
            .WithTags("Participants");

        group.MapPost("/", CreateParticipantEndpoint.Handle)
            .WithName("CreateParticipant");

        group.MapGet("/{id}", GetParticipantEndpoint.Handle)
            .WithName("GetParticipant");

        group.MapGet("/by-organisation/{organisation}", ListParticipantsByEventEndpoint.Handle)
            .WithName("ListParticipantsByOrganisation");

        return app;
    }
}
