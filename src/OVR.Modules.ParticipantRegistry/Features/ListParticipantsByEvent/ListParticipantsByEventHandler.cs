using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.Modules.ParticipantRegistry.Services;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByEvent;

public sealed class ListParticipantsByEventHandler(
    IParticipantRepository repository,
    ParticipantEnricher enricher)
    : IRequestHandler<ListParticipantsByEventQuery, ErrorOr<IReadOnlyList<ParticipantResponse>>>
{
    public async Task<ErrorOr<IReadOnlyList<ParticipantResponse>>> Handle(
        ListParticipantsByEventQuery request, CancellationToken cancellationToken)
    {
        var participants = await repository.FindByOrganisationAsync(request.Organisation, cancellationToken);
        var responses = new List<ParticipantResponse>(participants.Count);

        foreach (var p in participants)
        {
            var org = await enricher.EnrichCodeAsync(
                CommonCodeTypes.Organisation, p.BiographicData.Organisation.Code, request.Language, cancellationToken);
            var gender = await enricher.EnrichCodeAsync(
                CommonCodeTypes.PersonGender, p.BiographicData.Gender.Value, request.Language, cancellationToken);
            var functions = await enricher.EnrichFunctionsAsync(
                p.Functions, request.Language, cancellationToken);

            responses.Add(new ParticipantResponse(
                p.Id, p.BiographicData.GivenName, p.BiographicData.FamilyName,
                p.BiographicData.BirthDate, org, gender, functions,
                p.PrintName, p.PrintInitialName,
                p.TvName, p.TvInitialName, p.TvFamilyName,
                p.PscbName, p.PscbShortName, p.PscbLongName,
                p.PhotoUrl, p.CreatedAt, p.UpdatedAt));
        }
        return responses;
    }
}
