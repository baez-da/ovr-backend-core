namespace OVR.Modules.Entries.Contracts;

public interface IEntryReader
{
    Task<IReadOnlyList<EntryDto>> ListActiveByEventRsc(string eventRsc, CancellationToken ct);
}
