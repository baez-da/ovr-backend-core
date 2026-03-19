using FluentAssertions;
using OVR.Modules.ParticipantRegistry.Domain.NameSystem;

namespace OVR.Modules.ParticipantRegistry.Tests.Domain.NameSystem;

public class OdfNameBuilderTests
{
    private readonly OdfNameBuilder _builder = new();

    // PrintName
    [Fact] public void BuildPrintName_StandardName() => _builder.BuildPrintName("Smith", "John").Should().Be("SMITH John");
    [Fact] public void BuildPrintName_McPrefix() => _builder.BuildPrintName("McPherson", "Liz").Should().Be("McPHERSON Liz");
    [Fact] public void BuildPrintName_Particle() => _builder.BuildPrintName("de Castella", "Robert").Should().Be("de CASTELLA Robert");
    [Fact] public void BuildPrintName_NullGivenName() => _builder.BuildPrintName("Pele", null).Should().Be("PELE");
    [Fact] public void BuildPrintName_Accented() => _builder.BuildPrintName("López", "María").Should().Be("LOPEZ Maria");

    // PrintInitialName
    [Fact] public void BuildPrintInitialName_Standard() => _builder.BuildPrintInitialName("Smith", "John").Should().Be("SMITH J");
    [Fact] public void BuildPrintInitialName_CompoundGiven() => _builder.BuildPrintInitialName("Jones", "Anne-Marie").Should().Be("JONES AM");
    [Fact] public void BuildPrintInitialName_NullGiven() => _builder.BuildPrintInitialName("Pele", null).Should().Be("PELE");

    // TVName
    [Fact] public void BuildTvName_Standard() => _builder.BuildTvName("Smith", "John").Should().Be("John SMITH");
    [Fact] public void BuildTvName_AsianNoc_JPN() => _builder.BuildTvName("Tanaka", "Yuki", "JPN").Should().Be("TANAKA Yuki");
    [Fact] public void BuildTvName_AsianNoc_CHN() => _builder.BuildTvName("Wang", "Lei", "CHN").Should().Be("WANG Lei");
    [Fact] public void BuildTvName_NonAsianNoc() => _builder.BuildTvName("Smith", "John", "USA").Should().Be("John SMITH");
    [Fact] public void BuildTvName_NullGiven() => _builder.BuildTvName("Pele", null, "BRA").Should().Be("PELE");
    [Fact] public void BuildTvName_NullGiven_AsianNoc() => _builder.BuildTvName("Pele", null, "JPN").Should().Be("PELE");

    // TVInitialName
    [Fact] public void BuildTvInitialName_Standard() => _builder.BuildTvInitialName("Smith", "John").Should().Be("J. SMITH");
    [Fact] public void BuildTvInitialName_AsianNoc() => _builder.BuildTvInitialName("Tanaka", "Yuki", "JPN").Should().Be("TANAKA Y.");
    [Fact] public void BuildTvInitialName_NullGiven() => _builder.BuildTvInitialName("Pele", null).Should().Be("PELE");

    // TVFamilyName
    [Fact] public void BuildTvFamilyName_Standard() => _builder.BuildTvFamilyName("Smith").Should().Be("SMITH");
    [Fact] public void BuildTvFamilyName_McPrefix() => _builder.BuildTvFamilyName("McBain").Should().Be("McBAIN");
    [Fact] public void BuildTvFamilyName_Particle() => _builder.BuildTvFamilyName("de Castella").Should().Be("de CASTELLA");

    // PSCBName
    [Fact] public void BuildPscbName_Standard() => _builder.BuildPscbName("Smith", "John").Should().Be("SMITH John");
    [Fact] public void BuildPscbName_NullGiven() => _builder.BuildPscbName("Pele", null).Should().Be("PELE");

    // PSCBShortName
    [Fact] public void BuildPscbShortName_Standard() => _builder.BuildPscbShortName("Smith", "John").Should().Be("SMITH J");
    [Fact] public void BuildPscbShortName_NullGiven() => _builder.BuildPscbShortName("Pele", null).Should().Be("PELE");

    // PSCBLongName
    [Fact] public void BuildPscbLongName_Standard() => _builder.BuildPscbLongName("Smith", "John").Should().Be("SMITH John");

    // Truncation
    [Fact] public void BuildTvFamilyName_LongName() => _builder.BuildTvFamilyName("Parris-Washington").Should().Be("PARRIS-WASHINGTON");
    [Fact] public void BuildPrintInitialName_LongFamily()
    {
        _builder.BuildPrintInitialName("Kooperen-Schmoranzer", "John")
            .Should().HaveLength(18).And.EndWith(".");
    }

    // Hyphenated
    [Fact] public void BuildPrintName_HyphenatedFamily() =>
        _builder.BuildPrintName("Parris-Washington", "Christine").Should().Be("PARRIS-WASHINGTON Christine");
}
