namespace RentAll.Api.Dtos.Reports;

public class EscrowReportRowResponseDto
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

    public EscrowReportRowResponseDto(EscrowReportRow row)
    {
        RowId = row.RowId;
        OwnerName = row.OwnerName;
        PropertyId = row.PropertyId;
        PropertyCode = row.PropertyCode;
        OfficeId = row.OfficeId;
        ArBalance = row.ArBalance;
        Prepaids = row.Prepaids;
        NotCollected = row.NotCollected;
        Total = row.Total;
        E2 = row.E2;
    }
}
