using FluentValidation;

namespace OVR.Modules.Entries.Features.ChangeEntryStatus;

public sealed class ChangeEntryStatusValidator : AbstractValidator<ChangeEntryStatusCommand>
{
    public ChangeEntryStatusValidator()
    {
        RuleFor(x => x.EntryId).NotEmpty();
        RuleFor(x => x.NewStatus).NotEmpty()
            .Must(s => Enum.TryParse<Domain.EntryStatus>(s, ignoreCase: true, out _))
            .WithMessage("NewStatus must be a valid EntryStatus value");
    }
}
