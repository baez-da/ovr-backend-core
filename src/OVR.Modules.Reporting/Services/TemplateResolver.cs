using ErrorOr;
using OVR.Modules.Reporting.Errors;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Services;

internal sealed class TemplateResolver : ITemplateResolver
{
    private readonly IReportTemplateRepository _templateRepo;
    private readonly IReportLayoutRepository _layoutRepo;
    private readonly IReportPartialRepository _partialRepo;

    public TemplateResolver(
        IReportTemplateRepository templateRepo,
        IReportLayoutRepository layoutRepo,
        IReportPartialRepository partialRepo)
    {
        _templateRepo = templateRepo;
        _layoutRepo = layoutRepo;
        _partialRepo = partialRepo;
    }

    public async Task<ErrorOr<ResolvedTemplates>> ResolveAsync(
        string? disciplineCode, string orisCode, CancellationToken ct = default)
    {
        // Try discipline-specific first, then generic (null discipline)
        ReportTemplateDocument? templateDoc = null;
        if (disciplineCode is not null)
            templateDoc = await _templateRepo.FindAsync(orisCode, disciplineCode, ct);

        templateDoc ??= await _templateRepo.FindAsync(orisCode, null, ct);

        if (templateDoc is null)
            return ReportingErrors.TemplateNotFound(orisCode, disciplineCode);

        // Resolve layouts and partials in parallel
        var headerTask = ResolveLayoutAsync("header", disciplineCode, ct);
        var footerTask = ResolveLayoutAsync("footer", disciplineCode, ct);
        var styleTask = ResolveLayoutAsync("style", disciplineCode, ct);
        var partialsTask = _partialRepo.GetAllAsync(ct);

        await Task.WhenAll(headerTask, footerTask, styleTask, partialsTask);

        var headerDoc = await headerTask;
        if (headerDoc is null)
            return ReportingErrors.LayoutNotFound("header");

        var footerDoc = await footerTask;
        if (footerDoc is null)
            return ReportingErrors.LayoutNotFound("footer");

        var styleDoc = await styleTask;
        var partialDocs = await partialsTask;

        var partials = partialDocs
            .ToDictionary(p => p.Name, p => p.Content);

        return new ResolvedTemplates(
            BodyTemplate: templateDoc.Content,
            HeaderTemplate: headerDoc.Content,
            FooterTemplate: footerDoc.Content,
            GlobalStyles: styleDoc?.Content ?? string.Empty,
            Partials: partials);
    }

    private async Task<ReportLayoutDocument?> ResolveLayoutAsync(
        string component, string? disciplineCode, CancellationToken ct)
    {
        ReportLayoutDocument? doc = null;
        if (disciplineCode is not null)
            doc = await _layoutRepo.FindAsync(component, disciplineCode, ct);

        doc ??= await _layoutRepo.FindAsync(component, null, ct);
        return doc;
    }
}
