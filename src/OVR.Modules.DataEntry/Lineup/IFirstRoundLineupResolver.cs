using ErrorOr;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.Entries.Contracts;

namespace OVR.Modules.DataEntry.Lineup;

public interface IFirstRoundLineupResolver
{
    ErrorOr<(Competitor Red, Competitor Blue)> Resolve(
        int seedA, int seedB, IReadOnlyList<EntryDto> activeEntries);
}
