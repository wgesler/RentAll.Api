using RentAll.Api.Dtos.Maintenances.Receipts;

namespace RentAll.Api.Dtos.Maintenances.ReceiptDrafts;

public class PromoteReceiptDraftResponseDto
{
    public ReceiptDraftResponseDto Draft { get; set; } = null!;
    public ReceiptResponseDto Receipt { get; set; } = null!;
}
