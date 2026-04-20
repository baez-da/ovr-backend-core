using OVR.SharedKernel.Domain.Progression;

namespace OVR.Modules.CompetitionConfig.Domain;

public sealed record PhaseSpec(string Code, int Order, int UnitCount);

/// <summary>
/// Seed pairing for a single first-round unit. Null for units in later rounds.
/// </summary>
public sealed record UnitSeedPairing(int? SeedA, int? SeedB);

public sealed record BracketPlan(
    IReadOnlyList<PhaseSpec> Phases,
    IReadOnlyList<string> UnitLocalSegments,
    IReadOnlyList<UnitSeedPairing> UnitSeedPairings,
    IReadOnlyList<ProgressionEdge> Edges);

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
        var seedPairings = new List<UnitSeedPairing>();
        var unitCounter = startUnitNumber;

        // Compute first-round seed pairings (index 0 = first phase)
        var firstRoundPairings = ComputeFirstRoundPairings(m);

        for (var i = 0; i < phaseCodes.Length; i++)
        {
            var phaseUnitCount = m >> (i + 1); // M / 2^(i+1)
            phases.Add(new PhaseSpec(phaseCodes[i], i, phaseUnitCount));

            for (var u = 0; u < phaseUnitCount; u++)
            {
                var unitBlock = $"{unitCounter:D4}----";
                segments.Add($"{phaseCodes[i]}{unitBlock}");

                // Only the first phase gets seed pairings
                if (i == 0)
                    seedPairings.Add(new UnitSeedPairing(firstRoundPairings[u].SeedA, firstRoundPairings[u].SeedB));
                else
                    seedPairings.Add(new UnitSeedPairing(null, null));

                unitCounter++;
            }
        }

        var edges = ComputeEdges(phaseCodes, m, startUnitNumber);
        return new BracketPlan(phases, segments, seedPairings, edges);
    }

    private static IReadOnlyList<ProgressionEdge> ComputeEdges(
        string[] phaseCodes,
        int bracketSize,
        int startUnitNumber)
    {
        var edges = new List<ProgressionEdge>();
        var phaseOffsets = ComputePhaseOffsets(phaseCodes, bracketSize, startUnitNumber);

        // No edges out of the final phase.
        for (var i = 0; i < phaseCodes.Length - 1; i++)
        {
            var phaseUnitCount = bracketSize >> (i + 1);
            var sourceOffset = phaseOffsets[i];
            var targetOffset = phaseOffsets[i + 1];

            for (var u = 1; u <= phaseUnitCount; u++)
            {
                var sourceUnitNumber = sourceOffset + u - 1;
                var targetUnitIndex = (u + 1) / 2;          // ceil(u / 2)
                var targetUnitNumber = targetOffset + targetUnitIndex - 1;
                var targetSlot = (u % 2 == 1) ? 1 : 2;

                edges.Add(new ProgressionEdge(
                    SourceUnitRsc: $"{phaseCodes[i]}{sourceUnitNumber:D4}----",
                    Outcome: Outcome.W,
                    TargetUnitRsc: $"{phaseCodes[i + 1]}{targetUnitNumber:D4}----",
                    TargetSlot: targetSlot));
            }
        }

        return edges;
    }

    private static int[] ComputePhaseOffsets(string[] phaseCodes, int bracketSize, int startUnitNumber)
    {
        var offsets = new int[phaseCodes.Length];
        var cursor = startUnitNumber;
        for (var i = 0; i < phaseCodes.Length; i++)
        {
            offsets[i] = cursor;
            cursor += bracketSize >> (i + 1);
        }
        return offsets;
    }

    private static IReadOnlyList<(int SeedA, int SeedB)> ComputeFirstRoundPairings(int bracketSize)
    {
        var pairings = new List<(int, int)>();
        var seedOrder = BuildSeedOrder(bracketSize);
        for (var i = 0; i < bracketSize; i += 2)
            pairings.Add((seedOrder[i], seedOrder[i + 1]));
        return pairings;
    }

    private static int[] BuildSeedOrder(int size)
    {
        // Classic recursion: [1,2] → [1,4,3,2] → [1,8,5,4,3,6,7,2] → ...
        if (size == 1) return [1];
        var half = BuildSeedOrder(size / 2);
        var result = new int[size];
        for (var i = 0; i < half.Length; i++)
        {
            result[2 * i] = half[i];
            result[2 * i + 1] = size + 1 - half[i];
        }
        return result;
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
