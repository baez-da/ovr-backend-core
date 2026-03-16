namespace OVR.Ingestion.Gms;

public interface IGmsClient
{
    Task<GmsDownloadResult> PerformInitialDownloadAsync(CancellationToken ct = default);
}

public sealed record GmsDownloadResult(
    int ParticipantsImported,
    int EventsImported,
    int TeamsImported,
    DateTime DownloadedAt);
