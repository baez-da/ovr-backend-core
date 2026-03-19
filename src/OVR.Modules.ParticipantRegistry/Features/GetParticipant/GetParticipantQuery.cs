using ErrorOr;
using MediatR;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public sealed record GetParticipantQuery(string ParticipantId) : IRequest<ErrorOr<ParticipantResponse>>;

public sealed record ParticipantResponse(
    string Id,
    string Type,
    string? GivenName,
    string FamilyName,
    string GenderCode,
    DateOnly? BirthDate,
    string Organisation,
    string PrintName,
    string PrintInitialName,
    string TvName,
    string TvInitialName,
    string TvFamilyName,
    string PscbName,
    string PscbShortName,
    string PscbLongName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
