using FluentValidation;
using OVR.Modules.DataEntry.SportRules;

namespace OVR.Modules.DataEntry.Features.ScorePeriod;

public sealed class ScorePeriodValidator : AbstractValidator<ScorePeriodCommand>
{
    private static readonly string[] ValidJudges = { "J1", "J2", "J3" };

    public ScorePeriodValidator()
    {
        RuleFor(x => x.UnitRsc).NotEmpty();
        RuleFor(x => x.PeriodCode).Must(p => BoxingRules.PeriodCodes.Contains(p))
            .WithMessage("PeriodCode must be one of R1, R2, R3.");
        RuleFor(x => x.Scorecards).NotNull()
            .Must(s => s.Count == 3).WithMessage("Exactly 3 scorecards required.");
        RuleForEach(x => x.Scorecards).ChildRules(card =>
        {
            card.RuleFor(c => c.JudgePos).Must(p => ValidJudges.Contains(p))
                .WithMessage("JudgePos must be J1, J2 or J3.");
            card.RuleFor(c => c.HomeScore)
                .InclusiveBetween(BoxingRules.MinPeriodScore, BoxingRules.MaxPeriodScore);
            card.RuleFor(c => c.AwayScore)
                .InclusiveBetween(BoxingRules.MinPeriodScore, BoxingRules.MaxPeriodScore);
        });
    }
}
