using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OVR.Modules.CommonCodes.Persistence;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.Entries.Persistence;
using OVR.Modules.Progression.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;
using Testcontainers.MongoDb;

namespace OVR.Api.IntegrationTests.Progression.Support;

/// <summary>
/// Single shared factory for all progression integration tests.
/// Starts a MongoDB container, seeds common codes, and registers
/// event-capture sink handlers so tests can assert on published events.
/// </summary>
public sealed class ProgressionWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .Build();

    private const string DatabaseName = "ovr-progression-tests";

    public IMongoDatabase Database { get; private set; } = null!;

    /// <summary>
    /// Singleton bag shared by all sink handlers.
    /// Tests call Reset() at the top of each test method to clear state.
    /// </summary>
    public CapturedEvents Events { get; } = new();

    // ---------------------------------------------------------------
    // Lifecycle
    // ---------------------------------------------------------------

    public async Task InitializeAsync()
    {
        await _mongo.StartAsync();
        var client = new MongoClient(_mongo.GetConnectionString());
        Database = client.GetDatabase(DatabaseName);
        await SeedCommonCodesAsync();
    }

    public new async Task DisposeAsync()
    {
        await _mongo.DisposeAsync();
        await base.DisposeAsync();
    }

    // ---------------------------------------------------------------
    // WebApplicationFactory override
    // ---------------------------------------------------------------

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = _mongo.GetConnectionString(),
                ["MongoDb:DatabaseName"] = DatabaseName
            });
        });

        builder.ConfigureServices(services =>
        {
            // Register the shared captured-events bag as a singleton so the
            // sink handlers (which MediatR resolves from DI) all share the same instance.
            services.AddSingleton(Events);

            // Register sink handlers — they each capture one event type.
            services.AddSingleton<INotificationHandler<CompetitorAdvancedEvent>, CompetitorAdvancedSink>();
            services.AddSingleton<INotificationHandler<ProgressionSkippedEvent>, ProgressionSkippedSink>();
            services.AddSingleton<INotificationHandler<EventProgressionCompletedEvent>, EventProgressionCompletedSink>();
            services.AddSingleton<INotificationHandler<UnitResultOfficialEvent>, UnitResultOfficialSink>();
            services.AddSingleton<INotificationHandler<UnitResultStartListCreatedEvent>, UnitResultStartListCreatedSink>();
        });
    }

    // ---------------------------------------------------------------
    // Seeding helpers
    // ---------------------------------------------------------------

    private async Task SeedCommonCodesAsync()
    {
        var collection = Database.GetCollection<CommonCodeDocument>("commonCodes_codes");

        var codes = new List<CommonCodeDocument>
        {
            new()
            {
                Id = "ORGANISATIONS:ESP",
                Type = "ORGANISATIONS",
                Code = "ESP",
                Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Spain" } },
                Attributes = []
            },
            new()
            {
                Id = "ORGANISATIONS:POL",
                Type = "ORGANISATIONS",
                Code = "POL",
                Order = 2,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Poland" } },
                Attributes = []
            },
            new()
            {
                Id = "ORGANISATIONS:GBR",
                Type = "ORGANISATIONS",
                Code = "GBR",
                Order = 3,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Great Britain" } },
                Attributes = []
            },
            new()
            {
                Id = "ORGANISATIONS:FRA",
                Type = "ORGANISATIONS",
                Code = "FRA",
                Order = 4,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "France" } },
                Attributes = []
            },
            new()
            {
                Id = "ORGANISATIONS:ITA",
                Type = "ORGANISATIONS",
                Code = "ITA",
                Order = 5,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Italy" } },
                Attributes = []
            },
            new()
            {
                Id = "ORGANISATIONS:USA",
                Type = "ORGANISATIONS",
                Code = "USA",
                Order = 6,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "United States" } },
                Attributes = []
            },
            new()
            {
                Id = "DISCIPLINE:BOX",
                Type = "DISCIPLINE",
                Code = "BOX",
                Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Boxing" } },
                Attributes = []
            },
            new()
            {
                Id = "VENUES:AXC",
                Type = "VENUES",
                Code = "AXC",
                Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Arena Boxing Center" } },
                Attributes = []
            },
            // Event codes used by each test class (unique per class to avoid shared state)
            new()
            {
                Id = "EVENT:H4KG",
                Type = "EVENT",
                Code = "H4KG",
                Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Happy Path 4kg" } },
                Attributes = []
            },
            new()
            {
                Id = "EVENT:B4KG",
                Type = "EVENT",
                Code = "B4KG",
                Order = 2,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Buffering 4kg" } },
                Attributes = []
            },
            new()
            {
                Id = "EVENT:D4KG",
                Type = "EVENT",
                Code = "D4KG",
                Order = 3,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "DKO 4kg" } },
                Attributes = []
            },
            new()
            {
                Id = "EVENT:Y6KG",
                Type = "EVENT",
                Code = "Y6KG",
                Order = 4,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Bye 6kg" } },
                Attributes = []
            },
            new()
            {
                Id = "EVENT:I4KG",
                Type = "EVENT",
                Code = "I4KG",
                Order = 5,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Idempotency 4kg" } },
                Attributes = []
            },
        };

        await collection.InsertManyAsync(codes);
    }

    // ---------------------------------------------------------------
    // Domain data helpers (seeded directly into Mongo)
    // ---------------------------------------------------------------

    /// <summary>Seeds Unit documents so DataEntry can resolve seeds for scheduling events.</summary>
    public async Task SeedUnitsAsync(string eventRsc, IEnumerable<UnitDocument> units)
    {
        var collection = Database.GetCollection<UnitDocument>("competitionconfig_units");
        await collection.InsertManyAsync(units);
    }

    /// <summary>Seeds Entry documents so DataEntry can build lineups from seeds.</summary>
    public async Task SeedEntriesAsync(
        string eventRsc,
        IEnumerable<(string participantId, string organisation, int seed)> entries)
    {
        var collection = Database.GetCollection<EntryDocument>("entries_entries");
        var docs = entries.Select(e => new EntryDocument
        {
            Id = $"{e.participantId}_{eventRsc}",
            ParticipantId = e.participantId,
            EventRsc = eventRsc,
            CompetitorType = "Athlete",
            Organisation = e.organisation,
            Status = "Active",
            InscriptionStatus = "Confirmed",
            Seed = e.seed.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await collection.InsertManyAsync(docs);
    }

    /// <summary>Reads a UnitResult document directly from Mongo (bypasses API layer).</summary>
    public async Task<UnitResultDocument?> GetUnitResultAsync(string unitRsc)
    {
        var collection = Database.GetCollection<UnitResultDocument>("unitResults");
        return await collection.Find(d => d.Id == unitRsc).FirstOrDefaultAsync();
    }

    /// <summary>Reads a BracketProgression document directly from Mongo.</summary>
    public async Task<BracketProgressionDocument?> GetBracketProgressionAsync(string eventRsc)
    {
        var collection = Database.GetCollection<BracketProgressionDocument>(
            MongoBracketProgressionRepository.CollectionName);
        return await collection.Find(d => d.EventRsc == eventRsc).FirstOrDefaultAsync();
    }
}
