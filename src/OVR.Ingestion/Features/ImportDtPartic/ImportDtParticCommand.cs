using ErrorOr;
using MediatR;

namespace OVR.Ingestion.Features.ImportDtPartic;

public sealed record ImportDtParticCommand(Stream File) : IRequest<ErrorOr<ImportDtParticResponse>>;

public sealed record ImportDtParticResponse(
    string Discipline,
    int Version,
    string FeedFlag,
    ParticipantSummary Participants,
    EntrySummary Entries,
    PreviousImportSummary PreviousImport,
    IReadOnlyList<ImportWarning> Warnings);

public sealed record ParticipantSummary(
    int Total,
    int Created,
    int Athletes,
    int Officials,
    int Other,
    int Skipped);

public sealed record EntrySummary(int Created);

public sealed record PreviousImportSummary(
    bool Existed,
    int ParticipantsRemoved,
    int EntriesRemoved);

public sealed record ImportWarning(string Code, string Reason);
