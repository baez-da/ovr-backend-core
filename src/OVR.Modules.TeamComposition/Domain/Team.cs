using OVR.SharedKernel.Domain.Primitives;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.TeamComposition.Domain;

public sealed class Team : AggregateRoot<string>
{
    public TeamId TeamId { get; private set; } = null!;
    public Noc Organisation { get; private set; } = null!;
    public Rsc EventRsc { get; private set; } = null!;
    private readonly List<string> _memberIds = [];
    public IReadOnlyList<string> MemberIds => _memberIds.AsReadOnly();

    private Team() { }

    public static Team Create(TeamId teamId, Noc organisation, Rsc eventRsc)
    {
        return new Team
        {
            Id = teamId.Value,
            TeamId = teamId,
            Organisation = organisation,
            EventRsc = eventRsc
        };
    }

    public void AddMember(string participantId)
    {
        if (!_memberIds.Contains(participantId))
            _memberIds.Add(participantId);
    }
}
