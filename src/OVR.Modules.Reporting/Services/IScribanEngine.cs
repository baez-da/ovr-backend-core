namespace OVR.Modules.Reporting.Services;

public record RenderedHtml(string Body, string Header, string Footer);

public interface IScribanEngine
{
    Task<RenderedHtml> RenderAsync(ResolvedTemplates templates, object data, CancellationToken ct = default);
}
