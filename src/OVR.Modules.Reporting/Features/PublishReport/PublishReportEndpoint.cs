using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Reporting.Features.PublishReport;

public static class PublishReportEndpoint
{
    public static async Task<IResult> Handle(
        PublishReportCommand command,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            response => response.IsNew
                ? Results.Created($"/api/reports?rsc={command.Rsc}&orisCode={command.OrisCode}", response)
                : Results.Ok(response),
            errors => errors.ToApiError(httpContext));
    }
}
