namespace OVR.Ingestion.ResultsConcentrator;

public interface IResultsConcentrator
{
    Task<bool> TryAppendAsync(IncomingResult result, CancellationToken ct = default);
    Task<IReadOnlyList<IncomingResult>> ReplayAsync(string unitRsc, CancellationToken ct = default);
}

public sealed record IncomingResult(
    string UnitRsc,
    string Source,
    string RawPayload,
    DateTime ReceivedAt,
    string DeduplicationKey);
