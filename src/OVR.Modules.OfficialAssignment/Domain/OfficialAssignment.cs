using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.OfficialAssignment.Domain;

public sealed class OfficialAssignmentEntity : AggregateRoot<string>
{
    public string ParticipantId { get; private set; } = string.Empty;
    public Rsc UnitRsc { get; private set; } = null!;
    public string Function { get; private set; } = string.Empty;

    private OfficialAssignmentEntity() { }

    public static OfficialAssignmentEntity Create(string participantId, Rsc unitRsc, string function)
    {
        return new OfficialAssignmentEntity
        {
            Id = $"{participantId}_{unitRsc.Value}",
            ParticipantId = participantId,
            UnitRsc = unitRsc,
            Function = function
        };
    }
}
