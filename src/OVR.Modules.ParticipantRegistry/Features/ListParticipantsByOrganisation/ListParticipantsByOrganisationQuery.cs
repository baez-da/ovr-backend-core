using ErrorOr;
using MediatR;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByOrganisation;

public sealed record ListParticipantsByOrganisationQuery(
    string Organisation,
    string Language) : IRequest<ErrorOr<IReadOnlyList<ParticipantResponse>>>;
