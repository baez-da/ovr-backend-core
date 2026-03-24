namespace OVR.Modules.Reporting.Services;

public interface IS3Storage
{
    Task<string> UploadAsync(string key, byte[] content, string contentType, CancellationToken ct = default);
    string GetUrl(string key);
}
