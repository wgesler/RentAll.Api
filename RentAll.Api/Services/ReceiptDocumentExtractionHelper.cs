using RentAll.Domain.Models.Common;

namespace RentAll.Api.Services;

public static class ReceiptDocumentExtractionHelper
{
    public static byte[] GetFileBytes(FileDetails fileDetails)
    {
        var base64 = (fileDetails.File ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(base64) && !string.IsNullOrWhiteSpace(fileDetails.DataUrl))
            base64 = fileDetails.DataUrl.Trim();

        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException("Receipt file content is required.");

        if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = base64.IndexOf(',');
            if (commaIndex >= 0)
                base64 = base64[(commaIndex + 1)..];
        }

        return Convert.FromBase64String(base64);
    }
}
