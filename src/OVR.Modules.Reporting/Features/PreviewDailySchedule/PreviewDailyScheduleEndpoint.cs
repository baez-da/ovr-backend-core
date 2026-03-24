using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Reporting.Features.PreviewDailySchedule;

public static class PreviewDailyScheduleEndpoint
{
    public static async Task<IResult> Handle(
        PreviewDailyScheduleQuery query,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(query, ct);
        return result.Match(
            report => Results.File(report.Pdf, "application/pdf", $"{report.Metadata.OrisCode}.pdf"),
            errors => errors.ToApiError(httpContext));
    }
}
