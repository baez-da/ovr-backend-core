using ErrorOr;
using MediatR;

namespace OVR.Modules.CommonCodes.Features.ListCommonCodeTypes;

public sealed record ListCommonCodeTypesQuery : IRequest<ErrorOr<ListCommonCodeTypesResponse>>;

public sealed record ListCommonCodeTypesResponse(IReadOnlyList<string> Types);
