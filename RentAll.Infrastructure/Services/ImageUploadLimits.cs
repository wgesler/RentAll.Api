using RentAll.Domain.Configuration;

namespace RentAll.Infrastructure.Services;

internal static class ImageUploadLimits
{
    public static void ThrowIfExceedsMaxBytes(long byteLength, ImageUploadSettings settings)
    {
        var max = settings.MaxUploadBytes;
        if (max <= 0 || byteLength <= max)
            return;

        throw new ArgumentException(
            $"Image upload exceeds the maximum allowed size of {max:N0} bytes (this file is {byteLength:N0} bytes).");
    }

    /// <summary>
    /// Reads the entire stream into memory, failing as soon as <see cref="ImageUploadSettings.MaxUploadBytes"/> would be exceeded.
    /// </summary>
    public static async Task<MemoryStream> ReadImageStreamWithSizeCapAsync(Stream fileStream, ImageUploadSettings settings)
    {
        var max = settings.MaxUploadBytes;
        var buffer = new MemoryStream();
        var chunk = new byte[8192];
        long total = 0;
        int read;
        while ((read = await fileStream.ReadAsync(chunk).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (max > 0 && total > max)
            {
                await buffer.DisposeAsync().ConfigureAwait(false);
                throw new ArgumentException(
                    $"Image upload exceeds the maximum allowed size of {max:N0} bytes.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read)).ConfigureAwait(false);
        }

        buffer.Position = 0;
        return buffer;
    }
}
