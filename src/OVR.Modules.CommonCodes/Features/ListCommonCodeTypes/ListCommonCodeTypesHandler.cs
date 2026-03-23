using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Persistence;

namespace OVR.Modules.CommonCodes.Features.ListCommonCodeTypes;

public sealed class ListCommonCodeTypesHandler(ICommonCodeRepository repository)
    : IRequestHandler<ListCommonCodeTypesQuery, ErrorOr<ListCommonCodeTypesResponse>>
{
    public async Task<ErrorOr<ListCommonCodeTypesResponse>> Handle(
        ListCommonCodeTypesQuery request,
        CancellationToken cancellationToken)
    {
        var types = await repository.GetDistinctTypesAsync(cancellationToken);
        return new ListCommonCodeTypesResponse(types);
    }
}
