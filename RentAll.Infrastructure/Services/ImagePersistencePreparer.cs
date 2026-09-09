using RentAll.Domain.Configuration;
using RentAll.Domain.Enums;

namespace RentAll.Infrastructure.Services;

/// <summary>
/// HEIC→WebP conversion, optional dimension limiting, and aggressive JPEG compression for listing photos.
/// Takes ownership of <paramref name="input"/> and disposes it.
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
        ImageUploadSettings settings,
        ImageType imageType)
    {
        var effectiveExtension = Path.GetExtension(fileName).ToLowerInvariant();
        var effectiveContentType = contentType;
        MemoryStream current = input;

        try
        {
            if (imageType == ImageType.Photos
                && settings.AggressivePhotoCompressionEnabled
                && RasterImagePhotoCompressor.IsCompressiblePhoto(effectiveExtension, contentType))
            {
                var compressed = await Task.Run(() => RasterImagePhotoCompressor.Compress(current, settings)).ConfigureAwait(false);
                if (!ReferenceEquals(compressed, current))
                {
                    await current.DisposeAsync().ConfigureAwait(false);
                    current = compressed;
                }

                effectiveExtension = ".jpg";
                effectiveContentType = "image/jpeg";
                ImageUploadLimits.ThrowIfExceedsMaxBytes(current.Length, settings);
                current.Position = 0;
                return (current, effectiveExtension, effectiveContentType);
            }

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
