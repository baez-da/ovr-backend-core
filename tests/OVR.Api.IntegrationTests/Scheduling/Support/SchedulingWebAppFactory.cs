using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using OVR.Modules.CommonCodes.Persistence;
using Testcontainers.MongoDb;

namespace OVR.Api.IntegrationTests.Scheduling.Support;

public sealed class SchedulingWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:8")
        .Build();

    private const string DatabaseName = "ovr_scheduling_tests";

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
            new() { Id = "VENUES:ABC", Type = "VENUES", Code = "ABC", Order = 1,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Arena Boxing Center" } }, Attributes = [] },
            new() { Id = "VENUES:DEF", Type = "VENUES", Code = "DEF", Order = 2,
                Name = new() { ["eng"] = new LocalizedTextDocument { Long = "Secondary Arena" } }, Attributes = [] },
        };

        await collection.InsertManyAsync(seed);
    }
}
