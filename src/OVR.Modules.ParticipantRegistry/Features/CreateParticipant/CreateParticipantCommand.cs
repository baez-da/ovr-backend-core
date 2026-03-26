using ErrorOr;
using MediatR;

namespace OVR.Modules.ParticipantRegistry.Features.CreateParticipant;

public sealed record CreateParticipantCommand(
    string? GivenName,
    string FamilyName,
    string Gender,
    DateOnly? BirthDate,
    string Organisation,
    List<FunctionDto> Functions,
    string? PhotoUrl = null) : IRequest<ErrorOr<CreateParticipantResponse>>;

public sealed record FunctionDto(string Function, string Discipline, bool IsMain);

public sealed record CreateParticipantResponse(
    string Id,
    string PrintName,
    string PrintInitialName,
    string TvName,
    string TvInitialName,
    string TvFamilyName,
    string PscbName,
    string PscbShortName,
    string PscbLongName,
    List<FunctionDto> Functions,
    DateTime CreatedAt,
    string? PhotoUrl = null);
