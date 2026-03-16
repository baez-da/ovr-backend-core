using ErrorOr;
using MediatR;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public sealed record GetParticipantQuery(string ParticipantId) : IRequest<ErrorOr<ParticipantResponse>>;

public sealed record ParticipantResponse(
    string Id,
    string Type,
    string GivenName,
    string FamilyName,
    string GenderCode,
    DateOnly? BirthDate,
    string Noc,
    string PrintName,
    string TvName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
