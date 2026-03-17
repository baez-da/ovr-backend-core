using ErrorOr;
using MediatR;
using OVR.Modules.Entries.Domain;
using OVR.Modules.Entries.Persistence;

namespace OVR.Modules.Entries.Features.ChangeEntryStatus;

public sealed class ChangeEntryStatusHandler(
    IEntryRepository repository,
    IPublisher publisher)
    : IRequestHandler<ChangeEntryStatusCommand, ErrorOr<ChangeEntryStatusResponse>>
{
    public async Task<ErrorOr<ChangeEntryStatusResponse>> Handle(
        ChangeEntryStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await repository.GetByIdAsync(request.EntryId, cancellationToken);
        if (entry is null)
            return Errors.EntryErrors.NotFound(request.EntryId);

        var oldStatus = entry.Status.ToString();
        var newStatus = Enum.Parse<EntryStatus>(request.NewStatus, ignoreCase: true);

        try
        {
            entry.ChangeStatus(newStatus);
        }
        catch (InvalidOperationException)
        {
            return Errors.EntryErrors.InvalidStatusTransition(oldStatus, request.NewStatus);
        }

        await repository.UpdateAsync(entry, cancellationToken);

        foreach (var domainEvent in entry.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
        entry.ClearDomainEvents();

        return new ChangeEntryStatusResponse(entry.Id, oldStatus, entry.Status.ToString());
    }
}
