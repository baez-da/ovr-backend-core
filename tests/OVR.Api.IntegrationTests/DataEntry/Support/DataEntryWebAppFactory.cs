using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using OVR.Modules.CommonCodes.Persistence;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.Modules.Entries.Persistence;
using Testcontainers.MongoDb;

namespace OVR.Api.IntegrationTests.DataEntry.Support;

public sealed class DataEntryWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:8.0")
        .Build();

    private const string DatabaseName = "ovr-test";

    public IMongoDatabase Database { get; private set; } = null!;

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
    }

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
                Id = "DISCIPLINE:BOX",
                Type = "DISCIPLINE",
                Code = "BOX",
                Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Boxing" } },
                Attributes = []
            },
        };

        await collection.InsertManyAsync(codes);
    }

    public async Task SeedFirstRoundBracketAsync(
        string eventRsc, string unitRsc, int seedA, int seedB)
    {
        var collection = Database.GetCollection<UnitDocument>("competitionconfig_units");

        var unit = new UnitDocument
        {
            Id = unitRsc,
            EventRsc = eventRsc,
            PhaseCode = "FNL-",
            UnitNumber = 1,
            SeedA = seedA,
            SeedB = seedB,
            CreatedAt = DateTime.UtcNow
        };

        await collection.InsertOneAsync(unit);
    }

    public async Task SeedEntriesAsync(
        string eventRsc,
        (string participantId, string organisation, int seed)[] entries)
    {
        var collection = Database.GetCollection<EntryDocument>("entries_entries");

        var documents = entries.Select(e => new EntryDocument
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
        }).ToList();

        await collection.InsertManyAsync(documents);
    }
}
