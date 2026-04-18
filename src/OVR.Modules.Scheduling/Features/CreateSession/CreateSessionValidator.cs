using FluentValidation;

namespace OVR.Modules.Scheduling.Features.CreateSession;

public sealed class CreateSessionValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(1, 10)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("Code must be 1..10 uppercase alphanumeric chars.");

        RuleFor(x => x.VenueCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z0-9]{3}$")
            .WithMessage("VenueCode must be exactly 3 uppercase alphanumeric chars.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(1, 40);

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be greater than StartDate.");

        RuleFor(x => x.Leadin)
            .Must(l => l >= TimeSpan.Zero)
            .When(x => x.Leadin.HasValue)
            .WithMessage("Leadin must be non-negative when provided.");
    }
}
