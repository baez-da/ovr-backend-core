using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.DataEntry;

public static class DataEntryModule
{
    public static IServiceCollection AddDataEntryModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapDataEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/data-entry")
            .WithTags("DataEntry");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "DataEntry module" }))
            .WithName("GetDataEntry");

        return app;
    }
}
