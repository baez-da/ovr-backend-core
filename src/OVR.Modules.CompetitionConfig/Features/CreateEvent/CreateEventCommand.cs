using ErrorOr;
using MediatR;

namespace OVR.Modules.CompetitionConfig.Features.CreateEvent;

public sealed record CreateEventCommand(
    string Discipline,
    string Gender,
    string EventCode,
    string? Modifier,
    string Name) : IRequest<ErrorOr<CreateEventResponse>>;

public sealed record CreateEventResponse(
    string Rsc,
    string Name,
    DateTime CreatedAt);
