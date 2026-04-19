using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.Features.GetUnitResult;
using OVR.Modules.DataEntry.Persistence;
using OVR.Modules.Scheduling.Contracts;

namespace OVR.Modules.DataEntry.Features.ListUnitResults;

public sealed class ListUnitResultsHandler
    : IRequestHandler<ListUnitResultsQuery, ErrorOr<IReadOnlyList<UnitResultResponse>>>
{
    private readonly IUnitResultRepository _repository;
    private readonly IUnitScheduleReader _scheduleReader;

    public ListUnitResultsHandler(
        IUnitResultRepository repository, IUnitScheduleReader scheduleReader)
    {
        _repository = repository;
        _scheduleReader = scheduleReader;
    }

    public async Task<ErrorOr<IReadOnlyList<UnitResultResponse>>> Handle(
        ListUnitResultsQuery request, CancellationToken ct)
    {
        IReadOnlyList<UnitResult> results;

        if (request.SessionCode is null && request.LocationCode is null)
        {
            results = await _repository.ListAllAsync(ct);
        }
        else
        {
            var rscs = await _scheduleReader.ListUnitRscs(
                request.SessionCode, request.LocationCode, ct);
            results = await _repository.GetManyAsync(rscs, ct);
        }

        if (request.Status is not null)
        {
            var status = Enum.Parse<ResultStatus>(request.Status);
            results = results.Where(r => r.Status == status).ToList();
        }

        var responses = results.Select(GetUnitResultHandler.Map).ToList();
        return responses;
    }
}
