namespace OVR.SharedKernel.Contracts;

public sealed record LocalizedCode(string Code, LocalizedTextDto Description);
public sealed record LocalizedTextDto(string Long, string? Short = null);
