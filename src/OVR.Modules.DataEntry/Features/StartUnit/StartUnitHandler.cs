using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;

namespace OVR.Modules.DataEntry.Features.StartUnit;

public sealed class StartUnitHandler
    : IRequestHandler<StartUnitCommand, ErrorOr<Success>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IPublisher _publisher;

    public StartUnitHandler(IUnitResultRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<ErrorOr<Success>> Handle(
        StartUnitCommand request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);

        var result = ur.Start();
        if (result.IsError) return result.Errors;

        await _repository.UpdateAsync(ur, ct);

        foreach (var e in ur.DomainEvents) await _publisher.Publish(e, ct);
        ur.ClearDomainEvents();

        return Result.Success;
    }
}
