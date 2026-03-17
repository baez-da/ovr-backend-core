using ErrorOr;
using MediatR;

namespace OVR.Modules.CoachAssignment.Features.AssignCoach;

public sealed record AssignCoachCommand(
    string ParticipantId,
    string EventRsc,
    string Function,
    string Organisation,
    string? AccreditationFunction = null) : IRequest<ErrorOr<AssignCoachResponse>>;

public sealed record AssignCoachResponse(
    string Id,
    string Status,
    DateTime CreatedAt);
