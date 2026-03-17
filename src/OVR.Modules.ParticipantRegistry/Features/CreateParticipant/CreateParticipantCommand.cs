using ErrorOr;
using MediatR;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public sealed record CreateParticipantCommand(
    string ParticipantId,
    string Type,
    string GivenName,
    string FamilyName,
    string GenderCode,
    DateOnly? BirthDate,
    string Organisation) : IRequest<ErrorOr<CreateParticipantResponse>>;

public sealed record CreateParticipantResponse(
    string Id,
    string PrintName,
    DateTime CreatedAt);
