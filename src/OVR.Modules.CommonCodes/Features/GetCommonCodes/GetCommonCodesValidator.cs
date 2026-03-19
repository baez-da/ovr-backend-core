using FluentValidation;

namespace OVR.Modules.CommonCodes.Features.GetCommonCodes;

public sealed class GetCommonCodesValidator : AbstractValidator<GetCommonCodesQuery>
{
    public GetCommonCodesValidator()
    {
        RuleFor(x => x.Type).NotEmpty();
    }
}
