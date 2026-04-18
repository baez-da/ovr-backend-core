using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.Modules.CompetitionConfig.Errors;
using OVR.Modules.CompetitionConfig.Persistence;
using OVR.SharedKernel.Contracts;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Features.CreateEvent;

public sealed class CreateEventHandler(
    IEventRepository repository,
    IPublisher publisher,
    ICommonCodeCache cache)
    : IRequestHandler<CreateEventCommand, ErrorOr<CreateEventResponse>>
{
    public async Task<ErrorOr<CreateEventResponse>> Handle(
        CreateEventCommand request,
        CancellationToken ct)
    {
        if (!cache.Exists(CommonCodeTypes.Discipline, request.Discipline))
            return CompetitionConfigErrors.InvalidDiscipline(request.Discipline);

        if (!cache.Exists(CommonCodeTypes.Event, request.EventCode))
            return CompetitionConfigErrors.InvalidEventCode(request.EventCode);

        var rscString =
            request.Discipline
            + request.Gender
            + request.EventCode.PadRight(8, '-')
            + (request.Modifier?.PadRight(10, '-') ?? new string('-', 10))
            + new string('-', 12);

        var rsc = Rsc.Create(rscString);
        var gender = Gender.FromCode(request.Gender);

        var existing = await repository.GetByRscAsync(rsc.Value, ct);
        if (existing is not null)
            return CompetitionConfigErrors.EventAlreadyExists(rsc.Value);

        var evt = Event.Create(rsc, request.Discipline, gender, request.EventCode, request.Modifier, request.Name);
        await repository.AddAsync(evt, ct);

        foreach (var e in evt.DomainEvents)
            await publisher.Publish(e, ct);
        evt.ClearDomainEvents();

        return new CreateEventResponse(evt.Id, evt.Name, evt.CreatedAt);
    }
}
