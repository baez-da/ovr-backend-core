using FluentValidation;

namespace OVR.Modules.Reporting.Features.GetReport;

public sealed class GetReportValidator : AbstractValidator<GetReportQuery>
{
    public GetReportValidator()
    {
        RuleFor(x => x.Rsc).NotEmpty();
        RuleFor(x => x.OrisCode).NotEmpty();
    }
}
