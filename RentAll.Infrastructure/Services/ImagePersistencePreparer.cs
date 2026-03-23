using RentAll.Domain.Configuration;

namespace RentAll.Infrastructure.Services;

/// <summary>
/// HEIC→WebP conversion and optional dimension limiting. Takes ownership of <paramref name="input"/> and disposes it.
/// </summary>
internal static class ImagePersistencePreparer
{
    /// <summary>
    /// Returns the stream to persist (caller must dispose). <paramref name="input"/> is always disposed before return.
    /// </summary>
    public static async Task<(MemoryStream Stream, string FileExtension, string ContentType)> PrepareForSaveAsync(
        MemoryStream input,
        string fileName,
        string contentType,
        ImageUploadSettings settings)
    {
        var effectiveExtension = Path.GetExtension(fileName).ToLowerInvariant();
        var effectiveContentType = contentType;
        MemoryStream current = input;

        try
        {
            if (HeicToWebpConverter.IsHeic(effectiveExtension, contentType))
            {
                var converted = await HeicToWebpConverter.ConvertToWebpAsync(current).ConfigureAwait(false);
                await current.DisposeAsync().ConfigureAwait(false);
                current = converted;
                effectiveExtension = ".webp";
                effectiveContentType = "image/webp";
                ImageUploadLimits.ThrowIfExceedsMaxBytes(current.Length, settings);
            }

            var afterResize = await Task.Run(() =>
                RasterImageDimensionLimiter.LimitDimensions(current, effectiveExtension, settings.MaxImageDimensionPixels))
                .ConfigureAwait(false);

            if (!ReferenceEquals(afterResize, current))
            {
                await current.DisposeAsync().ConfigureAwait(false);
                current = afterResize;
            }

            current.Position = 0;
            return (current, effectiveExtension, effectiveContentType);
        }
        catch
        {
            await current.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
