using FluentValidation;

namespace OVR.Modules.CommonCodes.Features.ImportFromExcel;

public sealed class ImportFromExcelValidator : AbstractValidator<ImportFromExcelCommand>
{
    private static readonly string[] ValidStrategies = ["overwrite", "update"];
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public ImportFromExcelValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .Must(f => f is { Length: > 0 and <= MaxFileSize })
            .WithMessage($"File must be between 1 byte and {MaxFileSize / (1024 * 1024)}MB.");

        RuleFor(x => x.Strategy)
            .NotEmpty()
            .Must(s => ValidStrategies.Contains(s.ToLowerInvariant()))
            .WithMessage("Strategy must be 'overwrite' or 'update'.");
    }
}
