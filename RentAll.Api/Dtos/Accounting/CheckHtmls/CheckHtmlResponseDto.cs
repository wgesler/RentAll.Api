using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Accounting.CheckHtmls;

public class CheckHtmlResponseDto
{
    public Guid CheckHtmlId { get; set; }
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string Check { get; set; } = "[]";
    public string? CheckStockPath { get; set; }
    public FileDetails? CheckStockFileDetails { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset ModifiedOn { get; set; }
    public Guid ModifiedBy { get; set; }

    public CheckHtmlResponseDto(CheckHtml checkHtml)
    {
        CheckHtmlId = checkHtml.CheckHtmlId;
        OrganizationId = checkHtml.OrganizationId;
        OfficeId = checkHtml.OfficeId;
        Check = checkHtml.Check;
        CheckStockPath = checkHtml.CheckStockPath;
        CreatedOn = checkHtml.CreatedOn;
        CreatedBy = checkHtml.CreatedBy;
        ModifiedOn = checkHtml.ModifiedOn;
        ModifiedBy = checkHtml.ModifiedBy;
    }
}
