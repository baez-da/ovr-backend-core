using OVR.SharedKernel.Domain.Events;

namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record OfficialAssignedEvent(
    string ParticipantId,
    string UnitRsc,
    string Function) : DomainEventBase;
