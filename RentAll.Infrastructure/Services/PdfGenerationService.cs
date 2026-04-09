using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using RentAll.Domain.Interfaces.Services;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RentAll.Infrastructure.Services;

public class PdfGenerationService : IPdfGenerationService, IDisposable
{
    private readonly ILogger<PdfGenerationService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Matches img tags and captures the src attribute value (supports single and double quotes; src in any order).
    /// </summary>
    private static readonly Regex ImgSrcRegex = new(
        @"<img\s[^>]*?src\s*=\s*[""']([^""']+)[""'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PdfGenerationService(
        ILogger<PdfGenerationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    private async Task<IBrowser> GetBrowserAsync()
    {
        await _browserSemaphore.WaitAsync();
        try
        {
            if (_browser == null || !_browser.IsConnected)
            {
                var executablePath = ResolveChromeExecutablePath();

                _logger.LogDebug("Launching browser from path: {ExecutablePath}", executablePath);

                var launchOptions = new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = executablePath,
                    Timeout = 60000,
                    Args = new[]

                    {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-gpu"
                }
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

    private static string ResolveChromeExecutablePath()
    {
        var envPath = Environment.GetEnvironmentVariable("CHROME_BIN");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            // Support either a full executable path or a PATH command (e.g. "google-chrome").
            var resolvedFromEnv = ResolveExecutableReference(envPath);
            if (!string.IsNullOrWhiteSpace(resolvedFromEnv))
                return resolvedFromEnv;
        }

        // Playwright Docker images keep browsers under /ms-playwright
        var msPlaywright = "/ms-playwright";
        if (Directory.Exists(msPlaywright))
        {
            try
            {
                var candidates = Directory.GetFiles(msPlaywright, "chrome", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(msPlaywright, "chrome-headless-shell", SearchOption.AllDirectories))
                    .OrderBy(p => p)
                    .ToList();

                var match = candidates.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(match))
                    return match;
            }
            catch
            {
                // Keep probing other locations instead of failing hard.
            }
        }

        // Local development fallbacks.
        // Windows: check common Chrome/Edge install locations.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var winCandidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe")
            };

            var winMatch = winCandidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(winMatch))
                return winMatch;
        }

        // Linux: common package paths.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var linuxCandidates = new[]
            {
                "/usr/bin/google-chrome",
                "/usr/bin/chromium-browser",
                "/usr/bin/chromium",
                "/snap/bin/chromium"
            };

            var linuxMatch = linuxCandidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(linuxMatch))
                return linuxMatch;
        }

        // macOS: standard app bundle path.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            const string macChromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
            if (File.Exists(macChromePath))
                return macChromePath;
        }

        throw new FileNotFoundException(
            "Could not find a Chrome/Chromium executable. Checked CHROME_BIN, /ms-playwright, and common local browser install paths.");
    }

    private static string? ResolveExecutableReference(string reference)
    {
        var trimmed = reference.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        // Absolute or relative path directly supplied.
        if (trimmed.Contains(Path.DirectorySeparatorChar) || trimmed.Contains(Path.AltDirectorySeparatorChar))
            return File.Exists(trimmed) ? trimmed : null;

        // Command name supplied; resolve from PATH.
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
            return null;

        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { ".exe", ".cmd", ".bat", string.Empty }
            : new[] { string.Empty };

        foreach (var rawDir in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var dir = rawDir.Trim();
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir, trimmed + ext);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        return null;
    }

    public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent)
    {
        return await ConvertHtmlToPdfAsync(htmlContent, null);
    }

    public async Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent, Domain.Interfaces.Services.PdfOptions? options)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            throw new ArgumentException("HTML content cannot be null or empty", nameof(htmlContent));

        var totalStart = DateTime.UtcNow;

        options ??= new Domain.Interfaces.Services.PdfOptions();

        if (options.InlineExternalImages)
        {
            var inlineStart = DateTime.UtcNow;
            htmlContent = await InlineExternalImagesAsync(htmlContent, options.BaseUrl).ConfigureAwait(false);
            _logger.LogDebug("PDF timing: InlineExternalImagesAsync = {ElapsedMs} ms", (DateTime.UtcNow - inlineStart).TotalMilliseconds);
        }

        try
        {
            var browserStart = DateTime.UtcNow;
            var browser = await GetBrowserAsync();
            _logger.LogDebug("PDF timing: GetBrowserAsync = {ElapsedMs} ms", (DateTime.UtcNow - browserStart).TotalMilliseconds);

            var newPageStart = DateTime.UtcNow;
            await using var page = await browser.NewPageAsync();
            _logger.LogDebug("PDF timing: NewPageAsync = {ElapsedMs} ms", (DateTime.UtcNow - newPageStart).TotalMilliseconds);

            var setContentStart = DateTime.UtcNow;
            await page.SetContentAsync(htmlContent, new NavigationOptions
            {
                Timeout = 60000,
                WaitUntil = new[] { WaitUntilNavigation.Load }
            });
            _logger.LogDebug("PDF timing: SetContentAsync = {ElapsedMs} ms", (DateTime.UtcNow - setContentStart).TotalMilliseconds);

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

            var pdfStart = DateTime.UtcNow;
            var pdfBytes = await page.PdfDataAsync(pdfOptions);
            var totalMs = (DateTime.UtcNow - pdfStart).TotalMilliseconds;

            _logger.LogDebug("PDF timing: PdfDataAsync = {ElapsedMs} ms", (DateTime.UtcNow - pdfStart).TotalMilliseconds);
            if (totalMs > 4000) _logger.LogInformation("SLOW PDF: Total={TotalMs}ms", totalMs);

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

    /// <summary>
    /// Fetches external image URLs (http/https) and relative URLs (if baseUrl is set),
    /// converts them to data URLs, and replaces src attributes so the HTML has no external refs.
    /// </summary>
    private async Task<string> InlineExternalImagesAsync(string html, string? baseUrl)
    {
        var matches = ImgSrcRegex.Matches(html).Cast<Match>().ToList();
        if (matches.Count == 0)
            return html;

        var urlToDataUrl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var client = _httpClientFactory.CreateClient();

        foreach (Match m in matches)
        {
            var url = m.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(url) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                continue;
            if (urlToDataUrl.ContainsKey(url))
                continue;

            var absoluteUrl = ResolveUrl(url, baseUrl);
            if (absoluteUrl == null)
                continue;

            try
            {
                var dataUrl = await FetchAsDataUrlAsync(client, absoluteUrl).ConfigureAwait(false);
                if (dataUrl != null)
                    urlToDataUrl[url] = dataUrl;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not inline image {Url}", absoluteUrl);
            }
        }

        if (urlToDataUrl.Count == 0)
            return html;

        var sb = new System.Text.StringBuilder(html.Length);
        var lastEnd = 0;
        foreach (Match m in matches)
        {
            var url = m.Groups[1].Value.Trim();
            sb.Append(html, lastEnd, m.Groups[1].Index - lastEnd);
            sb.Append(urlToDataUrl.TryGetValue(url, out var dataUrl) ? dataUrl : url);
            lastEnd = m.Groups[1].Index + m.Groups[1].Length;
        }
        sb.Append(html, lastEnd, html.Length - lastEnd);
        return sb.ToString();
    }

    private static string? ResolveUrl(string url, string? baseUrl)
    {
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;

        if (string.IsNullOrEmpty(baseUrl))
            return null;

        baseUrl = baseUrl.TrimEnd('/');
        var relative = url.TrimStart('/');
        return $"{baseUrl}/{relative}";
    }

    private static async Task<string?> FetchAsDataUrlAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        var base64 = Convert.ToBase64String(bytes);

        return $"data:{contentType};base64,{base64}";
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
