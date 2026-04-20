using Microsoft.Extensions.Hosting;

namespace OVR.Modules.Progression.Persistence;

public sealed class ProgressionIndexInitializer : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        // EventRsc is the BsonId (_id), so MongoDB guarantees uniqueness automatically.
        // No additional indexes are needed for this collection at this time.
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
