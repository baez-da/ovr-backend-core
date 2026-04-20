namespace OVR.SharedKernel.Domain;

public static class RscParser
{
    public const int EventRscLength = 22;
    public const int UnitRscLength = 34;

    public static string GetEventRscFromUnitRsc(string unitRsc)
    {
        if (string.IsNullOrEmpty(unitRsc) || unitRsc.Length < EventRscLength)
            throw new ArgumentException($"Invalid unit RSC: '{unitRsc}'.", nameof(unitRsc));

        // Return the full 34-char event-level RSC (same format stored as BracketProgression.EventRsc
        // and Event.Id — the 22-char prefix padded to 34 chars with dashes).
        return unitRsc[..EventRscLength].PadRight(UnitRscLength, '-');
    }
}
