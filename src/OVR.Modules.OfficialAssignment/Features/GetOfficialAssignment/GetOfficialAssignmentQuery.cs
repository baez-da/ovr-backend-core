using ErrorOr;
using MediatR;

namespace OVR.Modules.OfficialAssignment.Features.GetOfficialAssignment;

public sealed record GetOfficialAssignmentQuery(string AssignmentId) : IRequest<ErrorOr<OfficialAssignmentResponse>>;

public sealed record OfficialAssignmentResponse(
    string Id,
    string ParticipantId,
    string UnitRsc,
    string Function,
    string? AccreditationFunction,
    string Organisation,
    string Status,
    string? TeamId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
