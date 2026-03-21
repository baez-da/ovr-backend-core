using ErrorOr;
using MediatR;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public sealed record GetParticipantQuery(string ParticipantId) : IRequest<ErrorOr<ParticipantResponse>>;

public sealed record ParticipantResponse(
    string Id,
    string? GivenName,
    string FamilyName,
    string GenderCode,
    DateOnly? BirthDate,
    string Organisation,
    List<FunctionResponse> Functions,
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

public sealed record FunctionResponse(
    string FunctionId,
    string DisciplineCode,
    bool IsMain);
