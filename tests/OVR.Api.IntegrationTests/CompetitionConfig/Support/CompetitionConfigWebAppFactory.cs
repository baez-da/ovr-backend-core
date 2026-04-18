using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using OVR.Modules.CommonCodes.Persistence;
using Testcontainers.MongoDb;

namespace OVR.Api.IntegrationTests.CompetitionConfig.Support;

public sealed class CompetitionConfigWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:8")
        .Build();

    private const string DatabaseName = "ovr_integration_tests";

    public string ConnectionString => _mongo.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _mongo.StartAsync();
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
        var client = new MongoClient(_mongo.GetConnectionString());
        var db = client.GetDatabase(DatabaseName);
        var collection = db.GetCollection<CommonCodeDocument>("commonCodes_codes");

        var seed = new List<CommonCodeDocument>
        {
            new() { Id = "DISCIPLINE:BOX", Type = "DISCIPLINE", Code = "BOX", Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Boxing" } }, Attributes = [] },
            new() { Id = "EVENT:57KG", Type = "EVENT", Code = "57KG", Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Men's 57kg" } }, Attributes = [] },
            new() { Id = "EVENT:60KG", Type = "EVENT", Code = "60KG", Order = 2,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Men's 60kg" } }, Attributes = [] },
            new() { Id = "EVENT:63KG", Type = "EVENT", Code = "63KG", Order = 3,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Men's 63kg" } }, Attributes = [] },
            new() { Id = "EVENT:66KG", Type = "EVENT", Code = "66KG", Order = 4,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Men's 66kg" } }, Attributes = [] },
        };

        await collection.InsertManyAsync(seed);
    }
}
