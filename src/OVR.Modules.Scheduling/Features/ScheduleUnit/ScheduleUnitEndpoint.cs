using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.Scheduling.Features.ScheduleUnit;

public static class ScheduleUnitEndpoint
{
    public static async Task<IResult> Handle(
        string sessionCode,
        ScheduleUnitBody body,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var command = new ScheduleUnitCommand(
            sessionCode,
            body.UnitRsc,
            body.LocationCode,
            body.StartTime,
            body.OrderInSession,
            body.OrderInLocation);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(
            $"/api/scheduling/unit-schedules/{body.UnitRsc}", httpContext);
    }
}

public sealed record ScheduleUnitBody(
    string UnitRsc,
    string LocationCode,
    DateTime StartTime,
    int OrderInSession,
    int OrderInLocation);
