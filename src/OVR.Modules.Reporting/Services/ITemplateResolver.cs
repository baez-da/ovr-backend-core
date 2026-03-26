using ErrorOr;

namespace OVR.Modules.Reporting.Services;

public record ResolvedTemplates(
    string BodyTemplate,
    string HeaderTemplate,
    string FooterTemplate,
    string GlobalStyles,
    IReadOnlyDictionary<string, string> Partials);

public interface ITemplateResolver
{
    Task<ErrorOr<ResolvedTemplates>> ResolveAsync(string? discipline, string orisCode, CancellationToken ct = default);
}
