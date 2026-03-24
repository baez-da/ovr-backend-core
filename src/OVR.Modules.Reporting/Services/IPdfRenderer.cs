using ErrorOr;

namespace OVR.Modules.Reporting.Services;

public interface IPdfRenderer
{
    Task<ErrorOr<byte[]>> RenderAsync(RenderedHtml html, CancellationToken ct = default);
}
