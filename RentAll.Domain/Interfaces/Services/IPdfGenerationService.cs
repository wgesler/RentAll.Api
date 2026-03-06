namespace RentAll.Domain.Interfaces.Services;

public interface IPdfGenerationService
{
    Task<byte[]> ConvertHtmlToPdfAsync(string htmlContent);

    Task<string> ConvertHtmlToPdfBase64Async(string htmlContent);
}

public class PdfOptions
{
    public int? Width { get; set; } // in pixels
    public int? Height { get; set; } // in pixels
    public string? Format { get; set; } // A4, Letter, etc.
    public PdfMargins? Margins { get; set; }
    public bool PrintBackground { get; set; } = true;
    public bool Landscape { get; set; } = false;

    /// <summary>
    /// When true (default), external image URLs (http/https) and relative URLs (if BaseUrl is set)
    /// are fetched and inlined as data URLs so the resulting PDF has no external references.
    /// </summary>
    public bool InlineExternalImages { get; set; } = true;

    /// <summary>
    /// Optional base URL used to resolve relative image URLs when InlineExternalImages is true.
    /// </summary>
    public string? BaseUrl { get; set; }
}

public class PdfMargins
{
    public string? Top { get; set; } // e.g., "1cm", "20px"
    public string? Right { get; set; }
    public string? Bottom { get; set; }
    public string? Left { get; set; }
}


