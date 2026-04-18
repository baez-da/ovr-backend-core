using OVR.SharedKernel.Domain.Events.Integration;
using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Domain;

public sealed class UnitSchedule : AggregateRoot<string>
{
    public Rsc UnitRsc { get; private set; } = null!;
    public Rsc EventRsc { get; private set; } = null!;
    public string SessionCode { get; private set; } = string.Empty;
    public string LocationCode { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public int OrderInSession { get; private set; }
    public int OrderInLocation { get; private set; }
    public ScheduleStatus Status { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private UnitSchedule() { }

    public static UnitSchedule Create(
        Rsc unitRsc,
        string sessionCode,
        string locationCode,
        DateTime startTime,
        int orderInSession,
        int orderInLocation)
    {
        ArgumentNullException.ThrowIfNull(unitRsc);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationCode);

        if (!unitRsc.IsAtLevel(RscLevel.Unit))
            throw new ArgumentException(
                $"RSC must be at Unit level, got {unitRsc.Level}: '{unitRsc.Value}'.",
                nameof(unitRsc));

        if (locationCode.Length != 3)
            throw new ArgumentException(
                $"LocationCode must be exactly 3 characters, got '{locationCode}'.",
                nameof(locationCode));

        if (orderInSession < 1)
            throw new ArgumentException(
                $"OrderInSession must be >= 1, got {orderInSession}.",
                nameof(orderInSession));

        if (orderInLocation < 1)
            throw new ArgumentException(
                $"OrderInLocation must be >= 1, got {orderInLocation}.",
                nameof(orderInLocation));

        var eventRsc = Rsc.Create(unitRsc.AtEventLevel());
        var now = DateTime.UtcNow;

        var schedule = new UnitSchedule
        {
            Id = unitRsc.Value,
            UnitRsc = unitRsc,
            EventRsc = eventRsc,
            SessionCode = sessionCode,
            LocationCode = locationCode,
            StartTime = startTime,
            OrderInSession = orderInSession,
            OrderInLocation = orderInLocation,
            Status = ScheduleStatus.Scheduled,
            ScheduledAt = now
        };

        schedule.RaiseDomainEvent(new UnitScheduledEvent(
            UnitRsc: unitRsc.Value,
            EventRsc: eventRsc.Value,
            SessionCode: sessionCode,
            LocationCode: locationCode,
            StartTime: startTime,
            OrderInSession: orderInSession,
            OrderInLocation: orderInLocation,
            ScheduledAt: now));

        return schedule;
    }

    public void Reschedule(
        string newSessionCode,
        string newLocationCode,
        DateTime newStartTime,
        int newOrderInSession,
        int newOrderInLocation,
        string? reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newSessionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(newLocationCode);

        if (newLocationCode.Length != 3)
            throw new ArgumentException(
                $"LocationCode must be exactly 3 characters, got '{newLocationCode}'.",
                nameof(newLocationCode));

        if (newOrderInSession < 1)
            throw new ArgumentException(
                $"OrderInSession must be >= 1, got {newOrderInSession}.",
                nameof(newOrderInSession));

        if (newOrderInLocation < 1)
            throw new ArgumentException(
                $"OrderInLocation must be >= 1, got {newOrderInLocation}.",
                nameof(newOrderInLocation));

        if (Status != ScheduleStatus.Scheduled)
            throw new InvalidOperationException(
                $"Cannot reschedule a unit in status '{Status}'.");

        SessionCode = newSessionCode;
        LocationCode = newLocationCode;
        StartTime = newStartTime;
        OrderInSession = newOrderInSession;
        OrderInLocation = newOrderInLocation;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new UnitScheduleChangedEvent(
            UnitRsc: UnitRsc.Value,
            EventRsc: EventRsc.Value,
            SessionCode: newSessionCode,
            LocationCode: newLocationCode,
            StartTime: newStartTime,
            OrderInSession: newOrderInSession,
            OrderInLocation: newOrderInLocation,
            Reason: reason,
            ChangedAt: UpdatedAt.Value));
    }
}
