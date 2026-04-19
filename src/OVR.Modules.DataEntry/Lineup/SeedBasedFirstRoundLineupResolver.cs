using ErrorOr;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.Entries.Contracts;

namespace OVR.Modules.DataEntry.Lineup;

// TD-DE-01: corner assignment convention ("lower seed → red") is local.
// Refactor path: move to IUnitLineupReader.GetCornerAssignment in CompetitionConfig.
// See docs/superpowers/specs/2026-04-18-dataentry-mvp-design.md § Technical debt.
public sealed class SeedBasedFirstRoundLineupResolver : IFirstRoundLineupResolver
{
    public ErrorOr<(Competitor Red, Competitor Blue)> Resolve(
        int seedA, int seedB, IReadOnlyList<EntryDto> activeEntries)
    {
        // Duplicate check first (SingleOrDefault would throw).
        if (activeEntries.Count(e => e.Seed == seedA) > 1 ||
            activeEntries.Count(e => e.Seed == seedB) > 1)
            return Error.Validation("DataEntry.LineupResolutionFailed",
                "Duplicate seeds present in active entries.");

        var entryA = activeEntries.SingleOrDefault(e => e.Seed == seedA);
        var entryB = activeEntries.SingleOrDefault(e => e.Seed == seedB);

        if (entryA is null || entryB is null)
            return Error.NotFound("DataEntry.LineupResolutionFailed",
                $"Could not resolve seeds ({seedA}, {seedB}) to active entries.");

        var lowerSeed = seedA < seedB ? entryA : entryB;
        var higherSeed = seedA < seedB ? entryB : entryA;

        var red = new Competitor(
            SortOrder: 1,
            ParticipantId: lowerSeed.ParticipantId,
            NocompDetail: null,
            Seed: lowerSeed.Seed,
            Organisation: lowerSeed.Organisation,
            Wlt: null);

        var blue = new Competitor(
            SortOrder: 2,
            ParticipantId: higherSeed.ParticipantId,
            NocompDetail: null,
            Seed: higherSeed.Seed,
            Organisation: higherSeed.Organisation,
            Wlt: null);

        return (red, blue);
    }
}
