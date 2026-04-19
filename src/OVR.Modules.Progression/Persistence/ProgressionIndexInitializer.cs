using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace OVR.Modules.Progression.Persistence;

public sealed class ProgressionIndexInitializer(IMongoDatabase db) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var collection = db.GetCollection<BracketProgressionDocument>(
            MongoBracketProgressionRepository.CollectionName);

        var eventRscIdx = new CreateIndexModel<BracketProgressionDocument>(
            Builders<BracketProgressionDocument>.IndexKeys.Ascending(d => d.EventRsc),
            new CreateIndexOptions { Unique = true, Name = "eventRsc_unique" });

        await collection.Indexes.CreateOneAsync(eventRscIdx, cancellationToken: ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
