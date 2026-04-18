using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class PhaseTests
{
    [Fact]
    public void Create_WithValidValues_SetsProperties()
    {
        var phase = Phase.CreateInternal(PhaseCodes.EighthFinals, order: 0, unitCount: 8);

        phase.Id.Should().Be(PhaseCodes.EighthFinals);
        phase.Code.Should().Be(PhaseCodes.EighthFinals);
        phase.Order.Should().Be(0);
        phase.UnitCount.Should().Be(8);
    }

    [Fact]
    public void Create_WithNegativeOrder_Throws()
    {
        Action act = () => Phase.CreateInternal(PhaseCodes.Final, order: -1, unitCount: 1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithZeroOrNegativeUnitCount_Throws()
    {
        Action act = () => Phase.CreateInternal(PhaseCodes.Final, order: 0, unitCount: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
