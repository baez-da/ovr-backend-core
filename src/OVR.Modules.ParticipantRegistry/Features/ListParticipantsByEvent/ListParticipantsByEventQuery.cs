using ErrorOr;
using MediatR;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByEvent;

public sealed record ListParticipantsByEventQuery(string Organisation) : IRequest<ErrorOr<IReadOnlyList<ParticipantResponse>>>;
