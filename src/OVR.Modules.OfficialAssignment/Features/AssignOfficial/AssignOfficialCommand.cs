using ErrorOr;
using MediatR;

namespace OVR.Modules.OfficialAssignment.Features.AssignOfficial;

public sealed record AssignOfficialCommand(
    string ParticipantId,
    string UnitRsc,
    string Function,
    string Organisation,
    string? AccreditationFunction = null,
    string? TeamId = null) : IRequest<ErrorOr<AssignOfficialResponse>>;

public sealed record AssignOfficialResponse(
    string Id,
    string Status,
    DateTime CreatedAt);
