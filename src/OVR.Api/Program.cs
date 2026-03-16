using FluentValidation;
using MediatR;
using MongoDB.Driver;
using OVR.Api.Configuration;
using OVR.Modules.ParticipantRegistry;
using OVR.Modules.TeamComposition;
using OVR.Modules.CompetitionConfig;
using OVR.Modules.Scheduling;
using OVR.Modules.Entries;
using OVR.Modules.OfficialAssignment;
using OVR.Modules.CoachAssignment;
using OVR.Modules.DataEntry;
using OVR.Modules.Progression;
using OVR.Modules.Reporting;
using OVR.Modules.DataDistribution;
using OVR.SharedKernel.Behaviors;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // MongoDB
    builder.Services.AddOptions<MongoDbOptions>()
        .BindConfiguration(MongoDbOptions.Section)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddSingleton<IMongoClient>(sp =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
        return new MongoClient(options.ConnectionString);
    });

    builder.Services.AddSingleton<IMongoDatabase>(sp =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
        var client = sp.GetRequiredService<IMongoClient>();
        return client.GetDatabase(options.DatabaseName);
    });

    // MediatR with pipeline behaviors
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssemblies(
            typeof(OVR.SharedKernel.Behaviors.ValidationBehavior<,>).Assembly,
            typeof(ParticipantRegistryModule).Assembly,
            typeof(TeamCompositionModule).Assembly,
            typeof(CompetitionConfigModule).Assembly,
            typeof(SchedulingModule).Assembly,
            typeof(EntriesModule).Assembly,
            typeof(OfficialAssignmentModule).Assembly,
            typeof(CoachAssignmentModule).Assembly,
            typeof(DataEntryModule).Assembly,
            typeof(ProgressionModule).Assembly,
            typeof(ReportingModule).Assembly,
            typeof(DataDistributionModule).Assembly);

        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });

    // FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(ParticipantRegistryModule).Assembly);

    // Core services
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddOpenApi();

    // Modules
    builder.Services.AddParticipantRegistryModule();
    builder.Services.AddTeamCompositionModule();
    builder.Services.AddCompetitionConfigModule();
    builder.Services.AddSchedulingModule();
    builder.Services.AddEntriesModule();
    builder.Services.AddOfficialAssignmentModule();
    builder.Services.AddCoachAssignmentModule();
    builder.Services.AddDataEntryModule();
    builder.Services.AddProgressionModule();
    builder.Services.AddReportingModule();
    builder.Services.AddDataDistributionModule();

    var app = builder.Build();

    // Middleware
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    app.MapOpenApi();
    app.MapGet("/", () => TypedResults.Ok(new { Name = "OVR Backend Core", Version = "0.1.0" }));

    // Module endpoints
    app.MapParticipantRegistryEndpoints();
    app.MapTeamCompositionEndpoints();
    app.MapCompetitionConfigEndpoints();
    app.MapSchedulingEndpoints();
    app.MapEntriesEndpoints();
    app.MapOfficialAssignmentEndpoints();
    app.MapCoachAssignmentEndpoints();
    app.MapDataEntryEndpoints();
    app.MapProgressionEndpoints();
    app.MapReportingEndpoints();
    app.MapDataDistributionEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory in integration tests
public partial class Program;
