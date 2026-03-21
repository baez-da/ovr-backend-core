using FluentAssertions;
using OVR.Modules.ParticipantRegistry.Domain;

namespace OVR.Modules.ParticipantRegistry.Tests.Domain;

public class ParticipantFunctionTests
{
    [Fact]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var fn = ParticipantFunction.Create("COACH", "VVO", true);
        fn.FunctionId.Should().Be("COACH");
        fn.DisciplineCode.Should().Be("VVO");
        fn.IsMain.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldTrimValues()
    {
        var fn = ParticipantFunction.Create("  COACH  ", "  VVO  ", false);
        fn.FunctionId.Should().Be("COACH");
        fn.DisciplineCode.Should().Be("VVO");
    }

    [Fact]
    public void Create_WithEmptyFunctionId_ShouldThrow()
    {
        var act = () => ParticipantFunction.Create("", "VVO", true);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyDisciplineCode_ShouldThrow()
    {
        var act = () => ParticipantFunction.Create("COACH", "", true);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_SameFunctionAndDiscipline_ShouldBeEqual()
    {
        var fn1 = ParticipantFunction.Create("COACH", "VVO", true);
        var fn2 = ParticipantFunction.Create("COACH", "VVO", false);
        fn1.Should().Be(fn2);
    }

    [Fact]
    public void Equality_DifferentFunction_ShouldNotBeEqual()
    {
        var fn1 = ParticipantFunction.Create("COACH", "VVO", true);
        var fn2 = ParticipantFunction.Create("AST_COA", "VVO", true);
        fn1.Should().NotBe(fn2);
    }

    [Fact]
    public void Equality_DifferentDiscipline_ShouldNotBeEqual()
    {
        var fn1 = ParticipantFunction.Create("COACH", "VVO", true);
        var fn2 = ParticipantFunction.Create("COACH", "BVO", true);
        fn1.Should().NotBe(fn2);
    }
}
