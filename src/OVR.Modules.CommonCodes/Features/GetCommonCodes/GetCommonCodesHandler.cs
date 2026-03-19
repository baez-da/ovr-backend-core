using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Persistence;

namespace OVR.Modules.CommonCodes.Features.GetCommonCodes;

public sealed class GetCommonCodesHandler(ICommonCodeRepository repository)
    : IRequestHandler<GetCommonCodesQuery, ErrorOr<GetCommonCodesResponse>>
{
    public async Task<ErrorOr<GetCommonCodesResponse>> Handle(
        GetCommonCodesQuery request,
        CancellationToken cancellationToken)
    {
        var documents = await repository.GetByTypeAsync(request.Type, cancellationToken);

        var dtos = documents.Select(doc =>
        {
            var name = doc.Name
                .Where(kvp => request.Languages is null ||
                              request.Languages.Length == 0 ||
                              request.Languages.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new LocalizedTextDto(kvp.Value.Long, kvp.Value.Short));

            return new CommonCodeDto(doc.Code, name, doc.Order, doc.Attributes);
        }).ToList();

        return new GetCommonCodesResponse(dtos);
    }
}
