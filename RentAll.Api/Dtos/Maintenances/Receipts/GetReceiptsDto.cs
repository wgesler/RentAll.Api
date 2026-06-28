using RentAll.Domain.Enums;

namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class GetReceiptsDto
{
    public int[] OfficeIds { get; set; } = [];
    public Guid? PropertyId { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeInactive { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ReceiptKind? ReceiptKind { get; set; }

    public string ResolvedOfficeIds => string.Join(",", OfficeIds);

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (OfficeIds == null || OfficeIds.Length == 0)
            return (false, "At least one office is required");

        if (OfficeIds.Any(id => id <= 0))
            return (false, "Each office ID must be a positive integer");

        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
            return (false, "EndDate must be on or after StartDate");

        if (ReceiptKind.HasValue && !Enum.IsDefined(typeof(ReceiptKind), ReceiptKind.Value))
            return (false, $"Invalid ReceiptKind value: {ReceiptKind.Value}");

        return (true, null);
    }
}
