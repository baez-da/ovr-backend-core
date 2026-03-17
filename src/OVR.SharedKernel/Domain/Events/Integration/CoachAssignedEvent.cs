using OVR.SharedKernel.Domain.Events;

namespace OVR.SharedKernel.Domain.Events.Integration;

public sealed record CoachAssignedEvent(
    string ParticipantId,
    string EventRsc,
    string Function) : DomainEventBase;
