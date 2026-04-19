using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Features.FinishByStoppage;

public sealed class FinishByStoppageHandler
    : IRequestHandler<FinishByStoppageCommand, ErrorOr<Success>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IPublisher _publisher;

    public FinishByStoppageHandler(IUnitResultRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<ErrorOr<Success>> Handle(
        FinishByStoppageCommand request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);

        var code = Enum.Parse<ResultCode>(request.ResultCode);
        ParticipantId? winner = request.WinnerParticipantId is null
            ? null : ParticipantId.Create(request.WinnerParticipantId);

        var result = ur.FinishByStoppage(
            code, request.StoppageRound, request.StoppageTime, winner);
        if (result.IsError) return result.Errors;

        await _repository.UpdateAsync(ur, ct);
        foreach (var e in ur.DomainEvents) await _publisher.Publish(e, ct);
        ur.ClearDomainEvents();

        return Result.Success;
    }
}
