namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class ReceiptSplitDto
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? WorkOrder { get; set; }
    public int BankCardId { get; set; }
    public int ReceiptTypeId { get; set; }

    public ReceiptSplitDto()
    {
    }

    public ReceiptSplitDto(ReceiptSplit split)
    {
        Amount = split.Amount;
        Description = split.Description;
        WorkOrder = split.WorkOrder;
        BankCardId = split.BankCardId;
        ReceiptTypeId = split.ReceiptTypeId;
    }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (!Enum.IsDefined(typeof(ReceiptType), ReceiptTypeId))
            return (false, $"Invalid ReceiptTypeId value: {ReceiptTypeId}");

        return (true, null);
    }

    public ReceiptSplit ToModel()
    {
        return new ReceiptSplit
        {
            Amount = Amount,
            Description = Description,
            WorkOrder = WorkOrder,
            BankCardId = BankCardId,
            ReceiptType = (ReceiptType)ReceiptTypeId
        };
    }
}
