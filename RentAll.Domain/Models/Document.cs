using RentAll.Domain.Enums;

namespace RentAll.Domain.Models;

public class Document
{
	public Guid DocumentId { get; set; }
	public Guid OrganizationId { get; set; }
	public int? OfficeId { get; set; }
	public DocumentType DocumentType { get; set; }
	public string FileName { get; set; } = string.Empty;
	public string FileExtension { get; set; } = string.Empty;
	public string ContentType { get; set; } = string.Empty;
	public string DocumentPath { get; set; } = string.Empty;
	public bool IsDeleted { get; set; }
	public DateTimeOffset CreatedOn { get; set; }
	public Guid CreatedBy { get; set; }
	public DateTimeOffset ModifiedOn { get; set; }
	public Guid ModifiedBy { get; set; }
}


