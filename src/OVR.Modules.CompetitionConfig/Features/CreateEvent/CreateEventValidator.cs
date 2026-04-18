using FluentValidation;

namespace OVR.Modules.CompetitionConfig.Features.CreateEvent;

public sealed class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Discipline)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Discipline must be 3 uppercase letters.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .Must(g => g is "M" or "W" or "X")
            .WithMessage("Gender must be M, W, or X.");

        RuleFor(x => x.EventCode)
            .NotEmpty()
            .Length(1, 8)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("EventCode must be 1..8 uppercase alphanumeric chars.");

        RuleFor(x => x.Modifier)
            .Length(1, 10)
            .Matches("^[A-Z0-9]+$")
            .When(x => x.Modifier is not null)
            .WithMessage("Modifier must be 1..10 uppercase alphanumeric chars when provided.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(1, 80);
    }
}
