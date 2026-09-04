namespace RentAll.Domain.Models;

public class DocumentHealthSummary
{
    public string Section { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public int TotalDocuments { get; set; }
    public int DocumentsWithJe { get; set; }
    public int DocumentsMissingJe { get; set; }
    public int DuplicateOpenJes { get; set; }
    public bool IsClean { get; set; }
}

public class DocumentHealthIssue
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
}

public class DocumentHealthResult
{
    public DocumentHealthSummary Summary { get; set; } = new();
    public List<DocumentHealthIssue> Issues { get; set; } = [];
}
