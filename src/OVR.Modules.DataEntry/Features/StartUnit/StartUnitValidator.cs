using FluentValidation;

namespace OVR.Modules.DataEntry.Features.StartUnit;

public sealed class StartUnitValidator : AbstractValidator<StartUnitCommand>
{
    public StartUnitValidator()
    {
        RuleFor(x => x.UnitRsc).NotEmpty();
    }
}
