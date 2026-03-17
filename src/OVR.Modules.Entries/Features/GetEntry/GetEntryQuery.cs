using ErrorOr;
using MediatR;

namespace OVR.Modules.Entries.Features.GetEntry;

public sealed record GetEntryQuery(string EntryId) : IRequest<ErrorOr<EntryResponse>>;

public sealed record EntryResponse(
    string Id,
    string ParticipantId,
    string EventRsc,
    string CompetitorType,
    string Organisation,
    string Status,
    string InscriptionStatus,
    string? RegisteredEventRsc,
    string? Category,
    string? TeamId,
    string? Seed,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
