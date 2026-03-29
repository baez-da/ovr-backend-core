namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record DtParticImportedEvent(
    string Discipline,
    int Version,
    IReadOnlyList<string> ParticipantIds,
    IReadOnlyList<string> EntryIds,
    IReadOnlyList<string> RemovedParticipantIds,
    IReadOnlyList<string> RemovedEntryIds,
    DateTime ImportedAt) : DomainEventBase;
