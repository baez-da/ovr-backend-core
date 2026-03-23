using ErrorOr;
using MediatR;
using OVR.SharedKernel.Contracts;

namespace OVR.Modules.ParticipantRegistry.Features.GetParticipant;

public sealed record GetParticipantQuery(
    string ParticipantId,
    string Language) : IRequest<ErrorOr<ParticipantResponse>>;

public sealed record ParticipantResponse(
    string Id,
    string? GivenName,
    string FamilyName,
    DateOnly? BirthDate,
    LocalizedCode Organisation,
    LocalizedCode Gender,
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
    LocalizedCode Function,
    LocalizedCode Discipline,
    bool IsMain);
