namespace OVR.Ingestion.TimingScoring;

public interface ITimingScoringListener
{
    Task StartListeningAsync(CancellationToken ct = default);
    Task StopListeningAsync(CancellationToken ct = default);
}
