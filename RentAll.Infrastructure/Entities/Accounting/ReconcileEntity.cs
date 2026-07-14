namespace RentAll.Infrastructure.Entities.Accounting;

public class ReconcileEntity
{
    public int ReconcileId { get; set; }
    public int AccountId { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public DateOnly? StatementDate { get; set; }
    public decimal? EndingBalance { get; set; }
    public decimal? ServiceChargeAmount { get; set; }
    public DateOnly? ServiceChargeDate { get; set; }
    public int? ServiceChargeAccountId { get; set; }
    public decimal? InterestAmount { get; set; }
    public DateOnly? InterestDate { get; set; }
    public int? InterestAccountId { get; set; }
}
