using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace OVR.Modules.DataEntry.Persistence;

public sealed class DataEntryIndexInitializer : IHostedService
{
    private readonly IMongoDatabase _database;

    public DataEntryIndexInitializer(IMongoDatabase database)
    {
        _database = database;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = _database.GetCollection<UnitResultDocument>("unitResults");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
