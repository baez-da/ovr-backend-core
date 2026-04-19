using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.ConfirmUnitResult;

public sealed record ConfirmUnitResultCommand(string UnitRsc)
    : IRequest<ErrorOr<Success>>;
