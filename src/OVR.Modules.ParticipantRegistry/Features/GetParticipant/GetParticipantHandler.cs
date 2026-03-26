using ErrorOr;
using MediatR;
using OVR.Modules.ParticipantRegistry.Persistence;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public sealed class GetParticipantHandler(IParticipantRepository repository)
    : IRequestHandler<GetParticipantQuery, ErrorOr<ParticipantResponse>>
{
    public async Task<ErrorOr<ParticipantResponse>> Handle(
        GetParticipantQuery request, CancellationToken cancellationToken)
    {
        var participant = await repository.GetByIdAsync(request.ParticipantId, cancellationToken);
        if (participant is null)
            return Errors.ParticipantErrors.NotFound(request.ParticipantId);

        return new ParticipantResponse(
            participant.Id,
            participant.BiographicData.GivenName,
            participant.BiographicData.FamilyName,
            participant.BiographicData.BirthDate,
            participant.BiographicData.Organisation.Code,
            participant.BiographicData.Gender.Value,
            participant.Functions.Select(f =>
                new FunctionResponse(f.Function, f.Discipline, f.IsMain)).ToList(),
            participant.PrintName, participant.PrintInitialName,
            participant.TvName, participant.TvInitialName, participant.TvFamilyName,
            participant.PscbName, participant.PscbShortName, participant.PscbLongName,
            participant.PhotoUrl,
            participant.CreatedAt, participant.UpdatedAt);
    }
}
