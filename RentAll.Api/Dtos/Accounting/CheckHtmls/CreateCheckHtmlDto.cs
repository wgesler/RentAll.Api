using RentAll.Domain.Models.Common;

namespace RentAll.Api.Dtos.Accounting.CheckHtmls;

public class CreateCheckHtmlDto
{
    public Guid OrganizationId { get; set; }
    public int? OfficeId { get; set; }
    public string Check { get; set; } = "[]";
    public FileDetails? CheckStockFileDetails { get; set; }

    public (bool IsValid, string? ErrorMessage) IsValid(Guid organizationId)
    {
        if (OrganizationId == Guid.Empty || OrganizationId != organizationId)
            return (false, "OrganizationId not valid");

        if (string.IsNullOrWhiteSpace(Check) && (CheckStockFileDetails == null || string.IsNullOrWhiteSpace(CheckStockFileDetails.File)))
            return (false, "Check or CheckStockFileDetails is required");

        return (true, null);
    }

    public CheckHtml ToModel(Guid currentUser)
    {
        return new CheckHtml
        {
            OrganizationId = OrganizationId,
            OfficeId = OfficeId,
            Check = string.IsNullOrWhiteSpace(Check) ? "[]" : Check,
            CreatedBy = currentUser
        };
    }
}
