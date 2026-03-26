using ErrorOr;
using OVR.Modules.Reporting.Errors;

namespace OVR.Modules.Reporting.Services;

/// <summary>Concrete wrapper so the implicit operator of ErrorOr can convert it.</summary>
public sealed record DataProviderResult(IReportDataProvider Provider);

public sealed class DataProviderFactory
{
    private readonly IEnumerable<IReportDataProvider> _providers;

    public DataProviderFactory(IEnumerable<IReportDataProvider> providers)
    {
        _providers = providers;
    }

    public ErrorOr<DataProviderResult> Resolve(string? discipline, string orisCode)
    {
        // Try discipline-specific first
        if (discipline is not null)
        {
            var disciplineProvider = _providers.FirstOrDefault(p =>
                p.OrisCode == orisCode &&
                p.Discipline == discipline);

            if (disciplineProvider is not null)
                return new DataProviderResult(disciplineProvider);
        }

        // Fallback to generic (null discipline)
        var genericProvider = _providers.FirstOrDefault(p =>
            p.OrisCode == orisCode &&
            p.Discipline is null);

        if (genericProvider is not null)
            return new DataProviderResult(genericProvider);

        return ReportingErrors.DataProviderNotFound(orisCode, discipline);
    }
}
