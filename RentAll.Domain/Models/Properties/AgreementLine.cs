namespace RentAll.Domain.Models;

public class AgreementLine
{
    public int AgreementLineId { get; set; }
    public Guid AgreementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Deposit { get; set; }
    public decimal OneTime { get; set; }
    public decimal Monthly { get; set; }
    public decimal Daily { get; set; }
}
