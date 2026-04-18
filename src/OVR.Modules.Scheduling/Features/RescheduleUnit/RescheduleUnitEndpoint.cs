using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.RescheduleUnit;

public static class RescheduleUnitEndpoint
{
    public static async Task<IResult> Handle(
        string unitRsc,
        RescheduleUnitBody body,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var command = new RescheduleUnitCommand(
            unitRsc,
            body.SessionCode,
            body.LocationCode,
            body.StartTime,
            body.OrderInSession,
            body.OrderInLocation,
            body.Reason);

        var result = await sender.Send(command, ct);
        return result.ToApiResult(httpContext);
    }
}

public sealed record RescheduleUnitBody(
    string SessionCode,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation,
    string? Reason);
