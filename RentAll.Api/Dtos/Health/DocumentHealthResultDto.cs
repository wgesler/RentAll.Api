using RentAll.Domain.Models;

namespace RentAll.Api.Dtos.Health;

public class DocumentHealthSummaryDto
{
    public string Section { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int DocumentsWithJe { get; set; }
    public int DocumentsMissingJe { get; set; }
    public int DuplicateOpenJes { get; set; }
    public bool IsClean { get; set; }

    public DocumentHealthSummaryDto()
    {
    }

    public DocumentHealthSummaryDto(DocumentHealthSummary summary)
    {
        Section = summary.Section;
        DocumentType = summary.DocumentType;
        TotalDocuments = summary.TotalDocuments;
        DocumentsWithJe = summary.DocumentsWithJe;
        DocumentsMissingJe = summary.DocumentsMissingJe;
        DuplicateOpenJes = summary.DuplicateOpenJes;
        IsClean = summary.IsClean;
    }
}

public class DocumentHealthIssueDto
{
    public string Issue { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string? RelatedCode { get; set; }
    public Guid? RelatedId { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly? TransactionDate { get; set; }
    public string? Detail { get; set; }

    public DocumentHealthIssueDto()
    {
    }

    public DocumentHealthIssueDto(DocumentHealthIssue issue)
    {
        Issue = issue.Issue;
        OrganizationId = issue.OrganizationId;
        OfficeId = issue.OfficeId;
        DocumentCode = issue.DocumentCode;
        DocumentId = issue.DocumentId;
        RelatedCode = issue.RelatedCode;
        RelatedId = issue.RelatedId;
        Amount = issue.Amount;
        TransactionDate = issue.TransactionDate;
        Detail = issue.Detail;
    }
}

public class DocumentHealthResultDto
{
    public DocumentHealthSummaryDto Summary { get; set; } = new();
    public List<DocumentHealthIssueDto> Issues { get; set; } = [];

    public DocumentHealthResultDto()
    {
    }

    public DocumentHealthResultDto(DocumentHealthResult result)
    {
        Summary = new DocumentHealthSummaryDto(result.Summary);
        Issues = result.Issues.Select(issue => new DocumentHealthIssueDto(issue)).ToList();
    }
}
