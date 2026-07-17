namespace RentAll.Domain.Models;

public class EscrowReportRow
{
    public string RowId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public Guid PropertyId { get; set; }
    public string PropertyCode { get; set; } = string.Empty;
    public int OfficeId { get; set; }
    public decimal ArBalance { get; set; }
    public decimal Prepaids { get; set; }
    public decimal NotCollected { get; set; }
    public decimal Total { get; set; }
    public decimal E2 { get; set; }
}
