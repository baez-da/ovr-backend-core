using ErrorOr;
using MediatR;
using OVR.Ingestion.Errors;
using OVR.Ingestion.Gms.Mapping;
using OVR.Ingestion.Gms.Parsing;
using OVR.Modules.Entries.Domain;
using OVR.Modules.Entries.Persistence;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.SharedKernel.Domain.Events.Integration;

namespace OVR.Ingestion.Features.ImportDtPartic;

public sealed class ImportDtParticHandler(
    IParticipantRepository participantRepository,
    IEntryRepository entryRepository,
    IPublisher publisher,
    INameBuilder nameBuilder)
    : IRequestHandler<ImportDtParticCommand, ErrorOr<ImportDtParticResponse>>
{
    public async Task<ErrorOr<ImportDtParticResponse>> Handle(
        ImportDtParticCommand request, CancellationToken cancellationToken)
    {
        DtParticDocument document;
        try
        {
            document = DtParticXmlParser.Parse(request.File);
        }
        catch (Exception ex)
        {
            return IngestionErrors.MalformedXml(ex.Message);
        }

        if (document.Participants.Count == 0)
            return IngestionErrors.EmptyParticipantList();

        // Remove previous ODF-imported participants for this discipline
        var previousImport = await RemovePreviousImportAsync(
            document.Discipline, cancellationToken);

        // Process participants
        var warnings = new List<ImportWarning>();
        var participants = new List<Participant>();
        var entries = new List<Entry>();
        var athletes = 0;
        var officials = 0;
        var other = 0;
        var skipped = 0;

        foreach (var parsed in document.Participants.Where(p => p.Current))
        {
            if (string.IsNullOrWhiteSpace(parsed.FamilyName))
            {
                warnings.Add(new ImportWarning(parsed.Code, "Missing FamilyName"));
                skipped++;
                continue;
            }

            if (parsed.Gender is not ("M" or "F" or "X"))
            {
                warnings.Add(new ImportWarning(parsed.Code, $"Invalid Gender: {parsed.Gender}"));
                skipped++;
                continue;
            }

            var (participant, participantEntries) = DtParticMapper.Map(parsed, nameBuilder);
            participants.Add(participant);
            entries.AddRange(participantEntries);

            if (participantEntries.Count > 0) athletes++;
            else if (parsed.MainFunctionId is "JU" or "RE" or "J1" or "J2" or "J3" or "J4" or "J5")
                officials++;
            else
                other++;
        }

        // Persist
        await participantRepository.AddManyAsync(participants, cancellationToken);
        await entryRepository.AddManyAsync(entries, cancellationToken);

        // Publish integration event
        await publisher.Publish(new DtParticImportedEvent(
            document.Discipline,
            document.Version,
            participants.Select(p => p.Id).ToList(),
            entries.Select(e => e.Id).ToList(),
            previousImport.RemovedParticipantIds,
            previousImport.RemovedEntryIds,
            DateTime.UtcNow), cancellationToken);

        return new ImportDtParticResponse(
            document.Discipline,
            document.Version,
            document.FeedFlag,
            new ParticipantSummary(
                document.Participants.Count(p => p.Current),
                participants.Count, athletes, officials, other, skipped),
            new EntrySummary(entries.Count),
            new PreviousImportSummary(
                previousImport.Existed,
                previousImport.RemovedParticipantIds.Count,
                previousImport.RemovedEntryIds.Count),
            warnings);
    }

    private async Task<(bool Existed, IReadOnlyList<string> RemovedParticipantIds, IReadOnlyList<string> RemovedEntryIds)>
        RemovePreviousImportAsync(string discipline, CancellationToken ct)
    {
        var existing = await participantRepository.FindByOdfDisciplineAsync(discipline, ct);
        if (existing.Count == 0)
            return (false, [], []);

        var participantIds = existing.Select(p => p.Id).ToList();

        // Extract discipline prefix for entry filtering (e.g., "BOX" from "BOX-------------------------------")
        var rscPrefix = discipline.TrimEnd('-');

        // Get entry count before deletion for reporting
        var entriesToRemove = new List<string>();
        foreach (var pid in participantIds)
        {
            var participantEntries = await entryRepository.FindByParticipantIdAsync(pid, ct);
            entriesToRemove.AddRange(
                participantEntries
                    .Where(e => e.EventRsc.Value.StartsWith(rscPrefix, StringComparison.Ordinal))
                    .Select(e => e.Id));
        }

        await entryRepository.DeleteByParticipantIdsAsync(participantIds, rscPrefix, ct);
        await participantRepository.DeleteManyAsync(participantIds, ct);

        return (true, participantIds, entriesToRemove);
    }
}
