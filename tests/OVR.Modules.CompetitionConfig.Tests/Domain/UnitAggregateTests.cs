using FluentAssertions;
using OVR.Modules.CompetitionConfig.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.CompetitionConfig.Tests.Domain;

public class UnitAggregateTests
{
    [Fact]
    public void Create_FromUnitLevelRsc_DerivesEventRscPhaseCodeAndUnitNumber()
    {
        var rsc = Rsc.Create("BOXM57KG--------------8FNL0001----");

        var unit = Unit.Create(rsc);

        unit.Id.Should().Be("BOXM57KG--------------8FNL0001----");
        unit.Rsc.Value.Should().Be("BOXM57KG--------------8FNL0001----");
        unit.EventRsc.Value.Should().Be("BOXM57KG--------------------------");
        unit.PhaseCode.Should().Be("8FNL");
        unit.UnitNumber.Should().Be(1);
        unit.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithUnitNumber15_ParsesCorrectly()
    {
        var rsc = Rsc.Create("BOXM57KG--------------FNL-0015----");

        var unit = Unit.Create(rsc);

        unit.UnitNumber.Should().Be(15);
        unit.PhaseCode.Should().Be("FNL-");
    }

    [Fact]
    public void Create_FromEventLevelRsc_Throws()
    {
        var rsc = Rsc.Create("BOXM57KG--------------------------");

        Action act = () => Unit.Create(rsc);

        act.Should().Throw<ArgumentException>().WithMessage("*Unit*");
    }

    [Fact]
    public void Create_FromPhaseLevelRsc_Throws()
    {
        // Phase level: discipline+gender+event+phase, unit/sub dashes.
        var rsc = Rsc.Create("BOXM57KG--------------8FNL--------");

        Action act = () => Unit.Create(rsc);

        act.Should().Throw<ArgumentException>().WithMessage("*Unit*");
    }
}
