using ErrorOr;
using MediatR;
using OVR.Modules.OfficialAssignment.Features.GetOfficialAssignment;
using OVR.Modules.OfficialAssignment.Persistence;

namespace OVR.Modules.OfficialAssignment.Features.ListOfficialsByUnit;

public sealed class ListOfficialsByUnitHandler(IOfficialAssignmentRepository repository)
    : IRequestHandler<ListOfficialsByUnitQuery, ErrorOr<IReadOnlyList<OfficialAssignmentResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<OfficialAssignmentResponse>>> Handle(
        ListOfficialsByUnitQuery request,
        CancellationToken cancellationToken)
    {
        var assignments = await repository.FindByRscPrefixAsync(request.RscPrefix, cancellationToken);

        var responses = assignments.Select(a => new OfficialAssignmentResponse(
            a.Id,
            a.ParticipantId.Value,
            a.UnitRsc.Value,
            a.Function,
            a.AccreditationFunction,
            a.Organisation.Code,
            a.Status.ToString(),
            a.TeamId?.Value,
            a.CreatedAt,
            a.UpdatedAt)).ToList();

        return responses;
    }
}
