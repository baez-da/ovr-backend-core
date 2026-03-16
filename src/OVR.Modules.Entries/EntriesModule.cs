using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.Entries;

public static class EntriesModule
{
    public static IServiceCollection AddEntriesModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapEntriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entries")
            .WithTags("Entries");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "Entries module" }))
            .WithName("GetEntries");

        return app;
    }
}
