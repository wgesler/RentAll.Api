namespace RentAll.Api.Dtos.Maintenances.Receipts;

public class ReceiptSplitDto
{
    public int? ReceiptSplitId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? WorkOrder { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderCode { get; set; }
    public int ReceiptTypeId { get; set; }
    public int? ChartOfAccountId { get; set; }
    public string? ChartOfAccountDisplayName { get; set; }

    public ReceiptSplitDto()
    {
    }

    public ReceiptSplitDto(ReceiptSplit split)
    {
        ReceiptSplitId = split.ReceiptSplitId;
        Amount = split.Amount;
        Description = split.Description;
        WorkOrder = split.WorkOrderCode ?? split.WorkOrder;
        PropertyId = split.PropertyId;
        WorkOrderId = split.WorkOrderId;
        WorkOrderCode = split.WorkOrderCode;
        ReceiptTypeId = split.ReceiptTypeId;
        ChartOfAccountId = split.ChartOfAccountId;
        ChartOfAccountDisplayName = split.ChartOfAccountDisplayName;
    }

    public (bool IsValid, string? ErrorMessage) IsValid()
    {
        if (!Enum.IsDefined(typeof(ReceiptType), ReceiptTypeId))
            return (false, $"Invalid ReceiptTypeId value: {ReceiptTypeId}");

        if (ChartOfAccountId.HasValue && ChartOfAccountId.Value <= 0)
            return (false, "ChartOfAccountId must be greater than 0 when provided");

        return (true, null);
    }

    public ReceiptSplit ToModel()
    {
        var isNonExpense = ReceiptTypeId == (int)ReceiptType.NonExpense;
        return new ReceiptSplit
        {
            ReceiptSplitId = ReceiptSplitId ?? 0,
            Amount = Amount,
            Description = Description,
            WorkOrder = WorkOrder,
            PropertyId = PropertyId == Guid.Empty ? null : PropertyId,
            WorkOrderId = WorkOrderId,
            WorkOrderCode = WorkOrderCode,
            ReceiptType = (ReceiptType)ReceiptTypeId,
            ChartOfAccountId = !isNonExpense && ChartOfAccountId is > 0 ? ChartOfAccountId : null
        };
    }
}
