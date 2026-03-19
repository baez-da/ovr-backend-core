using FluentAssertions;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;

namespace OVR.Modules.ParticipantRegistry.Tests.Domain.NameSystem;

public class NameNormalizationTests
{
    [Theory]
    [InlineData("MÜLLER", "MULLER")]
    [InlineData("LÓPEZ", "LOPEZ")]
    [InlineData("BJÖRK", "BJORK")]
    [InlineData("FAÇADE", "FACADE")]
    [InlineData("SEÑOR", "SENOR")]
    [InlineData("STRAßE", "STRAssE")]
    [InlineData("straße", "strasse")]
    [InlineData("Ælfred", "AElfred")]
    [InlineData("Þórsson", "THorsson")]
    [InlineData("þórsson", "thorsson")]
    [InlineData("SMITH", "SMITH")]
    [InlineData("O'Brien", "O'Brien")]
    [InlineData("", "")]
    public void ToAscii_ShouldConvertAccentedCharacters(string input, string expected)
    {
        var result = NameNormalization.ToAscii(input);
        result.Should().Be(expected);
    }
}
