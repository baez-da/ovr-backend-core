using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.DataEntry.Features.ConfirmUnitResult;
using OVR.Modules.DataEntry.Features.FinishByStoppage;
using OVR.Modules.DataEntry.Features.GetUnitResult;
using OVR.Modules.DataEntry.Features.ListUnitResults;
using OVR.Modules.DataEntry.Features.ScorePeriod;
using OVR.Modules.DataEntry.Features.StartUnit;
using OVR.Modules.DataEntry.Lineup;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.DataEntry.SportRules;

namespace OVR.Modules.DataEntry;

public static class DataEntryModule
{
    public static IServiceCollection AddDataEntryModule(this IServiceCollection services)
    {
        services.AddScoped<IUnitResultRepository, MongoUnitResultRepository>();
        services.AddSingleton<IFirstRoundLineupResolver, SeedBasedFirstRoundLineupResolver>();
        services.AddSingleton<ITenPointMustResolver, TenPointMustResolver>();
        services.AddHostedService<DataEntryIndexInitializer>();

        var assembly = Assembly.GetExecutingAssembly();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapDataEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/data-entry").WithTags("DataEntry");

        group.MapGet("/unit-results/{rsc}", GetUnitResultEndpoint.Handle)
            .WithName("GetUnitResult");

        group.MapGet("/unit-results", ListUnitResultsEndpoint.Handle)
            .WithName("ListUnitResults");

        group.MapPost("/unit-results/{rsc}/start", StartUnitEndpoint.Handle)
            .WithName("StartUnit");

        group.MapPost("/unit-results/{rsc}/periods/{code}/score", ScorePeriodEndpoint.Handle)
            .WithName("ScorePeriod");

        group.MapPost("/unit-results/{rsc}/finish-stoppage", FinishByStoppageEndpoint.Handle)
            .WithName("FinishByStoppage");

        group.MapPost("/unit-results/{rsc}/confirm", ConfirmUnitResultEndpoint.Handle)
            .WithName("ConfirmUnitResult");

        return app;
    }
}
