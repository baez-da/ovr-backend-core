namespace OVR.Modules.CompetitionConfig.Contracts;

public interface IUnitLineupReader
{
    Task<(int? SeedA, int? SeedB)> GetSeedsForUnit(string unitRsc, CancellationToken ct);
}
