using FluentValidation;

namespace OVR.Ingestion.Features.ImportDtPartic;

public sealed class ImportDtParticValidator : AbstractValidator<ImportDtParticCommand>
{
    public ImportDtParticValidator()
    {
        RuleFor(x => x.File).NotNull().WithMessage("XML file is required.");
    }
}
