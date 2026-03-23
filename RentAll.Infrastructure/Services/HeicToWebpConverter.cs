using ImageMagick;

namespace RentAll.Infrastructure.Services;

/// <summary>
/// Converts HEIC/HEIF uploads to WebP before persistence. Other formats are unchanged.
/// </summary>
internal static class HeicToWebpConverter
{
    private static readonly HashSet<string> HeicExtensions = new(StringComparer.OrdinalIgnoreCase) { ".heic", ".heif" };

    public static bool IsHeic(string fileExtension, string contentType)
    {
        if (HeicExtensions.Contains(fileExtension))
            return true;
        return string.Equals(contentType, "image/heic", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "image/heif", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Copies <paramref name="input"/> to a new memory stream and encodes as WebP (quality 82).
    /// </summary>
    public static async Task<MemoryStream> ConvertToWebpAsync(Stream input, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        return await Task.Run(() =>
        {
            using var image = new MagickImage(buffer);
            image.Format = MagickFormat.WebP;
            image.Quality = 82;
            var output = new MemoryStream();
            image.Write(output);
            output.Position = 0;
            return output;
        }, cancellationToken).ConfigureAwait(false);
    }
}
