using FluentAssertions;
using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Domain;

public class UnitResultAggregateTests
{
    protected static Rsc MakeUnitRsc() =>
        Rsc.Create("BOXM57KG--------------8FNL0001----");

    protected static Competitor Red() =>
        new(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.Create("ESP"), null);

    protected static Competitor Blue() =>
        new(2, ParticipantId.Create("NOC-POL-0014"), null, 8,
            Organisation.Create("POL"), null);

    [Fact]
    public void CreateForFirstRound_WithValidCompetitors_SucceedsInStartList()
    {
        var rsc = MakeUnitRsc();
        var result = UnitResult.CreateForFirstRound(rsc, Red(), Blue());

        result.IsError.Should().BeFalse();
        var ur = result.Value;
        ur.Status.Should().Be(ResultStatus.StartList);
        ur.Competitors.Should().HaveCount(2);
        ur.Competitors[0].SortOrder.Should().Be(1);
        ur.Competitors[1].SortOrder.Should().Be(2);
    }

    [Fact]
    public void CreateForFirstRound_WithDuplicateSortOrder_ReturnsError()
    {
        var rsc = MakeUnitRsc();
        var red = Red();
        var redAgain = red with { SortOrder = 1 };

        var result = UnitResult.CreateForFirstRound(rsc, red, redAgain);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("DataEntry.InvalidCompetitors");
    }

    [Fact]
    public void CreateForFirstRound_WithWrongSortOrderValues_ReturnsError()
    {
        var rsc = MakeUnitRsc();
        var a = Red() with { SortOrder = 3 };
        var b = Blue() with { SortOrder = 4 };

        var result = UnitResult.CreateForFirstRound(rsc, a, b);

        result.IsError.Should().BeTrue();
    }
}
