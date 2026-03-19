namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record CommonCodesReimportedEvent(
    string Type,
    string Strategy,
    IReadOnlyList<string> UpdatedCodes) : DomainEventBase;
