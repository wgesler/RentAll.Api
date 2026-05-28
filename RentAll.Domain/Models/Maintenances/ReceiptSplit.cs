using RentAll.Domain.Enums;
using System.Text.Json.Serialization;

namespace RentAll.Domain.Models;

public class ReceiptSplit
{
    public int ReceiptSplitId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? WorkOrder { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string? WorkOrderCode { get; set; }
    public int? BankCardId { get; set; }
    public string? BankCardDisplayName { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public int ReceiptTypeId { get; set; } = (int)ReceiptType.Tenant;

    [JsonIgnore]
    public ReceiptType ReceiptType
    {
        get
        {
            return Enum.IsDefined(typeof(ReceiptType), ReceiptTypeId)
                ? (ReceiptType)ReceiptTypeId
                : ReceiptType.Tenant;
        }
        set => ReceiptTypeId = (int)value;
    }
}
