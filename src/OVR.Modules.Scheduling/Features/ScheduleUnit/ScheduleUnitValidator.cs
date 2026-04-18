using FluentValidation;

namespace OVR.Modules.Scheduling.Features.ScheduleUnit;

public sealed class ScheduleUnitValidator : AbstractValidator<ScheduleUnitCommand>
{
    public ScheduleUnitValidator()
    {
        RuleFor(x => x.SessionCode).NotEmpty();
        RuleFor(x => x.UnitRsc).NotEmpty().Length(34);
        RuleFor(x => x.LocationCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z0-9]{3}$");
        RuleFor(x => x.StartTime).NotEqual(default(DateTime));
        RuleFor(x => x.OrderInSession).GreaterThanOrEqualTo(1);
        RuleFor(x => x.OrderInLocation).GreaterThanOrEqualTo(1);
    }
}
