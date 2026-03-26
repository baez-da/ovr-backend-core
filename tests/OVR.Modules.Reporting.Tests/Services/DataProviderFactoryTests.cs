using FluentAssertions;
using NSubstitute;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Tests.Services;

public class DataProviderFactoryTests
{
    private static IReportDataProvider MakeProvider(string orisCode, string? discipline)
    {
        var provider = Substitute.For<IReportDataProvider>();
        provider.OrisCode.Returns(orisCode);
        provider.Discipline.Returns(discipline);
        return provider;
    }

    [Fact]
    public void Resolve_DisciplineSpecificProvider_ReturnsDisciplineProvider()
    {
        var generic = MakeProvider("ATH-RESULTS", null);
        var discipline = MakeProvider("ATH-RESULTS", "ATH");

        var factory = new DataProviderFactory([generic, discipline]);

        var result = factory.Resolve("ATH", "ATH-RESULTS");

        result.IsError.Should().BeFalse();
        result.Value.Provider.Should().BeSameAs(discipline);
    }

    [Fact]
    public void Resolve_NoDisciplineSpecific_FallsBackToGeneric()
    {
        var generic = MakeProvider("ATH-RESULTS", null);

        var factory = new DataProviderFactory([generic]);

        var result = factory.Resolve("ATH", "ATH-RESULTS");

        result.IsError.Should().BeFalse();
        result.Value.Provider.Should().BeSameAs(generic);
    }

    [Fact]
    public void Resolve_NullDiscipline_ReturnsGenericProvider()
    {
        var generic = MakeProvider("SWM-RESULTS", null);

        var factory = new DataProviderFactory([generic]);

        var result = factory.Resolve(null, "SWM-RESULTS");

        result.IsError.Should().BeFalse();
        result.Value.Provider.Should().BeSameAs(generic);
    }

    [Fact]
    public void Resolve_NoMatchingProvider_ReturnsDataProviderNotFoundError()
    {
        var factory = new DataProviderFactory(Array.Empty<IReportDataProvider>());

        var result = factory.Resolve("ATH", "ATH-RESULTS");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Reporting.DataProviderNotFound");
    }

    [Fact]
    public void Resolve_WrongOrisCode_ReturnsDataProviderNotFoundError()
    {
        var provider = MakeProvider("SWM-RESULTS", null);
        var factory = new DataProviderFactory([provider]);

        var result = factory.Resolve(null, "ATH-RESULTS");

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Reporting.DataProviderNotFound");
    }
}
