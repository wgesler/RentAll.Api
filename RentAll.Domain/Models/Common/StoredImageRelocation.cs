namespace RentAll.Domain.Models.Common;

public class StoredImageRelocation
{
    public bool AlreadyCorrect { get; set; }
    public bool Missing { get; set; }
    public bool FoundExisting { get; set; }
    public string OriginalPath { get; set; } = string.Empty;
    public string? NewPath { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}
