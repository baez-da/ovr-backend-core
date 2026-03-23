using OVR.SharedKernel.Contracts;

namespace OVR.Modules.ParticipantRegistry.Contracts;

public interface IParticipantReader
{
    Task<ParticipantSummary?> GetSummaryAsync(string participantId, string language, CancellationToken ct = default);
    Task<IReadOnlyList<ParticipantSummary>> GetSummariesAsync(IReadOnlyList<string> participantIds, string language, CancellationToken ct = default);
}

public sealed record ParticipantSummary(
    string Id, string? GivenName, string FamilyName,
    string PrintName, string TvName, string TvFamilyName,
    LocalizedCode Organisation, LocalizedCode Gender,
    IReadOnlyList<FunctionSummary> Functions,
    string? PhotoUrl = null);

public sealed record FunctionSummary(
    LocalizedCode Function, LocalizedCode Discipline, bool IsMain);
