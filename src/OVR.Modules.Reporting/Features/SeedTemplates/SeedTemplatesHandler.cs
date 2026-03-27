using ErrorOr;
using MediatR;
using OVR.Modules.Reporting.Persistence;
using OVR.Modules.Reporting.Persistence.Documents;

namespace OVR.Modules.Reporting.Features.SeedTemplates;

public sealed class SeedTemplatesHandler(
    IReportTemplateRepository templateRepository,
    IReportLayoutRepository layoutRepository,
    IReportPartialRepository partialRepository)
    : IRequestHandler<SeedTemplatesCommand, ErrorOr<SeedTemplatesResponse>>
{
    // Layout component names that map to _global directory
    private static readonly HashSet<string> LayoutComponents = ["header", "footer", "style"];

    public async Task<ErrorOr<SeedTemplatesResponse>> Handle(
        SeedTemplatesCommand request, CancellationToken cancellationToken)
    {
        var templatesRoot = Path.Combine(AppContext.BaseDirectory, "Templates.Reporting");
        if (!Directory.Exists(templatesRoot))
            return new SeedTemplatesResponse(0);

        var processed = 0;
        var now = DateTime.UtcNow;

        foreach (var dir in Directory.EnumerateDirectories(templatesRoot, "*", SearchOption.TopDirectoryOnly))
        {
            var dirName = Path.GetFileName(dir);

            if (dirName == "_global")
            {
                foreach (var file in Directory.EnumerateFiles(dir, "*.html", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(dir, file);
                    var segments = relativePath.Split(Path.DirectorySeparatorChar);

                    if (segments.Length >= 2 && segments[0].Equals("partials", StringComparison.OrdinalIgnoreCase))
                    {
                        // Upsert as reusable partial (name = filename without extension)
                        var partialName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                        var content = await File.ReadAllTextAsync(file, cancellationToken);
                        var doc = new ReportPartialDocument
                        {
                            Name = partialName,
                            Content = content,
                            UpdatedAt = now
                        };
                        await partialRepository.UpsertAsync(doc, cancellationToken);
                    }
                    else
                    {
                        // Upsert as layout component (header, footer, style)
                        var component = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                        if (component == "styles") component = "style";
                        if (!LayoutComponents.Contains(component)) continue;

                        var content = await File.ReadAllTextAsync(file, cancellationToken);
                        var doc = new ReportLayoutDocument
                        {
                            Component = component,
                            Discipline = null,
                            ChampionshipCode = null,
                            Content = content,
                            UpdatedAt = now
                        };
                        await layoutRepository.UpsertAsync(doc, cancellationToken);
                    }

                    processed++;
                }
            }
            else
            {
                // Each subdirectory is an orisCode (optionally prefixed with discipline)
                // Convention: {orisCode}/ or {discipline}_{orisCode}/
                // For now treat directory name as orisCode directly
                var orisCode = dirName;

                foreach (var file in Directory.EnumerateFiles(dir, "*.html", SearchOption.AllDirectories))
                {
                    var content = await File.ReadAllTextAsync(file, cancellationToken);
                    var doc = new ReportTemplateDocument
                    {
                        OrisCode = orisCode,
                        Discipline = null,
                        ChampionshipCode = null,
                        Content = content,
                        UpdatedAt = now
                    };
                    await templateRepository.UpsertAsync(doc, cancellationToken);
                    processed++;
                }
            }
        }

        return new SeedTemplatesResponse(processed);
    }
}
