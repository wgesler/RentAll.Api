namespace RentAll.Domain.Configuration;

/// <summary>
/// Limits for image uploads handled by <see cref="RentAll.Domain.Interfaces.Services.IFileService"/> SaveImageAsync.
/// </summary>
public class ImageUploadSettings
{
    /// <summary>
    /// Maximum decoded upload size in bytes (before any HEIC→WebP conversion or resize). 0 = no limit.
    /// </summary>
    public long MaxUploadBytes { get; set; } = 20 * 1024 * 1024; // 20 MB

    /// <summary>
    /// If greater than 0, raster images (not PDF/SVG) are downscaled so width and height do not exceed this value (aspect ratio preserved). Uses a "shrink only" rule.
    /// </summary>
    public int MaxImageDimensionPixels { get; set; } = 4096;
}
