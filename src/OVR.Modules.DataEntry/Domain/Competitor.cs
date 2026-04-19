using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Domain;

public sealed record Competitor(
    int SortOrder,
    ParticipantId? ParticipantId,
    string? NocompDetail,
    int? Seed,
    Organisation Organisation,
    Wlt? Wlt);
