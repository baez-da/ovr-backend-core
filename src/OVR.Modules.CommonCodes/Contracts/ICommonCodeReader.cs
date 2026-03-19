using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CommonCodes.Contracts;

public interface ICommonCodeReader
{
    Task<bool> ExistsAsync(string type, string code, CancellationToken ct);
    Task<MultilingualText?> GetNameAsync(string type, string code, CancellationToken ct);
}
