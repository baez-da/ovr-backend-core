namespace OVR.Modules.CompetitionConfig.Domain;

public sealed record PhaseSpec(string Code, int Order, int UnitCount);

public sealed record BracketPlan(
    IReadOnlyList<PhaseSpec> Phases,
    IReadOnlyList<string> UnitLocalSegments);

public sealed class BracketGenerator
{
    private const int MinSize = 2;
    private const int MaxSize = 128;

    public BracketPlan Generate(CompetitionFormat format, int size, int startUnitNumber)
    {
        if (size < MinSize || size > MaxSize)
            throw new ArgumentOutOfRangeException(
                nameof(size),
                size,
                $"Size must be between {MinSize} and {MaxSize}.");

        if (format != CompetitionFormat.SingleElimination)
            throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "Only SingleElimination is supported in MVP.");

        var m = SmallestPowerOf2AtLeast(size);
        var phaseCodes = PhasesForBracketSize(m);

        var phases = new List<PhaseSpec>();
        var segments = new List<string>();
        var unitCounter = startUnitNumber;

        for (var i = 0; i < phaseCodes.Length; i++)
        {
            var phaseUnitCount = m >> (i + 1); // M / 2^(i+1)
            phases.Add(new PhaseSpec(phaseCodes[i], i, phaseUnitCount));

            for (var u = 0; u < phaseUnitCount; u++)
            {
                var unitBlock = $"{unitCounter:D4}----";
                segments.Add($"{phaseCodes[i]}{unitBlock}");
                unitCounter++;
            }
        }

        return new BracketPlan(phases, segments);
    }

    private static int SmallestPowerOf2AtLeast(int n)
    {
        var p = 1;
        while (p < n) p <<= 1;
        return p;
    }

    private static string[] PhasesForBracketSize(int m) => m switch
    {
        2 => [PhaseCodes.Final],
        4 => [PhaseCodes.SemiFinals, PhaseCodes.Final],
        8 => [PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        16 => [PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        32 => [PhaseCodes.R32, PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        64 => [PhaseCodes.R64, PhaseCodes.R32, PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        128 => [PhaseCodes.R128, PhaseCodes.R64, PhaseCodes.R32, PhaseCodes.EighthFinals, PhaseCodes.QuarterFinals, PhaseCodes.SemiFinals, PhaseCodes.Final],
        _ => throw new ArgumentOutOfRangeException(nameof(m), m, "Unsupported bracket size.")
    };
}
