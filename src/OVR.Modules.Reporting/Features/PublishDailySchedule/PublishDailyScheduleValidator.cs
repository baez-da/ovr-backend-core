using FluentValidation;

namespace OVR.Modules.Reporting.Features.PublishDailySchedule;

public sealed class PublishDailyScheduleValidator : AbstractValidator<PublishDailyScheduleCommand>
{
    public PublishDailyScheduleValidator()
    {
        RuleFor(x => x.Rsc).NotEmpty();
        RuleFor(x => x.OrisCode).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
    }
}
