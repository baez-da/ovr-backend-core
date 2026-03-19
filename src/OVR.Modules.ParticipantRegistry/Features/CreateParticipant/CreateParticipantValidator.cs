using FluentValidation;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public sealed class CreateParticipantValidator : AbstractValidator<CreateParticipantCommand>
{
    public CreateParticipantValidator()
    {
        RuleFor(x => x.Type).NotEmpty().Must(BeValidType).WithMessage("Type must be Athlete, TeamOfficial, or TechnicalOfficial");
        RuleFor(x => x.GivenName).MaximumLength(100).When(x => x.GivenName is not null);
        RuleFor(x => x.FamilyName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.GenderCode).NotEmpty().Must(code => code is "M" or "F" or "X").WithMessage("Gender must be M, F, or X");
        RuleFor(x => x.Organisation).NotEmpty().Length(3);
    }

    private static bool BeValidType(string type) =>
        Enum.TryParse<Domain.ParticipantType>(type, ignoreCase: true, out _);
}
