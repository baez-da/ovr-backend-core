using FluentAssertions;
using OVR.Modules.DataEntry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.DataEntry.Tests.Domain;

public class CompetitorTests
{
    [Fact]
    public void Record_Equality_ByValue()
    {
        var a = new Competitor(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.Create("ESP"), null);
        var b = new Competitor(1, ParticipantId.Create("NOC-ESP-0001"), null, 1,
            Organisation.Create("ESP"), null);

        a.Should().Be(b);
    }
}
