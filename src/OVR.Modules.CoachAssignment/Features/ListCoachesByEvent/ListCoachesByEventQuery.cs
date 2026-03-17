using ErrorOr;
using MediatR;
using OVR.Modules.CoachAssignment.Features.GetCoachAssignment;

namespace OVR.Modules.CoachAssignment.Features.ListCoachesByEvent;

public sealed record ListCoachesByEventQuery(string RscPrefix) : IRequest<ErrorOr<IReadOnlyList<CoachAssignmentResponse>>>;
