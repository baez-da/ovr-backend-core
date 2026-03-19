using FluentAssertions;
using OVR.Modules.ParticipantRegistry.Domain;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.Modules.ParticipantRegistry.Tests.Domain;

public class DescriptionTests
{
    private static readonly Gender TestGender = Gender.FromCode("M");
    private static readonly Organisation TestOrg = Organisation.Create("USA");

    [Fact]
    public void Create_WithNullGivenName_ShouldSucceed()
    {
        var desc = Description.Create(null, "Pele", TestGender, null, TestOrg);
        desc.GivenName.Should().BeNull();
        desc.FamilyName.Should().Be("Pele");
    }

    [Fact]
    public void Create_WithValidGivenName_ShouldTrimAndSet()
    {
        var desc = Description.Create("  John  ", "Smith", TestGender, null, TestOrg);
        desc.GivenName.Should().Be("John");
        desc.FamilyName.Should().Be("Smith");
    }

    [Fact]
    public void Create_WithWhitespaceOnlyGivenName_ShouldThrow()
    {
        var act = () => Description.Create("   ", "Smith", TestGender, null, TestOrg);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyFamilyName_ShouldThrow()
    {
        var act = () => Description.Create("John", "", TestGender, null, TestOrg);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_WithBothNullGivenNames_ShouldBeEqual()
    {
        var desc1 = Description.Create(null, "Pele", TestGender, null, TestOrg);
        var desc2 = Description.Create(null, "Pele", TestGender, null, TestOrg);
        desc1.Should().Be(desc2);
    }
}
