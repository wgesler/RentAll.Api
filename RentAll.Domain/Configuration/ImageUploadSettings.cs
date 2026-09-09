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

    /// <summary>
    /// When true, <see cref="RentAll.Domain.Enums.ImageType.Photos"/> uploads are resized and re-encoded as JPEG to match UI listing compression.
    /// </summary>
    public bool AggressivePhotoCompressionEnabled { get; set; } = true;

    /// <summary>
    /// Longest side for listing/property photos after aggressive compression.
    /// </summary>
    public int PhotoCompressionMaxDimensionPixels { get; set; } = 1800;

    /// <summary>
    /// Target maximum encoded JPEG size in bytes for aggressive photo compression.
    /// </summary>
    public long PhotoCompressionTargetMaxBytes { get; set; } = 500 * 1024;

    /// <summary>
    /// Preferred minimum encoded JPEG size; compression stops early when within the target range.
    /// </summary>
    public long PhotoCompressionTargetMinBytes { get; set; } = 150 * 1024;

    public int PhotoCompressionInitialQuality { get; set; } = 82;

    public int PhotoCompressionMinQuality { get; set; } = 50;

    public int PhotoCompressionQualityStep { get; set; } = 10;

    public double PhotoCompressionScaleStep { get; set; } = 0.85;

    public int PhotoCompressionMaxAttempts { get; set; } = 8;
}
