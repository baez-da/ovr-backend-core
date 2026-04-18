using ErrorOr;
using MediatR;

namespace OVR.Modules.Scheduling.Features.CreateSession;

public sealed record CreateSessionCommand(
    string Code,
    string VenueCode,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    TimeSpan? Leadin) : IRequest<ErrorOr<CreateSessionResponse>>;

public sealed record CreateSessionResponse(
    string Code,
    string VenueCode,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    TimeSpan? Leadin,
    DateTime CreatedAt);
