using ErrorOr;
using MediatR;
using OVR.Modules.Entries.Domain;
using OVR.Modules.Entries.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Entries.Features.CreateEntry;

public sealed class CreateEntryHandler(
    IEntryRepository repository,
    IPublisher publisher)
    : IRequestHandler<CreateEntryCommand, ErrorOr<CreateEntryResponse>>
{
    public async Task<ErrorOr<CreateEntryResponse>> Handle(
        CreateEntryCommand request,
        CancellationToken cancellationToken)
    {
        var participantId = ParticipantId.Create(request.ParticipantId);
        var eventRsc = Rsc.Create(request.EventRsc);
        var competitorType = Enum.Parse<CompetitorType>(request.CompetitorType, ignoreCase: true);
        var organisation = Organisation.Create(request.Organisation);
        var teamId = request.TeamId is not null ? TeamId.Create(request.TeamId) : null;

        var entryId = $"{participantId.Value}_{eventRsc.Value}";
        var existing = await repository.GetByIdAsync(entryId, cancellationToken);
        if (existing is not null)
            return Errors.EntryErrors.AlreadyExists(request.ParticipantId, request.EventRsc);

        var entry = Entry.Create(participantId, eventRsc, competitorType, organisation, request.Category, teamId);
        await repository.AddAsync(entry, cancellationToken);

        foreach (var domainEvent in entry.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
        entry.ClearDomainEvents();

        return new CreateEntryResponse(entry.Id, entry.Status.ToString(), entry.InscriptionStatus.ToString(), entry.CreatedAt);
    }
}
