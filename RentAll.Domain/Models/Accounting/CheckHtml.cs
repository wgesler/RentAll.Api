namespace RentAll.Domain.Models;

public class CheckHtml
{
    public Guid CheckHtmlId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string Check { get; set; } = "[]";
    public string? CheckStockPath { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }
}
