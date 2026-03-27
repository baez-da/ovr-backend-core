using ErrorOr;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using OVR.Modules.Reporting.Errors;

namespace OVR.Modules.Reporting.Services;

internal sealed class PlaywrightPdfRenderer : IPdfRenderer, IAsyncDisposable
{
    private readonly string? _wsEndpoint;
    private readonly Margin _margin;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public PlaywrightPdfRenderer(IConfiguration configuration)
    {
        _wsEndpoint = configuration["Reporting:PlaywrightWsEndpoint"];
        _margin = new Margin
        {
            Top    = configuration["Reporting:Margins:Top"]    ?? "40mm",
            Bottom = configuration["Reporting:Margins:Bottom"] ?? "38mm",
            Left   = configuration["Reporting:Margins:Left"]   ?? "0",
            Right  = configuration["Reporting:Margins:Right"]  ?? "0"
        };
    }

    public async Task<ErrorOr<byte[]>> RenderAsync(RenderedHtml html, CancellationToken ct = default)
    {
        try
        {
            var browser = await GetBrowserAsync(ct);
            var page = await browser.NewPageAsync();
            try
            {
                // Compose full HTML page with header and footer as data
                var fullHtml = ComposeHtml(html);
                await page.SetContentAsync(fullHtml, new PageSetContentOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle
                });

                var pdfBytes = await page.PdfAsync(new PagePdfOptions
                {
                    PrintBackground = true,
                    DisplayHeaderFooter = true,
                    HeaderTemplate = html.Header,
                    FooterTemplate = html.Footer,
                    Format = "A4",
                    Margin = _margin
                });

                return pdfBytes;
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            // Reset browser on failure to allow reconnection
            await ResetBrowserAsync();
            return ReportingErrors.PdfGenerationFailed(ex.Message);
        }
    }

    private static string ComposeHtml(RenderedHtml html) =>
        $"""
         <!DOCTYPE html>
         <html>
         <head><meta charset="utf-8"/></head>
         <body>
         {html.Body}
         </body>
         </html>
         """;

    private async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
    {
        if (_browser is { IsConnected: true })
            return _browser;

        await _lock.WaitAsync(ct);
        try
        {
            if (_browser is { IsConnected: true })
                return _browser;

            // Try to dispose stale browser
            if (_browser is not null)
            {
                try { await _browser.CloseAsync(); } catch { /* ignore */ }
                _browser = null;
            }

            if (_playwright is null)
                _playwright = await Playwright.CreateAsync();

            _browser = await ConnectOrLaunchAsync(_playwright);
            return _browser;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IBrowser> ConnectOrLaunchAsync(IPlaywright playwright)
    {
        if (!string.IsNullOrEmpty(_wsEndpoint))
        {
            try
            {
                return await playwright.Chromium.ConnectAsync(_wsEndpoint);
            }
            catch
            {
                // Fall back to local browser
            }
        }

        return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    private async Task ResetBrowserAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_browser is not null)
            {
                try { await _browser.CloseAsync(); } catch { /* ignore */ }
                _browser = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_browser is not null)
        {
            try { await _browser.CloseAsync(); } catch { /* ignore */ }
        }

        _playwright?.Dispose();
        _lock.Dispose();
    }
}
