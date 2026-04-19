using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Errors;
using OVR.Modules.DataEntry.Persistence;

namespace OVR.Modules.DataEntry.Features.ScorePeriod;

public sealed class ScorePeriodHandler
    : IRequestHandler<ScorePeriodCommand, ErrorOr<Success>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IPublisher _publisher;

    public ScorePeriodHandler(IUnitResultRepository repository, IPublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }

    public async Task<ErrorOr<Success>> Handle(
        ScorePeriodCommand request, CancellationToken ct)
    {
        var ur = await _repository.GetAsync(request.UnitRsc, ct);
        if (ur is null) return DataEntryErrors.UnitResultNotFound(request.UnitRsc);

        var cards = request.Scorecards.Select(c => new PeriodScorecard(
            Enum.Parse<JudgePosition>(c.JudgePos),
            c.HomeScore, c.AwayScore)).ToList();

        var result = ur.ScorePeriod(request.PeriodCode, cards);
        if (result.IsError) return result.Errors;

        await _repository.UpdateAsync(ur, ct);
        foreach (var e in ur.DomainEvents) await _publisher.Publish(e, ct);
        ur.ClearDomainEvents();

        return Result.Success;
    }
}
