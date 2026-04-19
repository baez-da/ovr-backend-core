using FluentAssertions;
using OVR.Modules.DataEntry.Domain;

namespace OVR.Modules.DataEntry.Tests.Domain;

public class DecisionTests
{
    [Fact]
    public void Points_Decision_HasDecisionMarkAndNoStoppage()
    {
        var d = new Decision(
            ResultType.Points, ResultCode.Wp,
            DecisionMark: "3:0", StoppageRound: null, StoppageTime: null,
            WinnerParticipantId: null);

        d.DecisionMark.Should().Be("3:0");
        d.StoppageRound.Should().BeNull();
    }
}
