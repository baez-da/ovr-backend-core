using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.SharedKernel.Contracts;

public interface ICommonCodeCache
{
    bool Exists(string type, string code);
    string? GetDescription(string type, string code, string language);
    IReadOnlyDictionary<string, CommonCodeEntry> GetByType(string type);
    IReadOnlyList<string> GetAvailableTypes();
    string GetVersion(string type);
}

public sealed record CommonCodeEntry(
    string Code,
    int Order,
    IReadOnlyDictionary<string, LocalizedText> Name,
    IReadOnlyDictionary<string, string> Attributes);
