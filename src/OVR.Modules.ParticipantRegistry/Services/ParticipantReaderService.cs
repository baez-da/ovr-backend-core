using OVR.Modules.ParticipantRegistry.Contracts;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Persistence;

namespace OVR.Modules.ParticipantRegistry.Services;

public sealed class ParticipantReaderService(
    IParticipantRepository repository,
    ParticipantEnricher enricher) : IParticipantReader
{
    public async Task<ParticipantSummary?> GetSummaryAsync(
        string participantId, string language, CancellationToken ct = default)
    {
        var participant = await repository.GetByIdAsync(participantId, ct);
        if (participant is null) return null;
        return await ToSummaryAsync(participant, language, ct);
    }

    public async Task<IReadOnlyList<ParticipantSummary>> GetSummariesAsync(
        IReadOnlyList<string> participantIds, string language, CancellationToken ct = default)
    {
        if (participantIds.Count == 0) return [];

        var participants = await repository.GetByIdsAsync(participantIds, ct);
        var summaries = new List<ParticipantSummary>(participants.Count);
        foreach (var p in participants)
            summaries.Add(await ToSummaryAsync(p, language, ct));
        return summaries;
    }

    private async Task<ParticipantSummary> ToSummaryAsync(
        Participant p, string language, CancellationToken ct)
    {
        var org = await enricher.EnrichCodeAsync(
            CommonCodes.Contracts.CommonCodeTypes.Organisation, p.BiographicData.Organisation.Code, language, ct);
        var gender = await enricher.EnrichCodeAsync(
            CommonCodes.Contracts.CommonCodeTypes.PersonGender, p.BiographicData.Gender.Value, language, ct);
        var functionResponses = await enricher.EnrichFunctionsAsync(p.Functions, language, ct);

        var functions = functionResponses
            .Select(f => new FunctionSummary(f.Function, f.Discipline, f.IsMain))
            .ToList();

        return new ParticipantSummary(
            p.Id, p.BiographicData.GivenName, p.BiographicData.FamilyName,
            p.PrintName, p.TvName, p.TvFamilyName,
            org, gender, functions, p.PhotoUrl);
    }
}
