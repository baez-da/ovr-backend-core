using FluentValidation;

namespace OVR.Modules.Scheduling.Features.ListUnitsByLocation;

public sealed class ListUnitsByLocationValidator : AbstractValidator<ListUnitsByLocationQuery>
{
    public ListUnitsByLocationValidator()
    {
        RuleFor(x => x.LocationCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z0-9]{3}$");
    }
}
