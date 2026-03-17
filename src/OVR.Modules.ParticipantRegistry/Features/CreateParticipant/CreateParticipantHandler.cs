using ErrorOr;
using MediatR;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public sealed class CreateParticipantHandler(
    IParticipantRepository repository,
    IPublisher publisher)
    : IRequestHandler<CreateParticipantCommand, ErrorOr<CreateParticipantResponse>>
{
    public async Task<ErrorOr<CreateParticipantResponse>> Handle(
        CreateParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIdAsync(request.ParticipantId, cancellationToken);
        if (existing is not null)
            return Errors.ParticipantErrors.AlreadyExists(request.ParticipantId);

        var participantId = ParticipantId.Create(request.ParticipantId);
        var type = Enum.Parse<ParticipantType>(request.Type, ignoreCase: true);
        var gender = Gender.FromCode(request.GenderCode);
        var organisation = Organisation.Create(request.Organisation);
        var description = Description.Create(request.GivenName, request.FamilyName, gender, request.BirthDate, organisation);

        var participant = Participant.Create(participantId, type, description);

        await repository.AddAsync(participant, cancellationToken);

        // Dispatch domain events
        foreach (var domainEvent in participant.DomainEvents)
        {
            await publisher.Publish(domainEvent, cancellationToken);
        }
        participant.ClearDomainEvents();

        return new CreateParticipantResponse(
            participant.Id,
            participant.PrintName,
            participant.CreatedAt);
    }
}
