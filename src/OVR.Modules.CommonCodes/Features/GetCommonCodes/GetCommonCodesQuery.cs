using ErrorOr;
using MediatR;

namespace OVR.Modules.CommonCodes.Features.GetCommonCodes;

public sealed record GetCommonCodesQuery(
    string Type,
    string[]? Languages = null) : IRequest<ErrorOr<GetCommonCodesResponse>>;

public sealed record GetCommonCodesResponse(
    List<CommonCodeDto> CommonCodes);

public sealed record CommonCodeDto(
    string Code,
    Dictionary<string, LocalizedTextDto> Name,
    int Order,
    Dictionary<string, string> Attributes);

public sealed record LocalizedTextDto(
    string Long,
    string? Short);
