namespace OVR.Modules.ParticipantRegistry.Contracts;

public interface IParticipantReader
{
    Task<ParticipantSummary?> GetSummaryAsync(string participantId, CancellationToken ct = default);
}

public sealed record ParticipantSummary(
    string Id, string? GivenName, string FamilyName,
    string PrintName, string TvName, string TvFamilyName,
    string OrganisationCode, string GenderCode,
    IReadOnlyList<ParticipantFunctionSummary> Functions,
    string? PhotoUrl = null);

public sealed record ParticipantFunctionSummary(string FunctionId, string DisciplineCode, bool IsMain);
