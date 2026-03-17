using FluentValidation;

namespace OVR.Modules.OfficialAssignment.Features.AssignOfficial;

public sealed class AssignOfficialValidator : AbstractValidator<AssignOfficialCommand>
{
    public AssignOfficialValidator()
    {
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x.UnitRsc).NotEmpty().MinimumLength(4);
        RuleFor(x => x.Function).NotEmpty();
        RuleFor(x => x.Organisation).NotEmpty().Length(3);
    }
}
