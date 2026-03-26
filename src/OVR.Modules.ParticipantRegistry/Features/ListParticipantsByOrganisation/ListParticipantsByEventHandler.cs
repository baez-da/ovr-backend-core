using ErrorOr;
using MediatR;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;
using OVR.Modules.ParticipantRegistry.Persistence;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByOrganisation;

public sealed class ListParticipantsByEventHandler(IParticipantRepository repository)
    : IRequestHandler<ListParticipantsByOrganisationQuery, ErrorOr<IReadOnlyList<ParticipantResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<ParticipantResponse>>> Handle(
        ListParticipantsByOrganisationQuery request, CancellationToken cancellationToken)
    {
        var participants = await repository.FindByOrganisationAsync(request.Organisation, cancellationToken);
        var responses = participants.Select(p => new ParticipantResponse(
            p.Id, p.BiographicData.GivenName, p.BiographicData.FamilyName,
            p.BiographicData.BirthDate,
            p.BiographicData.Organisation.Code,
            p.BiographicData.Gender.Value,
            p.Functions.Select(f =>
                new FunctionResponse(f.Function, f.Discipline, f.IsMain)).ToList(),
            p.PrintName, p.PrintInitialName,
            p.TvName, p.TvInitialName, p.TvFamilyName,
            p.PscbName, p.PscbShortName, p.PscbLongName,
            p.PhotoUrl, p.CreatedAt, p.UpdatedAt)).ToList();

        return responses;
    }
}
