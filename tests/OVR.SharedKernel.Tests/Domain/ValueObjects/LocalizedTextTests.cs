using FluentAssertions;
using OVR.SharedKernel.Domain.ValueObjects;

namespace OVR.SharedKernel.Tests.Domain.ValueObjects;

public class LocalizedTextTests
{
    [Fact]
    public void Create_WithLongOnly_ShouldSucceed()
    {
        var text = LocalizedText.Create("Boxing");

        text.Long.Should().Be("Boxing");
        text.Short.Should().BeNull();
    }

    [Fact]
    public void Create_WithLongAndShort_ShouldSucceed()
    {
        var text = LocalizedText.Create("Boxing", "BOX");

        text.Long.Should().Be("Boxing");
        text.Short.Should().Be("BOX");
    }

    [Fact]
    public void Create_WithWhitespaceLong_ShouldThrow()
    {
        var act = () => LocalizedText.Create("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTrimValues()
    {
        var text = LocalizedText.Create("  Boxing  ", "  BOX  ");

        text.Long.Should().Be("Boxing");
        text.Short.Should().Be("BOX");
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var a = LocalizedText.Create("Boxing", "BOX");
        var b = LocalizedText.Create("Boxing", "BOX");

        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var a = LocalizedText.Create("Boxing", "BOX");
        var b = LocalizedText.Create("Boxeo", "BOX");

        a.Should().NotBe(b);
    }
}
