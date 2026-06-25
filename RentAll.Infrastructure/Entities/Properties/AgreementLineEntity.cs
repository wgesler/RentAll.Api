namespace RentAll.Infrastructure.Entities.Properties;

public class AgreementLineEntity
{
    public int AgreementLineId { get; set; }
    public Guid AgreementId { get; set; }
    public string? Title { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public int TermsId { get; set; }
    public string? Terms { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Deposit { get; set; }
    public decimal OneTime { get; set; }
    public decimal Monthly { get; set; }
    public decimal Daily { get; set; }
    public int? ChartOfAccountId { get; set; }
    public bool IsRent { get; set; }
    public string? Notes { get; set; }
}
