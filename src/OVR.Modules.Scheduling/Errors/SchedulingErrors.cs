using ErrorOr;

namespace OVR.Modules.Scheduling.Errors;

public static class SchedulingErrors
{
    public static Error InvalidVenue(string code) =>
        Error.Validation(
            "Scheduling.InvalidVenue",
            "Venue code is not in the common codes catalog.",
            new Dictionary<string, object> { ["venueCode"] = code });

    public static Error SessionAlreadyExists(string code) =>
        Error.Conflict(
            "Scheduling.SessionAlreadyExists",
            "A session with this code already exists.",
            new Dictionary<string, object> { ["sessionCode"] = code });

    public static Error SessionNotFound(string code) =>
        Error.NotFound(
            "Scheduling.SessionNotFound",
            "Session not found.",
            new Dictionary<string, object> { ["sessionCode"] = code });

    public static Error StartTimeOutsideSession(
        DateTime startTime, DateTime sessionStart, DateTime sessionEnd) =>
        Error.Validation(
            "Scheduling.StartTimeOutsideSession",
            "StartTime is outside the session's date range.",
            new Dictionary<string, object>
            {
                ["startTime"] = startTime,
                ["sessionStart"] = sessionStart,
                ["sessionEnd"] = sessionEnd
            });

    public static Error UnitAlreadyScheduled(string unitRsc) =>
        Error.Conflict(
            "Scheduling.UnitAlreadyScheduled",
            "This unit is already scheduled. Use reschedule instead.",
            new Dictionary<string, object> { ["unitRsc"] = unitRsc });

    public static Error LocationTimeOccupied(
        string locationCode, DateTime startTime, string conflictingUnitRsc) =>
        Error.Conflict(
            "Scheduling.LocationTimeOccupied",
            "Another unit is already scheduled at this location and time.",
            new Dictionary<string, object>
            {
                ["locationCode"] = locationCode,
                ["startTime"] = startTime,
                ["conflictingUnit"] = conflictingUnitRsc
            });

    public static Error UnitScheduleNotFound(string unitRsc) =>
        Error.NotFound(
            "Scheduling.UnitScheduleNotFound",
            "Unit schedule not found.",
            new Dictionary<string, object> { ["unitRsc"] = unitRsc });
}
