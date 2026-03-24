using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OVR.Modules.Reporting.DataProviders;
using OVR.Modules.Reporting.Features.GetReport;
using OVR.Modules.Reporting.Features.PreviewCommunication;
using OVR.Modules.Reporting.Features.PreviewDailySchedule;
using OVR.Modules.Reporting.Features.PreviewReport;
using OVR.Modules.Reporting.Features.PublishCommunication;
using OVR.Modules.Reporting.Features.PublishDailySchedule;
using OVR.Modules.Reporting.Features.PublishReport;
using OVR.Modules.Reporting.Features.SeedTemplates;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddScoped<IReportTemplateRepository, MongoReportTemplateRepository>();
        services.AddScoped<IReportLayoutRepository, MongoReportLayoutRepository>();
        services.AddScoped<IReportPartialRepository, MongoReportPartialRepository>();
        services.AddScoped<IReportRecordRepository, MongoReportRecordRepository>();

        services.AddScoped<ITemplateResolver, TemplateResolver>();
        services.AddScoped<IScribanEngine, ScribanEngine>();
        services.AddSingleton<IPdfRenderer, PlaywrightPdfRenderer>();
        services.AddScoped<DataProviderFactory>();
        services.AddScoped<IReportGenerator, ReportGenerator>();

        services.AddScoped<IReportDataProvider, C51DataProvider>();

        services.AddScoped<IS3Storage, NoOpS3Storage>();

        return services;
    }

    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reporting");

        group.MapPost("/preview", PreviewReportEndpoint.Handle).WithName("PreviewReport");
        group.MapPost("/publish", PublishReportEndpoint.Handle).WithName("PublishReport");
        group.MapGet("/", GetReportEndpoint.Handle).WithName("GetReport");
        group.MapPost("/templates/seed", SeedTemplatesEndpoint.Handle).WithName("SeedTemplates");

        var commsGroup = app.MapGroup("/api/reports/communications").WithTags("Reporting");
        commsGroup.MapPost("/preview", PreviewCommunicationEndpoint.Handle).WithName("PreviewCommunication");
        commsGroup.MapPost("/publish", PublishCommunicationEndpoint.Handle).WithName("PublishCommunication");

        var schedGroup = app.MapGroup("/api/reports/daily-schedule").WithTags("Reporting");
        schedGroup.MapPost("/preview", PreviewDailyScheduleEndpoint.Handle).WithName("PreviewDailySchedule");
        schedGroup.MapPost("/publish", PublishDailyScheduleEndpoint.Handle).WithName("PublishDailySchedule");

        return app;
    }
}
