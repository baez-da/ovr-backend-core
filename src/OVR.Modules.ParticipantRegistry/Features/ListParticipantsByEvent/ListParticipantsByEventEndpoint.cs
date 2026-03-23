using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;
using OVR.SharedKernel.I18n;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByEvent;

public static class ListParticipantsByEventEndpoint
{
    public static async Task<IResult> Handle(
        string organisation, ISender sender, CancellationToken ct, HttpContext httpContext)
    {
        var language = LanguageDetector.DetectLanguage(httpContext);
        var result = await sender.Send(new ListParticipantsByEventQuery(organisation, language), ct);
        return result.ToApiResult(httpContext);
    }
}
