using ErrorOr;

namespace OVR.Modules.DataEntry.Errors;

public static class DataEntryErrors
{
    public static Error UnitResultNotFound(string rsc) =>
        Error.NotFound("DataEntry.UnitResultNotFound",
            $"UnitResult '{rsc}' not found.");

    public static Error InvalidCompetitors(string message) =>
        Error.Validation("DataEntry.InvalidCompetitors", message);

    public static Error InvalidStatusTransition(string from, string to) =>
        Error.Validation("DataEntry.InvalidStatusTransition",
            $"Cannot transition from {from} to {to}.");

    public static Error InvalidScorecardCount() =>
        Error.Validation("DataEntry.InvalidScorecardCount",
            "Exactly 3 scorecards are required (J1, J2, J3).");

    public static Error InvalidScoreRange(int value) =>
        Error.Validation("DataEntry.InvalidScoreRange",
            $"Score {value} is outside the allowed range [6..10].");

    public static Error DuplicateJudgePosition(string pos) =>
        Error.Validation("DataEntry.DuplicateJudgePosition",
            $"Judge position {pos} appears more than once.");

    public static Error InvalidPeriodOrder(string code) =>
        Error.Validation("DataEntry.InvalidPeriodOrder",
            $"Cannot score period {code} out of order.");

    public static Error PeriodAlreadyScored(string code) =>
        Error.Validation("DataEntry.PeriodAlreadyScored",
            $"Period {code} has already been scored.");

    public static Error DecisionAlreadyExists() =>
        Error.Validation("DataEntry.DecisionAlreadyExists",
            "Cannot modify scoring after a decision has been recorded.");

    public static Error InvalidPeriodCode(string code) =>
        Error.Validation("DataEntry.InvalidPeriodCode",
            $"Invalid period code '{code}'. Expected one of R1, R2, R3.");

    public static Error InvalidStoppageData(string reason) =>
        Error.Validation("DataEntry.InvalidStoppageData", reason);

    public static Error DecisionRequired() =>
        Error.Validation("DataEntry.DecisionRequired",
            "Cannot confirm without a Decision.");
}
