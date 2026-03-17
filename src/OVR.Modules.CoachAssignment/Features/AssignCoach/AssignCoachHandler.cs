using ErrorOr;
using MediatR;
using OVR.Modules.CoachAssignment.Domain;
using OVR.Modules.CoachAssignment.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CoachAssignment.Features.AssignCoach;

public sealed class AssignCoachHandler(
    ICoachAssignmentRepository repository,
    IPublisher publisher)
    : IRequestHandler<AssignCoachCommand, ErrorOr<AssignCoachResponse>>
{
    public async Task<ErrorOr<AssignCoachResponse>> Handle(
        AssignCoachCommand request,
        CancellationToken cancellationToken)
    {
        var participantId = ParticipantId.Create(request.ParticipantId);
        var eventRsc = Rsc.Create(request.EventRsc);
        var organisation = Organisation.Create(request.Organisation);

        var assignmentId = $"{participantId.Value}_{eventRsc.Value}";
        var existing = await repository.GetByIdAsync(assignmentId, cancellationToken);
        if (existing is not null)
            return Errors.CoachAssignmentErrors.AlreadyExists(request.ParticipantId, request.EventRsc);

        var assignment = CoachAssignmentEntity.Create(
            participantId, eventRsc, request.Function, organisation, request.AccreditationFunction);
        await repository.AddAsync(assignment, cancellationToken);

        foreach (var domainEvent in assignment.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
        assignment.ClearDomainEvents();

        return new AssignCoachResponse(assignment.Id, assignment.Status.ToString(), assignment.CreatedAt);
    }
}
