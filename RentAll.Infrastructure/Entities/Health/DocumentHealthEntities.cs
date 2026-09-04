namespace RentAll.Infrastructure.Entities.Health;

public class DocumentHealthSummaryEntity
{
    public string? Section { get; set; }
    public string? DocumentType { get; set; }
    public int TotalDocuments { get; set; }
    public int DocumentsWithJe { get; set; }
    public int DocumentsMissingJe { get; set; }
    public int DuplicateOpenJes { get; set; }
    public bool IsClean { get; set; }
}

public class DocumentHealthIssueEntity
{
    public string? Issue { get; set; }
    public Guid OrganizationId { get; set; }
    public int OfficeId { get; set; }
    public string? DocumentCode { get; set; }
    public Guid DocumentId { get; set; }
    public string? RelatedCode { get; set; }
    public Guid? RelatedId { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly? TransactionDate { get; set; }
    public string? Detail { get; set; }
}
