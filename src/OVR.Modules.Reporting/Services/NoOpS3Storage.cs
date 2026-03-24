namespace OVR.Modules.Reporting.Services;

/// <summary>
/// No-op implementation of IS3Storage used until a real S3 provider is configured.
/// </summary>
internal sealed class NoOpS3Storage : IS3Storage
{
    public Task<string> UploadAsync(string key, byte[] content, string contentType, CancellationToken ct = default)
    {
        // Returns the key as a local URL placeholder
        return Task.FromResult($"/storage/{key}");
    }

    public string GetUrl(string key) => $"/storage/{key}";
}
