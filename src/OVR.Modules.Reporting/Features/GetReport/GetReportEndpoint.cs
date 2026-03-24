using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Reporting.Features.GetReport;

public static class GetReportEndpoint
{
    public static async Task<IResult> Handle(
        [FromQuery] string rsc,
        [FromQuery] string orisCode,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(new GetReportQuery(rsc, orisCode), ct);
        return result.ToApiResult(httpContext);
    }
}
