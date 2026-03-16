using ErrorOr;
using MediatR;
using OVR.Modules.ParticipantRegistry.Features.GetParticipant;

namespace OVR.Modules.ParticipantRegistry.Features.ListParticipantsByEvent;

public sealed record ListParticipantsByEventQuery(string Noc) : IRequest<ErrorOr<IReadOnlyList<ParticipantResponse>>>;
