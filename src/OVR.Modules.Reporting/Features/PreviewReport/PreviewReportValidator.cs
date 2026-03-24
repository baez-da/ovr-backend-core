using FluentValidation;

namespace OVR.Modules.Reporting.Features.PreviewReport;

public sealed class PreviewReportValidator : AbstractValidator<PreviewReportQuery>
{
    public PreviewReportValidator()
    {
        RuleFor(x => x.Rsc).NotEmpty();
        RuleFor(x => x.OrisCode).NotEmpty();
    }
}
