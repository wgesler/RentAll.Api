using ImageMagick;

namespace RentAll.Infrastructure.Services;

/// <summary>
/// Downscales large raster images using ImageMagick. Skips PDF, SVG, and non-positive limits.
/// </summary>
internal static class RasterImageDimensionLimiter
{
    // GIF omitted: resizing would drop animation frames; size cap still applies to uploads.
    private static bool IsRasterImageExtension(string extension) =>
        extension is ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp" or ".tif" or ".tiff";

    /// <summary>
    /// Returns a stream to upload: either <paramref name="input"/> repositioned to 0, or a new stream if resized.
    /// When a new stream is returned, the caller should dispose <paramref name="input"/>.
    /// </summary>
    public static MemoryStream LimitDimensions(MemoryStream input, string fileExtension, int maxDimensionPixels)
    {
        if (maxDimensionPixels <= 0 || !IsRasterImageExtension(fileExtension))
        {
            input.Position = 0;
            return input;
        }

        input.Position = 0;
        using var image = new MagickImage(input);
        input.Position = 0;

        if (image.Width <= (uint)maxDimensionPixels && image.Height <= (uint)maxDimensionPixels)
        {
            input.Position = 0;
            return input;
        }

        image.Resize(new MagickGeometry($"{maxDimensionPixels}x{maxDimensionPixels}>"));
        var output = new MemoryStream();
        image.Write(output);
        output.Position = 0;
        return output;
    }
}
