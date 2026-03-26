using ErrorOr;
using MediatR;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public sealed record GetParticipantQuery(
    string ParticipantId) : IRequest<ErrorOr<ParticipantResponse>>;

public sealed record ParticipantResponse(
    string Id,
    string? GivenName,
    string FamilyName,
    DateOnly? BirthDate,
    string Organisation,
    string Gender,
    List<FunctionResponse> Functions,
    string PrintName,
    string PrintInitialName,
    string TvName,
    string TvInitialName,
    string TvFamilyName,
    string PscbName,
    string PscbShortName,
    string PscbLongName,
    string? PhotoUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record FunctionResponse(
    string Function,
    string Discipline,
    bool IsMain);
