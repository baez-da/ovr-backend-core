using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.Progression.Persistence;

namespace OVR.Modules.Progression;

public static class ProgressionModule
{
    public static IServiceCollection AddProgressionModule(this IServiceCollection services)
    {
        services.AddScoped<IBracketProgressionRepository, MongoBracketProgressionRepository>();
        services.AddHostedService<ProgressionIndexInitializer>();

        var assembly = Assembly.GetExecutingAssembly();
        // MediatR handlers are registered globally via Program.cs (covers all module assemblies).
        // Only FluentValidation validators need module-level registration here.
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapProgressionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/progression")
            .WithTags("Progression");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "Progression module" }))
            .WithName("GetProgression");

        return app;
    }
}
