using OVR.Modules.ParticipantRegistry.Contracts;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Persistence;

namespace OVR.Modules.ParticipantRegistry.Services;

public sealed class ParticipantReaderService(
    IParticipantRepository repository) : IParticipantReader
{
    public async Task<ParticipantSummary?> GetSummaryAsync(
        string participantId, CancellationToken ct = default)
    {
        var participant = await repository.GetByIdAsync(participantId, ct);
        return participant is null ? null : ToSummary(participant);
    }

    public async Task<IReadOnlyList<ParticipantSummary>> GetSummariesAsync(
        IReadOnlyList<string> participantIds, CancellationToken ct = default)
    {
        if (participantIds.Count == 0) return [];

        var participants = await repository.GetByIdsAsync(participantIds, ct);
        return participants.Select(ToSummary).ToList();
    }

    private static ParticipantSummary ToSummary(Participant p) =>
        new(p.Id, p.BiographicData.GivenName, p.BiographicData.FamilyName,
            p.PrintName, p.TvName, p.TvFamilyName,
            p.BiographicData.Organisation.Code,
            p.BiographicData.Gender.Value,
            p.Functions.Select(f => new FunctionSummary(f.Function, f.Discipline, f.IsMain)).ToList(),
            p.PhotoUrl);
}
