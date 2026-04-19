using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Domain;

public sealed record Decision(
    ResultType Type,
    ResultCode Code,
    string? DecisionMark,
    string? StoppageRound,
    string? StoppageTime,
    ParticipantId? WinnerParticipantId);
