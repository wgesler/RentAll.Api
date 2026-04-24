using System.Text.Json.Serialization;

namespace RentAll.Domain.Models;

public class ReceiptSplit
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? WorkOrder { get; set; }
}
