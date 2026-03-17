using ErrorOr;
using MediatR;
using OVR.Modules.Entries.Domain;
using OVR.Modules.Entries.Persistence;

namespace OVR.Modules.Entries.Features.WithdrawEntry;

public sealed class WithdrawEntryHandler(
    IEntryRepository repository,
    IPublisher publisher)
    : IRequestHandler<WithdrawEntryCommand, ErrorOr<WithdrawEntryResponse>>
{
    public async Task<ErrorOr<WithdrawEntryResponse>> Handle(
        WithdrawEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await repository.GetByIdAsync(request.EntryId, cancellationToken);
        if (entry is null)
            return Errors.EntryErrors.NotFound(request.EntryId);

        try
        {
            entry.ChangeStatus(EntryStatus.Withdrawn);
        }
        catch (InvalidOperationException)
        {
            return Errors.EntryErrors.InvalidStatusTransition(entry.Status.ToString(), "Withdrawn");
        }

        await repository.UpdateAsync(entry, cancellationToken);

        foreach (var domainEvent in entry.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
        entry.ClearDomainEvents();

        return new WithdrawEntryResponse(entry.Id, entry.Status.ToString());
    }
}
