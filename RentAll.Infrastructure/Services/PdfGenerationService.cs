using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using RentAll.Domain.Interfaces.Services;

namespace RentAll.Infrastructure.Services;

public class PdfGenerationService : IPdfGenerationService, IDisposable
{
    private readonly ILogger<PdfGenerationService> _logger;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserSemaphore = new SemaphoreSlim(1, 1);

    public PdfGenerationService(ILogger<PdfGenerationService> logger)
    {
        _logger = logger;
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        await _browserSemaphore.WaitAsync();
        try
        {
            if (_browser == null || !_browser.IsConnected)
            {
                // Download browser if not already downloaded
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();

                var launchOptions = new LaunchOptions
                {
                    Headless = true,
                    Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" } // For Linux/Docker compatibility
                };

                _browser = await Puppeteer.LaunchAsync(launchOptions);
            }

            return _browser;
        }
        finally
        {
            _browserSemaphore.Release();
        }
    }

    public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent)
    {
        return await ConvertHtmlToPdfAsync(htmlContent, null);
    }

    public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, Domain.Interfaces.Services.PdfOptions? options)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            throw new ArgumentException("HTML content cannot be null or empty", nameof(htmlContent));

        try
        {
            var browser = await GetBrowserAsync();
            await using var page = await browser.NewPageAsync();

            // Set content
            await page.SetContentAsync(htmlContent, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });

            // Configure PDF options
            var pdfOptions = new PuppeteerSharp.PdfOptions
            {
                Format = ParsePaperFormat(options?.Format ?? "Letter"),
                PrintBackground = options?.PrintBackground ?? true,
                Landscape = options?.Landscape ?? false
            };

            if (options?.Margins != null)
            {
                pdfOptions.MarginOptions = new PuppeteerSharp.Media.MarginOptions
                {
                    Top = options.Margins.Top ?? "1cm",
                    Right = options.Margins.Right ?? "1cm",
                    Bottom = options.Margins.Bottom ?? "1cm",
                    Left = options.Margins.Left ?? "1cm"
                };
            }
            else
            {
                pdfOptions.MarginOptions = new PuppeteerSharp.Media.MarginOptions
                {
                    Top = "1cm",
                    Right = "1cm",
                    Bottom = "1cm",
                    Left = "1cm"
                };
            }

            if (options?.Width.HasValue == true && options.Height.HasValue == true)
            {
                pdfOptions.Width = $"{options.Width}px";
                pdfOptions.Height = $"{options.Height}px";
            }

            // Generate PDF
            var pdfBytes = await page.PdfDataAsync(pdfOptions);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting HTML to PDF");
            throw;
        }
    }

    public async Task<string> ConvertHtmlToPdfBase64Async(string htmlContent)
    {
        var pdfBytes = await ConvertHtmlToPdfAsync(htmlContent);
        return Convert.ToBase64String(pdfBytes);
    }

    private PuppeteerSharp.Media.PaperFormat ParsePaperFormat(string format)
    {
        return format.ToUpperInvariant() switch
        {
            "LETTER" => PuppeteerSharp.Media.PaperFormat.Letter,
            "LEGAL" => PuppeteerSharp.Media.PaperFormat.Legal,
            "TABLOID" => PuppeteerSharp.Media.PaperFormat.Tabloid,
            "LEDGER" => PuppeteerSharp.Media.PaperFormat.Ledger,
            "A0" => PuppeteerSharp.Media.PaperFormat.A0,
            "A1" => PuppeteerSharp.Media.PaperFormat.A1,
            "A2" => PuppeteerSharp.Media.PaperFormat.A2,
            "A3" => PuppeteerSharp.Media.PaperFormat.A3,
            "A4" => PuppeteerSharp.Media.PaperFormat.A4,
            "A5" => PuppeteerSharp.Media.PaperFormat.A5,
            "A6" => PuppeteerSharp.Media.PaperFormat.A6,
            _ => PuppeteerSharp.Media.PaperFormat.Letter
        };
    }

    public void Dispose()
    {
        _browser?.DisposeAsync().AsTask().Wait();
        _browserSemaphore?.Dispose();
    }
}

