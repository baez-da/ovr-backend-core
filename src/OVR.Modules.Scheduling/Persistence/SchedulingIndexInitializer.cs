using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace OVR.Modules.Scheduling.Persistence;

internal sealed class SchedulingIndexInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var collection = database.GetCollection<UnitScheduleDocument>("scheduling_unit_schedules");

        // Unique compound index on (locationCode, startTime).
        // Closes the TOCTOU race between collision detection and insert: a concurrent
        // second insert at the same (location, time) fails with MongoDB error E11000,
        // which the handlers translate to Scheduling.LocationTimeOccupied.
        var locationTimeIndex = new CreateIndexModel<UnitScheduleDocument>(
            Builders<UnitScheduleDocument>.IndexKeys
                .Ascending(d => d.LocationCode)
                .Ascending(d => d.StartTime),
            new CreateIndexOptions
            {
                Name = "ix_locationCode_startTime_unique",
                Unique = true
            });

        // Non-unique secondary index on sessionCode for future "list units in session" queries.
        var sessionIndex = new CreateIndexModel<UnitScheduleDocument>(
            Builders<UnitScheduleDocument>.IndexKeys.Ascending(d => d.SessionCode),
            new CreateIndexOptions { Name = "ix_sessionCode" });

        await collection.Indexes.CreateManyAsync(
            new[] { locationTimeIndex, sessionIndex },
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
