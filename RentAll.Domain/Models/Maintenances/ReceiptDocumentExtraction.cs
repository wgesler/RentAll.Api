namespace RentAll.Domain.Models.Maintenances;

public class ReceiptDocumentExtraction
{
    public DateOnly? ReceiptDate { get; set; }
    public decimal? Amount { get; set; }
    public string? VendorName { get; set; }
    public string? Description { get; set; }
    public string? BillNumber { get; set; }
    public string? CardLastFour { get; set; }
    public int? CardTypeId { get; set; }
    public IReadOnlyList<string> DetectedPropertyCodes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> LineItemDescriptions { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, double?> FieldConfidences { get; set; } = new Dictionary<string, double?>();
}
