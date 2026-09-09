using ImageMagick;
using RentAll.Domain.Configuration;

namespace RentAll.Infrastructure.Services;

/// <summary>
/// Aggressive JPEG compression for property listing photos (matches UI upload targets).
/// </summary>
internal static class RasterImagePhotoCompressor
{
    public static bool IsCompressiblePhoto(string fileExtension, string contentType)
    {
        if (string.Equals(fileExtension, ".gif", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileExtension, ".svg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileExtension, ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(contentType, "image/gif", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(contentType, "image/svg+xml", StringComparison.OrdinalIgnoreCase);
        }

        return fileExtension is ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp" or ".tif" or ".tiff" or ".heic" or ".heif";
    }

    /// <summary>
    /// Returns a JPEG stream. Caller must dispose <paramref name="input"/> when a new stream is returned.
    /// </summary>
    public static MemoryStream Compress(MemoryStream input, ImageUploadSettings settings)
    {
        input.Position = 0;
        using var source = new MagickImage(input);

        source.BackgroundColor = MagickColors.White;
        source.Alpha(AlphaOption.Remove);

        var maxDimension = settings.PhotoCompressionMaxDimensionPixels;
        if (maxDimension > 0 && (source.Width > (uint)maxDimension || source.Height > (uint)maxDimension))
        {
            source.Resize(new MagickGeometry($"{maxDimension}x{maxDimension}>"));
        }

        var scale = 1.0;
        var quality = settings.PhotoCompressionInitialQuality;
        MemoryStream? bestOutput = null;

        for (var attempt = 0; attempt < settings.PhotoCompressionMaxAttempts; attempt++)
        {
            using var image = source.Clone();

            if (scale < 0.999)
            {
                var width = Math.Max(1, (int)Math.Floor(image.Width * scale));
                var height = Math.Max(1, (int)Math.Floor(image.Height * scale));
                image.Resize((uint)width, (uint)height);
            }

            image.Format = MagickFormat.Jpeg;
            image.Quality = (uint)Math.Clamp(quality, 1, 100);

            var output = new MemoryStream();
            image.Write(output);
            var size = output.Length;

            bestOutput?.Dispose();
            bestOutput = output;

            if (size <= settings.PhotoCompressionTargetMaxBytes)
            {
                if (size >= settings.PhotoCompressionTargetMinBytes
                    || (quality <= settings.PhotoCompressionMinQuality && scale <= settings.PhotoCompressionScaleStep))
                {
                    break;
                }
            }

            if (size > settings.PhotoCompressionTargetMaxBytes)
            {
                if (quality > settings.PhotoCompressionMinQuality)
                {
                    quality -= settings.PhotoCompressionQualityStep;
                    continue;
                }

                scale *= settings.PhotoCompressionScaleStep;
                quality = settings.PhotoCompressionInitialQuality;
                continue;
            }

            break;
        }

        bestOutput!.Position = 0;
        return bestOutput;
    }
}
