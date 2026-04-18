using ErrorOr;
using MediatR;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Errors;
using OVR.Modules.CompetitionConfig.Persistence;

namespace OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;

public sealed class GenerateEventStructureHandler(
    IEventRepository eventRepository,
    IUnitRepository unitRepository,
    IPublisher publisher,
    BracketGenerator generator)
    : IRequestHandler<GenerateEventStructureCommand, ErrorOr<GenerateEventStructureResponse>>
{
    public async Task<ErrorOr<GenerateEventStructureResponse>> Handle(
        GenerateEventStructureCommand request,
        CancellationToken ct)
    {
        var evt = await eventRepository.GetByRscAsync(request.EventRsc, ct);
        if (evt is null)
            return CompetitionConfigErrors.EventNotFound(request.EventRsc);

        if (!Enum.TryParse<CompetitionFormat>(request.Format, out var format))
            return CompetitionConfigErrors.UnsupportedFormat(request.Format);

        var structureResult = evt.GenerateStructure(format, request.Size, request.StartUnitNumber, generator);
        if (structureResult.IsError)
            return structureResult.Errors;

        var units = structureResult.Value.Select(Domain.Unit.Create).ToList();
        await unitRepository.AddManyAsync(units, ct);
        await eventRepository.UpdateAsync(evt, ct);

        foreach (var e in evt.DomainEvents)
            await publisher.Publish(e, ct);
        evt.ClearDomainEvents();

        return new GenerateEventStructureResponse(
            EventRsc: evt.Id,
            Format: evt.Format!.Value.ToString(),
            Size: evt.Size!.Value,
            Phases: evt.Phases
                .Select(p => new GenerateEventStructurePhase(p.Code, p.Order, p.UnitCount))
                .ToList(),
            UnitRscs: structureResult.Value.Select(r => r.Value).ToList());
    }
}
