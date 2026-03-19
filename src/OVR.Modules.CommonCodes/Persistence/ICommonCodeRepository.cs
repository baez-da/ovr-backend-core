namespace OVR.Modules.CommonCodes.Persistence;

public interface ICommonCodeRepository
{
    Task<IReadOnlyList<CommonCodeDocument>> GetByTypeAsync(string type, CancellationToken ct);
    Task<CommonCodeDocument?> GetAsync(string type, string code, CancellationToken ct);
    Task<bool> ExistsAsync(string type, string code, CancellationToken ct);
    Task UpsertManyAsync(IReadOnlyList<CommonCodeDocument> documents, CancellationToken ct);
    Task DeleteByTypeAsync(string type, CancellationToken ct);
}
