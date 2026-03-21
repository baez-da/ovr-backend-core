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
        var participants = await repository.FindByOrganisationAsync(request.Organisation, cancellationToken);

        var responses = participants.Select(p => new ParticipantResponse(
            p.Id,
            p.Description.GivenName,
            p.Description.FamilyName,
            p.Description.Gender.Value,
            p.Description.BirthDate,
            p.Description.Organisation.Code,
            p.Functions.Select(f => new FunctionResponse(f.FunctionId, f.DisciplineCode, f.IsMain)).ToList(),
            p.PrintName,
            p.PrintInitialName,
            p.TvName,
            p.TvInitialName,
            p.TvFamilyName,
            p.PscbName,
            p.PscbShortName,
            p.PscbLongName,
            p.CreatedAt,
            p.UpdatedAt)).ToList();

        return responses;
    }
}
