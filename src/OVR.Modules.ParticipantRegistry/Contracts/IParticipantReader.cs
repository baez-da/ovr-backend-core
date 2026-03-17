namespace OVR.Modules.ParticipantRegistry.Contracts;

public interface IParticipantReader
{
    Task<ParticipantSummary?> GetSummaryAsync(string participantId, CancellationToken ct = default);
}

public sealed record ParticipantSummary(
    string Id,
    string PrintName,
    string Organisation,
    string Type);
