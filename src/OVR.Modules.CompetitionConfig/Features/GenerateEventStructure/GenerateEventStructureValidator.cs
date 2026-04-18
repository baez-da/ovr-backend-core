using FluentValidation;

namespace OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;

public sealed class GenerateEventStructureValidator : AbstractValidator<GenerateEventStructureCommand>
{
    public GenerateEventStructureValidator()
    {
        RuleFor(x => x.EventRsc)
            .NotEmpty()
            .Length(34);

        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(f => f is "SingleElimination")
            .WithMessage("Format must be SingleElimination in MVP.");

        RuleFor(x => x.Size)
            .InclusiveBetween(2, 128);

        RuleFor(x => x.StartUnitNumber)
            .InclusiveBetween(1, 9999);
    }
}
