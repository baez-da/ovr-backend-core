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

    [Theory]
    [InlineData("JONES", "JONES")]
    [InlineData("MCBAIN", "McBAIN")]
    [InlineData("mcbain", "McBAIN")]
    [InlineData("DE SOUZA", "de SOUZA")]
    [InlineData("VAN DER BERG", "van der BERG")]
    [InlineData("PARRIS-WASHINGTON", "PARRIS-WASHINGTON")]
    [InlineData("MCPHERSON", "McPHERSON")]
    [InlineData("LA FONTAINE", "la FONTAINE")]
    [InlineData("VANDER WAALS", "vander WAALS")]
    [InlineData("DOS SANTOS", "dos SANTOS")]
    public void ToLimitedMixedCase_ShouldApplyOdfRules(string input, string expected)
    {
        var result = NameNormalization.ToLimitedMixedCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("JOHN", "John")]
    [InlineData("ANNE-MARIE", "Anne-Marie")]
    [InlineData("A'HERN", "A'Hern")]
    [InlineData("MCBAIN", "McBain")]
    [InlineData("DE SILVA", "de Silva")]
    [InlineData("JOSE LUIS", "Jose Luis")]
    [InlineData("O'CONNOR", "O'Connor")]
    [InlineData("", "")]
    public void ToMixedCase_ShouldApplyOdfRules(string input, string expected)
    {
        var result = NameNormalization.ToMixedCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("SMITH", 18, "SMITH")]
    [InlineData("KOOPEREN-SCHMORANZER", 18, "KOOPEREN-SCHMORAN.")]
    [InlineData("AB", 2, "AB")]
    [InlineData("ABC", 2, "A.")]
    public void Truncate_ShouldTruncateWithDot(string input, int maxLength, string expected)
    {
        var result = NameNormalization.Truncate(input, maxLength);
        result.Should().Be(expected);
    }
}
