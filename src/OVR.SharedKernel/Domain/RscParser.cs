namespace OVR.SharedKernel.Domain;

public static class RscParser
{
    public const int EventRscLength = 22;
    public const int UnitRscLength = 34;

    public static string GetEventRscFromUnitRsc(string unitRsc)
    {
        if (string.IsNullOrEmpty(unitRsc) || unitRsc.Length < EventRscLength)
            throw new ArgumentException($"Invalid unit RSC: '{unitRsc}'.", nameof(unitRsc));

        return unitRsc[..EventRscLength];
    }
}
