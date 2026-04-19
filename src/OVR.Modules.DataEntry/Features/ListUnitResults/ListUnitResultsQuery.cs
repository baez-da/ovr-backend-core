using ErrorOr;
using MediatR;
using OVR.Modules.DataEntry.Features.GetUnitResult;

namespace OVR.Modules.DataEntry.Features.ListUnitResults;

public sealed record ListUnitResultsQuery(
    string? SessionCode,
    string? LocationCode,
    string? Status) : IRequest<ErrorOr<IReadOnlyList<UnitResultResponse>>>;
