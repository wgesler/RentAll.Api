namespace RentAll.Infrastructure.Services;

public static class ReceiptFileRules
{
    public const string ConvertWordToPdfMessage = "Please convert this to a PDF before uploading.";

    public static void RejectWordDocument(string? fileName, string? contentType)
    {
        if (IsWordDocument(fileName, contentType))
            throw new ArgumentException(ConvertWordToPdfMessage);
    }

    public static bool IsWordDocument(string? fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (extension.Equals(".doc", StringComparison.OrdinalIgnoreCase) || extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
            return true;

        var type = contentType ?? string.Empty;
        return type.Contains("msword", StringComparison.OrdinalIgnoreCase)
            || type.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase);
    }
}
