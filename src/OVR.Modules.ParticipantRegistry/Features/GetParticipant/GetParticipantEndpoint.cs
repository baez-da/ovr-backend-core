using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;
using OVR.SharedKernel.I18n;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public static class GetParticipantEndpoint
{
    public static async Task<IResult> Handle(
        string id, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var language = LanguageDetector.DetectLanguage(httpContext);
        var result = await sender.Send(new GetParticipantQuery(id, language), ct);
        return result.ToApiResult(httpContext);
    }
}
