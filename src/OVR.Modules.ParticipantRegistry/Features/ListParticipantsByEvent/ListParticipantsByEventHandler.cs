using ErrorOr;
using MediatR;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;
using OVR.Modules.ParticipantRegistry.Persistence;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByEvent;

public sealed class ListParticipantsByEventHandler(IParticipantRepository repository)
    : IRequestHandler<ListParticipantsByEventQuery, ErrorOr<IReadOnlyList<ParticipantResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<ParticipantResponse>>> Handle(
        ListParticipantsByEventQuery request,
        CancellationToken cancellationToken)
    {
        var participants = await repository.FindByNocAsync(request.Noc, cancellationToken);

        var responses = participants.Select(p => new ParticipantResponse(
            p.Id,
            p.Type.ToString(),
            p.Description.GivenName,
            p.Description.FamilyName,
            p.Description.Gender.Value,
            p.Description.BirthDate,
            p.Description.Organisation.Code,
            p.PrintName,
            p.TvName,
            p.CreatedAt,
            p.UpdatedAt)).ToList();

        return responses;
    }
}
