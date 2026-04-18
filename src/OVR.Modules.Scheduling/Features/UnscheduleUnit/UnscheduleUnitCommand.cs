using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.UnscheduleUnit;

public sealed record UnscheduleUnitCommand(string UnitRsc)
    : IRequest<ErrorOr<Success>>;
