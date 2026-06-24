namespace RentAll.Domain.Models;

public class AccountingLog
{
    public int Id { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal? OriginalAmount { get; set; }
    public string? RentalLine { get; set; }
    public bool Split { get; set; }
    public string? FirstPeriod { get; set; }
    public string? SecondPeriod { get; set; }
    public decimal? FirstAmount { get; set; }
    public decimal? SecondAmount { get; set; }
    public string? Message { get; set; }
}
