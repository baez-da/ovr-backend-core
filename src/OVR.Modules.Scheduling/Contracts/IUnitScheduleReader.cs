namespace OVR.Modules.Scheduling.Contracts;

public interface IUnitScheduleReader
{
    Task<IReadOnlyList<string>> ListUnitRscs(
        string? sessionCode, string? locationCode, CancellationToken ct);
}
