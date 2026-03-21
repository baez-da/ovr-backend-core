using FluentValidation;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public sealed class CreateParticipantValidator : AbstractValidator<CreateParticipantCommand>
{
    public CreateParticipantValidator()
    {
        RuleFor(x => x.GivenName).MaximumLength(100).When(x => x.GivenName is not null);
        RuleFor(x => x.FamilyName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.GenderCode).NotEmpty()
            .Must(code => code is "M" or "F" or "X").WithMessage("Gender must be M, F, or X");
        RuleFor(x => x.Organisation).NotEmpty().Length(3);
        RuleFor(x => x.Functions).NotEmpty().WithMessage("At least one function is required.");
        RuleForEach(x => x.Functions).ChildRules(f =>
        {
            f.RuleFor(x => x.FunctionId).NotEmpty();
            f.RuleFor(x => x.DisciplineCode).NotEmpty();
        });
    }
}
