using ErrorOr;
using MediatR;

namespace OVR.Modules.CompetitionConfig.Features.GenerateEventStructure;

public sealed record GenerateEventStructureCommand(
    string EventRsc,
    string Format,
    int Size,
    int StartUnitNumber = 1) : IRequest<ErrorOr<GenerateEventStructureResponse>>;

public sealed record GenerateEventStructureResponse(
    string EventRsc,
    string Format,
    int Size,
    IReadOnlyList<GenerateEventStructurePhase> Phases,
    IReadOnlyList<string> UnitRscs);

public sealed record GenerateEventStructurePhase(
    string Code,
    int Order,
    int UnitCount);
