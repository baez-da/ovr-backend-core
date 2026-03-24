using FluentValidation;

namespace OVR.Modules.Reporting.Features.PreviewDailySchedule;

public sealed class PreviewDailyScheduleValidator : AbstractValidator<PreviewDailyScheduleQuery>
{
    public PreviewDailyScheduleValidator()
    {
        RuleFor(x => x.Rsc).NotEmpty();
        RuleFor(x => x.OrisCode).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
    }
}
