using ErrorOr;
using MediatR;
using OVR.Modules.OfficialAssignment.Features.GetOfficialAssignment;

namespace OVR.Modules.OfficialAssignment.Features.ListOfficialsByUnit;

public sealed record ListOfficialsByUnitQuery(string RscPrefix) : IRequest<ErrorOr<IReadOnlyList<OfficialAssignmentResponse>>>;
