namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record UnitResultOfficialEvent(
    string UnitRsc,
    string? WinnerParticipantId,
    string ResultCode,
    string ResultType,
    string? DecisionMark,
    string? StoppageRound,
    string? StoppageTime,
    DateTime ConfirmedAt) : DomainEventBase;
