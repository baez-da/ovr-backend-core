using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace OVR.Modules.Reporting;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reporting");

        group.MapGet("/", () => TypedResults.Ok(new { Message = "Reporting module" }))
            .WithName("GetReports");

        return app;
    }
}
