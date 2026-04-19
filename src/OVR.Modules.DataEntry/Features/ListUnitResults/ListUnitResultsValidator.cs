using FluentValidation;
using OVR.Modules.DataEntry.Domain;

namespace OVR.Modules.DataEntry.Features.ListUnitResults;

public sealed class ListUnitResultsValidator : AbstractValidator<ListUnitResultsQuery>
{
    public ListUnitResultsValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is null || Enum.TryParse<ResultStatus>(s, out _))
            .WithMessage("Status must be a valid ResultStatus.");
    }
}
