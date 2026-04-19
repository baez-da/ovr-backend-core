using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.StartUnit;

public sealed record StartUnitCommand(string UnitRsc) : IRequest<ErrorOr<Success>>;
