namespace OVR.Modules.ParticipantRegistry.Contracts;

public interface IParticipantReader
{
    Task<ParticipantSummary?> GetSummaryAsync(string participantId, CancellationToken ct = default);
    Task<IReadOnlyList<ParticipantSummary>> GetSummariesAsync(IReadOnlyList<string> participantIds, CancellationToken ct = default);
}

public sealed record ParticipantSummary(
    string Id, string? GivenName, string FamilyName,
    string PrintName, string TvName, string TvFamilyName,
    string Organisation, string Gender,
    IReadOnlyList<FunctionSummary> Functions,
    string? PhotoUrl = null);

public sealed record FunctionSummary(
    string Function, string Discipline, bool IsMain);
