using ErrorOr;
using MediatR;
using OVR.Modules.OfficialAssignment.Domain;
using OVR.Modules.OfficialAssignment.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.OfficialAssignment.Features.AssignOfficial;

public sealed class AssignOfficialHandler(
    IOfficialAssignmentRepository repository,
    IPublisher publisher)
    : IRequestHandler<AssignOfficialCommand, ErrorOr<AssignOfficialResponse>>
{
    public async Task<ErrorOr<AssignOfficialResponse>> Handle(
        AssignOfficialCommand request,
        CancellationToken cancellationToken)
    {
        var participantId = ParticipantId.Create(request.ParticipantId);
        var unitRsc = Rsc.Create(request.UnitRsc);
        var organisation = Organisation.Create(request.Organisation);
        var teamId = request.TeamId is not null ? TeamId.Create(request.TeamId) : null;

        var assignmentId = $"{participantId.Value}_{unitRsc.Value}";
        var existing = await repository.GetByIdAsync(assignmentId, cancellationToken);
        if (existing is not null)
            return Errors.OfficialAssignmentErrors.AlreadyExists(request.ParticipantId, request.UnitRsc);

        var assignment = OfficialAssignmentEntity.Create(
            participantId, unitRsc, request.Function, organisation,
            request.AccreditationFunction, teamId);
        await repository.AddAsync(assignment, cancellationToken);

        foreach (var domainEvent in assignment.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
        assignment.ClearDomainEvents();

        return new AssignOfficialResponse(assignment.Id, assignment.Status.ToString(), assignment.CreatedAt);
    }
}
