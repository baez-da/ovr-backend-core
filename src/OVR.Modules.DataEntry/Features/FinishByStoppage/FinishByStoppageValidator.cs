using FluentValidation;
using OVR.Modules.DataEntry.Domain;
using OVR.Modules.DataEntry.SportRules;

namespace OVR.Modules.DataEntry.Features.FinishByStoppage;

public sealed class FinishByStoppageValidator
    : AbstractValidator<FinishByStoppageCommand>
{
    public FinishByStoppageValidator()
    {
        RuleFor(x => x.UnitRsc).NotEmpty();
        RuleFor(x => x.ResultCode).Must(c =>
            Enum.TryParse<ResultCode>(c, out var rc) && rc != ResultCode.Wp)
            .WithMessage("ResultCode must be a valid stoppage code (not WP).");
        RuleFor(x => x.StoppageRound).Must(r => BoxingRules.PeriodCodes.Contains(r))
            .WithMessage("StoppageRound must be R1, R2 or R3.");
        RuleFor(x => x.StoppageTime).Matches(@"^\d{1,2}:\d{2}$");
    }
}
