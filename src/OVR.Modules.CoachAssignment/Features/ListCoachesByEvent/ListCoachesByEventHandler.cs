using ErrorOr;
using MediatR;
using OVR.Modules.CoachAssignment.Features.GetCoachAssignment;
using OVR.Modules.CoachAssignment.Persistence;

namespace OVR.Modules.CoachAssignment.Features.ListCoachesByEvent;

public sealed class ListCoachesByEventHandler(ICoachAssignmentRepository repository)
    : IRequestHandler<ListCoachesByEventQuery, ErrorOr<IReadOnlyList<CoachAssignmentResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<CoachAssignmentResponse>>> Handle(
        ListCoachesByEventQuery request,
        CancellationToken cancellationToken)
    {
        var assignments = await repository.FindByRscPrefixAsync(request.RscPrefix, cancellationToken);

        var responses = assignments.Select(a => new CoachAssignmentResponse(
            a.Id,
            a.ParticipantId.Value,
            a.EventRsc.Value,
            a.Function,
            a.AccreditationFunction,
            a.Organisation.Code,
            a.Status.ToString(),
            a.CreatedAt,
            a.UpdatedAt)).ToList();

        return responses;
    }
}
