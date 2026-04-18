using FluentValidation;

namespace OVR.Modules.Scheduling.Features.RescheduleUnit;

public sealed class RescheduleUnitValidator : AbstractValidator<RescheduleUnitCommand>
{
    public RescheduleUnitValidator()
    {
        RuleFor(x => x.UnitRsc).NotEmpty().Length(34);
        RuleFor(x => x.SessionCode).NotEmpty();
        RuleFor(x => x.LocationCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z0-9]{3}$");
        RuleFor(x => x.StartTime).NotEqual(default(DateTime));
        RuleFor(x => x.OrderInSession).GreaterThanOrEqualTo(1);
        RuleFor(x => x.OrderInLocation).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Reason)
            .Length(1, 200)
            .When(x => x.Reason is not null);
    }
}
