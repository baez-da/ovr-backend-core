namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record EventStructureGeneratedEvent(
    string EventRsc,
    string Format,
    int Size,
    IReadOnlyList<PhaseInfo> Phases,
    IReadOnlyList<string> UnitRscs,
    DateTime GeneratedAt) : DomainEventBase;

public sealed record PhaseInfo(string Code, int Order, int UnitCount);
