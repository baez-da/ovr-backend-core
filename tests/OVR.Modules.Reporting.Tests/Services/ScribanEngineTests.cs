using FluentAssertions;
using OVR.Modules.Reporting.Services;

namespace OVR.Modules.Reporting.Tests.Services;

public class ScribanEngineTests
{
    private readonly ScribanEngine _sut = new();

    private static ResolvedTemplates BuildTemplates(
        string body = "<p>{{name}}</p>",
        string header = "<h1>{{title}}</h1>",
        string footer = "<footer>{{year}}</footer>",
        string styles = "",
        Dictionary<string, string>? partials = null) =>
        new(
            BodyTemplate: body,
            HeaderTemplate: header,
            FooterTemplate: footer,
            GlobalStyles: styles,
            Partials: partials ?? new Dictionary<string, string>());

    [Fact]
    public async Task RenderAsync_WithData_RendersBodyWithInterpolation()
    {
        var templates = BuildTemplates(body: "<p>{{ name }}</p>");
        var data = new { Name = "Alice" };

        var result = await _sut.RenderAsync(templates, data, CancellationToken.None);

        result.Body.Should().Contain("Alice");
    }

    [Fact]
    public async Task RenderAsync_WithData_RendersHeaderAndFooterSeparately()
    {
        var templates = BuildTemplates(
            header: "<header>{{ event_name }}</header>",
            footer: "<footer>{{ year }}</footer>");
        var data = new { EventName = "Olympic Games", Year = 2024 };

        var result = await _sut.RenderAsync(templates, data, CancellationToken.None);

        result.Header.Should().Contain("Olympic Games");
        result.Footer.Should().Contain("2024");
    }

    [Fact]
    public async Task RenderAsync_WithGlobalStyles_InjectsStyleIntoBody()
    {
        var templates = BuildTemplates(body: "<div>content</div>", styles: "body { color: red; }");
        var data = new { };

        var result = await _sut.RenderAsync(templates, data, CancellationToken.None);

        result.Body.Should().Contain("<style>");
        result.Body.Should().Contain("body { color: red; }");
        result.Body.Should().Contain("<div>content</div>");
    }

    [Fact]
    public async Task RenderAsync_WithoutGlobalStyles_DoesNotInjectStyleTag()
    {
        var templates = BuildTemplates(body: "<div>content</div>", styles: "");
        var data = new { };

        var result = await _sut.RenderAsync(templates, data, CancellationToken.None);

        result.Body.Should().NotContain("<style>");
    }

    [Fact]
    public async Task RenderAsync_StylesNotInjectedIntoHeaderOrFooter()
    {
        var templates = BuildTemplates(
            header: "<h1>header</h1>",
            footer: "<span>footer</span>",
            styles: "body { margin: 0; }");
        var data = new { };

        var result = await _sut.RenderAsync(templates, data, CancellationToken.None);

        result.Header.Should().NotContain("<style>");
        result.Footer.Should().NotContain("<style>");
    }

    [Fact]
    public async Task RenderAsync_WithPartials_RendersPartialContent()
    {
        var partials = new Dictionary<string, string>
        {
            ["row"] = "<tr><td>{{ item }}</td></tr>"
        };
        // Scriban include syntax uses "include 'partial_name'"
        var templates = BuildTemplates(
            body: "{{ include 'row' }}",
            partials: partials);
        var data = new { Item = "Gold" };

        var result = await _sut.RenderAsync(templates, data, CancellationToken.None);

        result.Body.Should().Contain("Gold");
    }

    [Fact]
    public async Task RenderAsync_TemplateWithNoVariables_RendersStaticContent()
    {
        var templates = BuildTemplates(body: "<h1>Static Content</h1>", styles: "");
        var data = new { };

        var result = await _sut.RenderAsync(templates, data, CancellationToken.None);

        result.Body.Should().Be("<h1>Static Content</h1>");
    }
}
