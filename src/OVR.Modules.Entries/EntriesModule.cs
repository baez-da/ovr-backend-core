using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.Entries.Features.ChangeEntryStatus;
using OVR.Modules.Entries.Features.CreateEntry;
using OVR.Modules.Entries.Features.GetEntry;
using OVR.Modules.Entries.Features.ListEntriesByRsc;
using OVR.Modules.Entries.Features.WithdrawEntry;
using OVR.Modules.Entries.Persistence;

namespace OVR.Modules.Entries;

public static class EntriesModule
{
    public static IServiceCollection AddEntriesModule(this IServiceCollection services)
    {
        services.AddScoped<IEntryRepository, MongoEntryRepository>();
        return services;
    }

    public static IEndpointRouteBuilder MapEntriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entries")
            .WithTags("Entries");

        group.MapPost("/", CreateEntryEndpoint.Handle)
            .WithName("CreateEntry");

        group.MapGet("/{id}", GetEntryEndpoint.Handle)
            .WithName("GetEntry");

        group.MapGet("/by-rsc/{rscPrefix}", ListEntriesByRscEndpoint.Handle)
            .WithName("ListEntriesByRsc");

        group.MapPatch("/{id}/status", ChangeEntryStatusEndpoint.Handle)
            .WithName("ChangeEntryStatus");

        group.MapPost("/{id}/withdraw", WithdrawEntryEndpoint.Handle)
            .WithName("WithdrawEntry");

        return app;
    }
}
