using MediatR;
using Microsoft.AspNetCore.Http;
using OVR.SharedKernel.Extensions;

namespace OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;

public static class GenerateEventStructureEndpoint
{
    public static async Task<IResult> Handle(
        string rsc,
        GenerateEventStructureBody body,
        ISender sender,
        CancellationToken ct,
        HttpContext httpContext)
    {
        var command = new GenerateEventStructureCommand(
            rsc, body.Format, body.Size, body.StartUnitNumber);

        var result = await sender.Send(command, ct);
        return result.ToApiResult(httpContext);
    }
}

public sealed record GenerateEventStructureBody(
    string Format,
    int Size,
    int StartUnitNumber = 1);
