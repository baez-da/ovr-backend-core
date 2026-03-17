using ErrorOr;
using MediatR;

namespace OVR.Modules.CoachAssignment.Features.GetCoachAssignment;

public sealed record GetCoachAssignmentQuery(string Id) : IRequest<ErrorOr<CoachAssignmentResponse>>;

public sealed record CoachAssignmentResponse(
    string Id,
    string ParticipantId,
    string EventRsc,
    string Function,
    string? AccreditationFunction,
    string Organisation,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
