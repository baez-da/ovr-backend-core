using OVR.Modules.Scheduling.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.Scheduling.Persistence;

internal static class UnitScheduleMapping
{
    public static UnitScheduleDocument ToDocument(UnitSchedule schedule) => new()
    {
        Id = schedule.Id,
        EventRsc = schedule.EventRsc.Value,
        SessionCode = schedule.SessionCode,
        LocationCode = schedule.LocationCode,
        StartTime = schedule.StartTime,
        OrderInSession = schedule.OrderInSession,
        OrderInLocation = schedule.OrderInLocation,
        Status = schedule.Status.ToString(),
        ScheduledAt = schedule.ScheduledAt,
        UpdatedAt = schedule.UpdatedAt
    };

    public static UnitSchedule ToDomain(UnitScheduleDocument doc)
    {
        var unitRsc = Rsc.Create(doc.Id);
        var eventRsc = Rsc.Create(doc.EventRsc);
        var status = Enum.Parse<ScheduleStatus>(doc.Status, ignoreCase: true);

        return UnitSchedule.Hydrate(
            unitRsc, eventRsc, doc.SessionCode, doc.LocationCode,
            doc.StartTime, doc.OrderInSession, doc.OrderInLocation,
            status, doc.ScheduledAt, doc.UpdatedAt);
    }
}
