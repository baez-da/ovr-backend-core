using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.CoachAssignment.Features.AssignCoach;

public static class AssignCoachEndpoint
{
    public static async Task<IResult> Handle(
        AssignCoachCommand command,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult($"/api/coach-assignments/{command.ParticipantId}_{command.EventRsc}");
    }
}
