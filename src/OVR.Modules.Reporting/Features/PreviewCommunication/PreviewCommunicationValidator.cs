using FluentValidation;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Features.PreviewCommunication;

public sealed class PreviewCommunicationValidator : AbstractValidator<PreviewCommunicationQuery>
{
    private static readonly string[] ValidAffects = ["results", "schedule", "other"];

    public PreviewCommunicationValidator()
    {
        RuleFor(x => x.Rsc).NotEmpty();
        RuleFor(x => x.OrisCode).NotEmpty();
        RuleFor(x => x.OrisCode)
            .Must(code => code is "C67" or "C68")
            .When(x => !string.IsNullOrEmpty(x.OrisCode))
            .WithMessage("OrisCode must be 'C67' or 'C68'.");
        RuleFor(x => x.Content).NotNull();
        RuleFor(x => x.Content.Body)
            .MaximumLength(10000)
            .When(x => x.Content is not null && x.Content.Body is not null);
        RuleFor(x => x.Content.Affects)
            .Must(affects => ValidAffects.Contains(affects))
            .When(x => x.Content is not null && x.Content.Affects is not null)
            .WithMessage("Affects must be one of 'results', 'schedule', 'other'.");
    }
}
