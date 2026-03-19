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
using OVR.Modules.CommonCodes;
using OVR.SharedKernel.Behaviors;
using OVR.SharedKernel.I18n;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

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
            typeof(DataDistributionModule).Assembly,
            typeof(CommonCodesModule).Assembly);

        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblies([
        typeof(ParticipantRegistryModule).Assembly,
        typeof(EntriesModule).Assembly,
        typeof(OfficialAssignmentModule).Assembly,
        typeof(CoachAssignmentModule).Assembly,
        typeof(CommonCodesModule).Assembly
    ]);

    // i18n — global primero, luego módulos (auto-discovery de I18n.* en output dir)
    builder.Services.AddSingleton<ITranslationService>(sp =>
    {
        var env = sp.GetRequiredService<IWebHostEnvironment>();
        var logger = sp.GetRequiredService<ILogger<JsonTranslationService>>();

        var globalPath = Path.Combine(env.ContentRootPath, "I18n");
        var modulePaths = Directory.Exists(AppContext.BaseDirectory)
            ? Directory.GetDirectories(AppContext.BaseDirectory, "I18n.*")
            : [];

        return new JsonTranslationService([globalPath, .. modulePaths], logger);
    });

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
    builder.Services.AddCommonCodesModule();

    var app = builder.Build();

    // Middleware
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    app.MapOpenApi();

    if (builder.Configuration.GetValue("OpenApi:EnableUi", false))
    {
        app.MapScalarApiReference(options =>
        {
            options.Title = "OVR Backend Core API";
            options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });
    }

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
    app.MapCommonCodesEndpoints();

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
