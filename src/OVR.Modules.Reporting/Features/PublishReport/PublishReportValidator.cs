using FluentValidation;

namespace OVR.Modules.Reporting.Features.PublishReport;

public sealed class PublishReportValidator : AbstractValidator<PublishReportCommand>
{
    public PublishReportValidator()
    {
        RuleFor(x => x.Rsc).NotEmpty();
        RuleFor(x => x.OrisCode).NotEmpty();
    }
}
