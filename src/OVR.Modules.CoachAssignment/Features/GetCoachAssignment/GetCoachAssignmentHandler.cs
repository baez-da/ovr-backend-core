using ErrorOr;
using MediatR;
using OVR.Modules.CoachAssignment.Persistence;

namespace OVR.Modules.CoachAssignment.Features.GetCoachAssignment;

public sealed class GetCoachAssignmentHandler(ICoachAssignmentRepository repository)
    : IRequestHandler<GetCoachAssignmentQuery, ErrorOr<CoachAssignmentResponse>>
{
    public async Task<ErrorOr<CoachAssignmentResponse>> Handle(
        GetCoachAssignmentQuery request,
        CancellationToken cancellationToken)
    {
        var assignment = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (assignment is null)
            return Errors.CoachAssignmentErrors.NotFound(request.Id);

        return new CoachAssignmentResponse(
            assignment.Id,
            assignment.ParticipantId.Value,
            assignment.EventRsc.Value,
            assignment.Function,
            assignment.AccreditationFunction,
            assignment.Organisation.Code,
            assignment.Status.ToString(),
            assignment.CreatedAt,
            assignment.UpdatedAt);
    }
}
