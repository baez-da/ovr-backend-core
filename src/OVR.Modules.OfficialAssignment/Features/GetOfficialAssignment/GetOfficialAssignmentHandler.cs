using ErrorOr;
using MediatR;
using OVR.Modules.OfficialAssignment.Persistence;

namespace OVR.Modules.OfficialAssignment.Features.GetOfficialAssignment;

public sealed class GetOfficialAssignmentHandler(IOfficialAssignmentRepository repository)
    : IRequestHandler<GetOfficialAssignmentQuery, ErrorOr<OfficialAssignmentResponse>>
{
    public async Task<ErrorOr<OfficialAssignmentResponse>> Handle(
        GetOfficialAssignmentQuery request,
        CancellationToken cancellationToken)
    {
        var assignment = await repository.GetByIdAsync(request.AssignmentId, cancellationToken);
        if (assignment is null)
            return Errors.OfficialAssignmentErrors.NotFound(request.AssignmentId);

        return new OfficialAssignmentResponse(
            assignment.Id,
            assignment.ParticipantId.Value,
            assignment.UnitRsc.Value,
            assignment.Function,
            assignment.AccreditationFunction,
            assignment.Organisation.Code,
            assignment.Status.ToString(),
            assignment.TeamId?.Value,
            assignment.CreatedAt,
            assignment.UpdatedAt);
    }
}
