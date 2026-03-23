using ErrorOr;
using MediatR;
using OVR.Modules.CommonCodes.Contracts;
using OVR.Modules.ParticipantRegistry.Persistence;
using OVR.Modules.ParticipantRegistry.Services;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public sealed class GetParticipantHandler(
    IParticipantRepository repository,
    ParticipantEnricher enricher)
    : IRequestHandler<GetParticipantQuery, ErrorOr<ParticipantResponse>>
{
    public async Task<ErrorOr<ParticipantResponse>> Handle(
        GetParticipantQuery request, CancellationToken cancellationToken)
    {
        var participant = await repository.GetByIdAsync(request.ParticipantId, cancellationToken);
        if (participant is null)
            return Errors.ParticipantErrors.NotFound(request.ParticipantId);

        var org = await enricher.EnrichCodeAsync(
            CommonCodeTypes.Organisation, participant.BiographicData.Organisation.Code, request.Language, cancellationToken);
        var gender = await enricher.EnrichCodeAsync(
            CommonCodeTypes.PersonGender, participant.BiographicData.Gender.Value, request.Language, cancellationToken);
        var functions = await enricher.EnrichFunctionsAsync(
            participant.Functions, request.Language, cancellationToken);

        return new ParticipantResponse(
            participant.Id,
            participant.BiographicData.GivenName,
            participant.BiographicData.FamilyName,
            participant.BiographicData.BirthDate,
            org, gender, functions,
            participant.PrintName, participant.PrintInitialName,
            participant.TvName, participant.TvInitialName, participant.TvFamilyName,
            participant.PscbName, participant.PscbShortName, participant.PscbLongName,
            participant.PhotoUrl,
            participant.CreatedAt, participant.UpdatedAt);
    }
}
