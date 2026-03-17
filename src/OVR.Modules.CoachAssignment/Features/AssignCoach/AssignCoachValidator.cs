using FluentValidation;

namespace OVR.Modules.CoachAssignment.Features.AssignCoach;

public sealed class AssignCoachValidator : AbstractValidator<AssignCoachCommand>
{
    public AssignCoachValidator()
    {
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x.EventRsc).NotEmpty().MinimumLength(4);
        RuleFor(x => x.Function).NotEmpty();
        RuleFor(x => x.Organisation).NotEmpty().Length(3);
    }
}
