using ErrorOr;
using MediatR;

namespace OVR.Modules.Entries.Features.CreateEntry;

public sealed record CreateEntryCommand(
    string ParticipantId,
    string EventRsc,
    string CompetitorType,
    string Organisation,
    string? Category = null,
    string? TeamId = null) : IRequest<ErrorOr<CreateEntryResponse>>;

public sealed record CreateEntryResponse(
    string Id,
    string Status,
    string InscriptionStatus,
    DateTime CreatedAt);
