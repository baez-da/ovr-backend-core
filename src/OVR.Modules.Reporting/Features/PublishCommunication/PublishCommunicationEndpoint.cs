using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Reporting.Features.PublishCommunication;

public static class PublishCommunicationEndpoint
{
    public static async Task<IResult> Handle(
        PublishCommunicationCommand command,
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
