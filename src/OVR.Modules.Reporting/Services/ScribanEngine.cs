using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

namespace OVR.Modules.Reporting.Services;

internal sealed class ScribanEngine : IScribanEngine
{
    public async Task<RenderedHtml> RenderAsync(
        ResolvedTemplates templates, object data, CancellationToken ct = default)
    {
        // Pre-parse all partials
        var parsedPartials = templates.Partials
            .ToDictionary(p => p.Key, p => Template.Parse(p.Value));

        var bodyTemplate = Template.Parse(templates.BodyTemplate);
        var headerTemplate = Template.Parse(templates.HeaderTemplate);
        var footerTemplate = Template.Parse(templates.FooterTemplate);

        var bodyTask = RenderTemplateAsync(bodyTemplate, data, parsedPartials, templates.GlobalStyles, ct);
        var headerTask = RenderTemplateAsync(headerTemplate, data, parsedPartials, null, ct);
        var footerTask = RenderTemplateAsync(footerTemplate, data, parsedPartials, null, ct);

        await Task.WhenAll(bodyTask, headerTask, footerTask);

        return new RenderedHtml(
            Body: await bodyTask,
            Header: await headerTask,
            Footer: await footerTask);
    }

    private static async Task<string> RenderTemplateAsync(
        Template template,
        object data,
        Dictionary<string, Template> partials,
        string? globalStyles,
        CancellationToken ct)
    {
        var loader = new DictionaryTemplateLoader(partials);

        var context = new TemplateContext
        {
            CancellationToken = ct,
            MemberRenamer = StandardMemberRenamer.Default,
            TemplateLoader = loader
        };

        // Pre-seed CachedTemplates so partials don't need to be re-parsed on include
        foreach (var (name, parsedPartial) in partials)
            context.CachedTemplates[name] = parsedPartial;

        // Bind data model
        var scriptObject = new ScriptObject();
        scriptObject.Import(data, renamer: StandardMemberRenamer.Default);
        context.PushGlobal(scriptObject);

        var html = await template.RenderAsync(context);

        // Inject global CSS as <style> into body (only when styles provided)
        if (!string.IsNullOrEmpty(globalStyles))
            html = $"<style>\n{globalStyles}\n</style>\n{html}";

        return html;
    }

    /// <summary>Serves partials from an in-memory dictionary, bypassing file system access.</summary>
    private sealed class DictionaryTemplateLoader : ITemplateLoader
    {
        private readonly Dictionary<string, Template> _partials;

        public DictionaryTemplateLoader(Dictionary<string, Template> partials)
        {
            _partials = partials;
        }

        public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
            templateName;

        public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath)
        {
            if (_partials.TryGetValue(templatePath, out var tpl))
                return tpl.Page?.Body?.ToString() ?? string.Empty;
            throw new InvalidOperationException($"Partial '{templatePath}' not found.");
        }

        public ValueTask<string?> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath)
            => ValueTask.FromResult<string?>(Load(context, callerSpan, templatePath));
    }
}
