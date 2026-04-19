using ErrorOr;
using MediatR;

namespace OVR.Modules.DataEntry.Features.FinishByStoppage;

public sealed record FinishByStoppageCommand(
    string UnitRsc,
    string ResultCode,
    string StoppageRound,
    string StoppageTime,
    string? WinnerParticipantId) : IRequest<ErrorOr<Success>>;
