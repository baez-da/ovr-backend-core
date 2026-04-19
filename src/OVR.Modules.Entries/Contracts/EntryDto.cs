using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Entries.Contracts;

public sealed record EntryDto(
    ParticipantId ParticipantId,
    int? Seed,
    Organisation Organisation);
