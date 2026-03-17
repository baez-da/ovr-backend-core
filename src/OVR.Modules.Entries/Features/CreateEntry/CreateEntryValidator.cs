using FluentValidation;

namespace OVR.Modules.Entries.Features.CreateEntry;

public sealed class CreateEntryValidator : AbstractValidator<CreateEntryCommand>
{
    public CreateEntryValidator()
    {
        RuleFor(x => x.ParticipantId).NotEmpty();
        RuleFor(x => x.EventRsc).NotEmpty().MinimumLength(4);
        RuleFor(x => x.CompetitorType).NotEmpty()
            .Must(t => t is "Individual" or "Team")
            .WithMessage("CompetitorType must be Individual or Team");
        RuleFor(x => x.Organisation).NotEmpty().Length(3);
    }
}
