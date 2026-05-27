using RentAll.Domain.Enums;
using System.Text.Json.Serialization;

namespace RentAll.Domain.Models;

public class ReceiptSplit
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? WorkOrder { get; set; }
    public int BankCardId { get; set; } = 0;
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
